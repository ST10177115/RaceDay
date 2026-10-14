using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    public ResultsController(RaceDayDbContext db) => _db = db;

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private IQueryable<Result> Query() => _db.Results
        .Include(r => r.Enrolment).ThenInclude(e => e.Participant)
        .Include(r => r.Enrolment).ThenInclude(e => e.Category).ThenInclude(c => c.Event);

    /// <summary>Captures a result for a participant's enrolment. Only the Organiser who owns the event can do this.</summary>
    /// <response code="201">Result captured.</response>
    /// <response code="400">Validation failure, or the event has not taken place yet.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">A result already exists, or the position is taken in this category.</response>
    [Authorize(Roles = "Organiser")]
    [HttpPost("api/enrolments/{enrolmentId}/result")]
    [ProducesResponseType(typeof(ResultDto), 201)]
    public async Task<IActionResult> Capture(int enrolmentId, ResultUpsertDto dto)
    {
        var enrolment = await _db.Enrolments
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EnrolmentId == enrolmentId);

        if (enrolment == null) return NotFound();
        if (enrolment.Category.Event.OrganiserId != CurrentUserId) return Forbid();
        if (enrolment.Category.Event.EventDate.Date > DateTime.UtcNow.Date)
            return BadRequest("Results can only be captured once the event has taken place.");
        if (enrolment.Result != null) return Conflict("A result has already been captured for this enrolment.");

        var problem = await Validate(dto, enrolment.CategoryId, null);
        if (problem != null) return problem;

        var result = new Result { EnrolmentId = enrolmentId };
        Apply(result, dto);
        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        var saved = await Query().FirstAsync(r => r.ResultId == result.ResultId);
        return Created($"/api/events/{saved.Enrolment.Category.EventId}/results", ToDto(saved));
    }

    /// <summary>Corrects a captured result. Only the Organiser who owns the event can do this.</summary>
    /// <response code="200">Result updated.</response>
    /// <response code="400">Validation failure.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Result not found.</response>
    /// <response code="409">The position is taken in this category.</response>
    [Authorize(Roles = "Organiser")]
    [HttpPut("api/results/{id}")]
    [ProducesResponseType(typeof(ResultDto), 200)]
    public async Task<IActionResult> Update(int id, ResultUpsertDto dto)
    {
        var result = await Query().FirstOrDefaultAsync(r => r.ResultId == id);
        if (result == null) return NotFound();
        if (result.Enrolment.Category.Event.OrganiserId != CurrentUserId) return Forbid();

        var problem = await Validate(dto, result.Enrolment.CategoryId, id);
        if (problem != null) return problem;

        Apply(result, dto);
        await _db.SaveChangesAsync();
        return Ok(ToDto(result));
    }

    /// <summary>Lists the logged-in Participant's own results across all events.</summary>
    /// <response code="200">The participant's results, most recent event first.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in but not a Participant.</response>
    [Authorize(Roles = "Participant")]
    [HttpGet("api/results/me")]
    [ProducesResponseType(typeof(List<ResultDto>), 200)]
    public async Task<IActionResult> GetMine()
    {
        var list = await Query()
            .Where(r => r.Enrolment.ParticipantId == CurrentUserId)
            .ToListAsync();
        return Ok(list.OrderByDescending(r => r.Enrolment.Category.Event.EventDate).Select(ToDto));
    }

    /// <summary>Lists the results for an event, grouped by category and ordered by position. Public.</summary>
    /// <response code="200">The event's results.</response>
    /// <response code="404">Event not found.</response>
    [AllowAnonymous]
    [HttpGet("api/events/{eventId}/results")]
    [ProducesResponseType(typeof(List<ResultDto>), 200)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId)) return NotFound();

        var list = await Query().Where(r => r.Enrolment.Category.EventId == eventId).ToListAsync();
        return Ok(list
            .OrderBy(r => r.Enrolment.Category.Name)
            .ThenBy(r => r.Position ?? int.MaxValue)
            .Select(ToDto));
    }

    // Returns an error response if the data breaks a rule, otherwise null
    private async Task<IActionResult?> Validate(ResultUpsertDto dto, int categoryId, int? ignoreResultId)
    {
        if (dto.Status != "Finished") return null;

        if (dto.FinishTime == null || dto.Position == null)
            return BadRequest("A finish time and position are required when the status is Finished.");

        var taken = await _db.Results.AnyAsync(r =>
            r.Enrolment.CategoryId == categoryId &&
            r.Position == dto.Position &&
            r.ResultId != ignoreResultId);

        return taken ? Conflict("That position is already taken in this category.") : null;
    }

    private static void Apply(Result r, ResultUpsertDto dto)
    {
        r.Status = dto.Status;
        // A DNF or DNS has no time or position
        r.FinishTime = dto.Status == "Finished" ? dto.FinishTime : null;
        r.Position = dto.Status == "Finished" ? dto.Position : null;
    }

    private static ResultDto ToDto(Result r) => new()
    {
        ResultId = r.ResultId,
        EnrolmentId = r.EnrolmentId,
        ParticipantId = r.Enrolment.ParticipantId,
        ParticipantName = r.Enrolment.Participant.FullName,
        EventId = r.Enrolment.Category.EventId,
        EventName = r.Enrolment.Category.Event.Name,
        EventDate = r.Enrolment.Category.Event.EventDate,
        CategoryId = r.Enrolment.CategoryId,
        CategoryName = r.Enrolment.Category.Name,
        FinishTime = r.FinishTime,
        Position = r.Position,
        Status = r.Status
    };
}
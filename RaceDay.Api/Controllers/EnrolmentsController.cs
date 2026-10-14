using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    public EnrolmentsController(RaceDayDbContext db) => _db = db;

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private IQueryable<Enrolment> Query() => _db.Enrolments
        .Include(e => e.Participant)
        .Include(e => e.Category).ThenInclude(c => c.Event);

    /// <summary>Enrols the logged-in Participant in an event by selecting a category.</summary>
    /// <response code="201">Enrolment recorded.</response>
    /// <response code="400">The event has already taken place.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in but not a Participant.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">Already enrolled in this event, or the category is full.</response>
    [Authorize(Roles = "Participant")]
    [HttpPost("api/categories/{categoryId}/enrolments")]
    [ProducesResponseType(typeof(EnrolmentDto), 201)]
    public async Task<IActionResult> Enrol(int categoryId)
    {
        var cat = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == categoryId);
        if (cat == null) return NotFound();

        if (cat.Event.EventDate.Date < DateTime.UtcNow.Date)
            return BadRequest("This event has already taken place.");

        // One enrolment per event, whichever category was chosen
        if (await _db.Enrolments.AnyAsync(e => e.ParticipantId == CurrentUserId && e.Category.EventId == cat.EventId))
            return Conflict("You are already enrolled in this event.");

        if (await _db.Enrolments.CountAsync(e => e.CategoryId == categoryId) >= cat.MaxParticipants)
            return Conflict("This category is full.");

        var enrolment = new Enrolment { ParticipantId = CurrentUserId, CategoryId = categoryId };
        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        var saved = await Query().FirstAsync(e => e.EnrolmentId == enrolment.EnrolmentId);
        return Created("/api/enrolments/me", ToDto(saved));
    }

    /// <summary>Lists the logged-in Participant's own enrolments.</summary>
    /// <response code="200">The participant's enrolments, soonest event first.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in but not a Participant.</response>
    [Authorize(Roles = "Participant")]
    [HttpGet("api/enrolments/me")]
    [ProducesResponseType(typeof(List<EnrolmentDto>), 200)]
    public async Task<IActionResult> GetMine()
    {
        var list = await Query()
            .Where(e => e.ParticipantId == CurrentUserId)
            .OrderBy(e => e.Category.Event.EventDate)
            .ToListAsync();
        return Ok(list.Select(ToDto));
    }

    /// <summary>Lists all enrolments for an event. Only the Organiser who owns the event can do this.</summary>
    /// <response code="200">All enrolments for the event.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event not found.</response>
    [Authorize(Roles = "Organiser")]
    [HttpGet("api/events/{eventId}/enrolments")]
    [ProducesResponseType(typeof(List<EnrolmentDto>), 200)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbid();

        var list = await Query()
            .Where(e => e.Category.EventId == eventId)
            .OrderBy(e => e.Category.Name).ThenBy(e => e.Participant.FullName)
            .ToListAsync();
        return Ok(list.Select(ToDto));
    }

    /// <summary>Withdraws the logged-in Participant from an event.</summary>
    /// <response code="204">Enrolment removed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant, or not your enrolment.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">A result has already been captured for this enrolment.</response>
    [Authorize(Roles = "Participant")]
    [HttpDelete("api/enrolments/{id}")]
    public async Task<IActionResult> Withdraw(int id)
    {
        var e = await _db.Enrolments.Include(x => x.Result).FirstOrDefaultAsync(x => x.EnrolmentId == id);
        if (e == null) return NotFound();
        if (e.ParticipantId != CurrentUserId) return Forbid();
        if (e.Result != null) return Conflict("Cannot withdraw: a result has already been recorded.");

        _db.Enrolments.Remove(e);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static EnrolmentDto ToDto(Enrolment e) => new()
    {
        EnrolmentId = e.EnrolmentId,
        ParticipantId = e.ParticipantId,
        ParticipantName = e.Participant.FullName,
        ParticipantEmail = e.Participant.Email,
        EventId = e.Category.EventId,
        EventName = e.Category.Event.Name,
        EventDate = e.Category.Event.EventDate,
        CategoryId = e.CategoryId,
        CategoryName = e.Category.Name,
        EnrolmentDate = e.EnrolmentDate,
        Status = e.Status
    };
}
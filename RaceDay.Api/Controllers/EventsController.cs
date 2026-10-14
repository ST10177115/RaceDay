using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    public EventsController(RaceDayDbContext db) => _db = db;

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Lists events, soonest first. Public.</summary>
    /// <param name="upcomingOnly">When true, hides events that have already happened.</param>
    /// <response code="200">List of events with their categories.</response>
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(typeof(List<EventDto>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] bool upcomingOnly = false)
    {
        var query = _db.Events.Include(e => e.Organiser).Include(e => e.Categories).AsQueryable();
        if (upcomingOnly) query = query.Where(e => e.EventDate >= DateTime.UtcNow.Date);

        var events = await query.OrderBy(e => e.EventDate).ToListAsync();
        return Ok(events.Select(ToDto));
    }

    /// <summary>Gets one event with its categories. Public.</summary>
    /// <response code="200">The event.</response>
    /// <response code="404">Event not found.</response>
    [AllowAnonymous]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EventDto), 200)]
    public async Task<IActionResult> GetById(int id)
    {
        var e = await _db.Events.Include(x => x.Organiser).Include(x => x.Categories)
            .FirstOrDefaultAsync(x => x.EventId == id);
        return e == null ? NotFound() : Ok(ToDto(e));
    }

    /// <summary>Creates an event owned by the logged-in Organiser.</summary>
    /// <response code="201">Event created.</response>
    /// <response code="400">Validation failure.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in but not an Organiser.</response>
    [Authorize(Roles = "Organiser")]
    [HttpPost]
    [ProducesResponseType(typeof(EventDto), 201)]
    public async Task<IActionResult> Create(EventUpsertDto dto)
    {
        var e = new Event { OrganiserId = CurrentUserId };
        Apply(e, dto);
        _db.Events.Add(e);
        await _db.SaveChangesAsync();

        await _db.Entry(e).Reference(x => x.Organiser).LoadAsync();
        return CreatedAtAction(nameof(GetById), new { id = e.EventId }, ToDto(e));
    }

    /// <summary>Updates an event. Only the Organiser who created it can do this.</summary>
    /// <response code="200">Event updated.</response>
    /// <response code="400">Validation failure.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event not found.</response>
    [Authorize(Roles = "Organiser")]
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(EventDto), 200)]
    public async Task<IActionResult> Update(int id, EventUpsertDto dto)
    {
        var e = await _db.Events.Include(x => x.Organiser).Include(x => x.Categories)
            .FirstOrDefaultAsync(x => x.EventId == id);
        if (e == null) return NotFound();
        if (e.OrganiserId != CurrentUserId) return Forbid();

        Apply(e, dto);
        await _db.SaveChangesAsync();
        return Ok(ToDto(e));
    }

    /// <summary>Deletes an event and its categories. Only the owning Organiser can do this.</summary>
    /// <response code="204">Event deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event not found.</response>
    [Authorize(Roles = "Organiser")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var e = await _db.Events.FindAsync(id);
        if (e == null) return NotFound();
        if (e.OrganiserId != CurrentUserId) return Forbid();

        _db.Events.Remove(e);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static void Apply(Event e, EventUpsertDto dto)
    {
        e.Name = dto.Name.Trim();
        e.Description = dto.Description?.Trim();
        e.EventDate = dto.EventDate;
        e.Location = dto.Location.Trim();
        e.DistanceKm = dto.DistanceKm;
        e.EventType = dto.EventType;
        e.RouteInfo = dto.RouteInfo?.Trim();
    }

    private static EventDto ToDto(Event e) => new()
    {
        EventId = e.EventId,
        OrganiserId = e.OrganiserId,
        OrganiserName = e.Organiser.FullName,
        Name = e.Name,
        Description = e.Description,
        EventDate = e.EventDate,
        Location = e.Location,
        DistanceKm = e.DistanceKm,
        EventType = e.EventType,
        RouteInfo = e.RouteInfo,
        Categories = e.Categories.Select(c => new CategoryDto
        {
            CategoryId = c.CategoryId,
            EventId = c.EventId,
            Name = c.Name,
            DistanceKm = c.DistanceKm,
            MaxParticipants = c.MaxParticipants
        }).ToList()
    };
}
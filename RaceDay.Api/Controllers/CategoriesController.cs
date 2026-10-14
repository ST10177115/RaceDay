using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    public CategoriesController(RaceDayDbContext db) => _db = db;

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Lists all categories for an event. Public.</summary>
    /// <response code="200">List of categories.</response>
    /// <response code="404">Event not found.</response>
    [AllowAnonymous]
    [HttpGet("api/events/{eventId}/categories")]
    [ProducesResponseType(typeof(List<CategoryDto>), 200)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId)) return NotFound();

        var cats = await _db.Categories.Where(c => c.EventId == eventId)
            .OrderBy(c => c.DistanceKm).ToListAsync();
        return Ok(cats.Select(ToDto));
    }

    /// <summary>Adds a category to an event. Only the owning Organiser can do this.</summary>
    /// <response code="201">Category created.</response>
    /// <response code="400">Validation failure.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">A category with that name already exists for this event.</response>
    [Authorize(Roles = "Organiser")]
    [HttpPost("api/events/{eventId}/categories")]
    [ProducesResponseType(typeof(CategoryDto), 201)]
    public async Task<IActionResult> Create(int eventId, CategoryUpsertDto dto)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbid();

        var name = dto.Name.Trim();
        if (await _db.Categories.AnyAsync(c => c.EventId == eventId && c.Name == name))
            return Conflict("This event already has a category with that name.");

        var cat = new Category
        {
            EventId = eventId,
            Name = name,
            DistanceKm = dto.DistanceKm,
            MaxParticipants = dto.MaxParticipants
        };
        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();

        return Created($"/api/events/{eventId}/categories", ToDto(cat));
    }

    /// <summary>Updates a category. Only the Organiser who owns its event can do this.</summary>
    /// <response code="200">Category updated.</response>
    /// <response code="400">Validation failure.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of the event.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">Another category in this event already has that name.</response>
    [Authorize(Roles = "Organiser")]
    [HttpPut("api/categories/{id}")]
    [ProducesResponseType(typeof(CategoryDto), 200)]
    public async Task<IActionResult> Update(int id, CategoryUpsertDto dto)
    {
        var cat = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (cat == null) return NotFound();
        if (cat.Event.OrganiserId != CurrentUserId) return Forbid();

        var name = dto.Name.Trim();
        if (await _db.Categories.AnyAsync(c => c.EventId == cat.EventId && c.Name == name && c.CategoryId != id))
            return Conflict("This event already has a category with that name.");

        cat.Name = name;
        cat.DistanceKm = dto.DistanceKm;
        cat.MaxParticipants = dto.MaxParticipants;
        await _db.SaveChangesAsync();

        return Ok(ToDto(cat));
    }

    /// <summary>Deletes a category. Only the owning Organiser can do this, and only if nobody has enrolled.</summary>
    /// <response code="204">Category deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of the event.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">Participants are already enrolled in this category.</response>
    [Authorize(Roles = "Organiser")]
    [HttpDelete("api/categories/{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var cat = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (cat == null) return NotFound();
        if (cat.Event.OrganiserId != CurrentUserId) return Forbid();

        if (await _db.Enrolments.AnyAsync(e => e.CategoryId == id))
            return Conflict("Cannot delete a category that already has enrolments.");

        _db.Categories.Remove(cat);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static CategoryDto ToDto(Category c) => new()
    {
        CategoryId = c.CategoryId,
        EventId = c.EventId,
        Name = c.Name,
        DistanceKm = c.DistanceKm,
        MaxParticipants = c.MaxParticipants
    };
}
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Controllers;

[ApiController]
[Authorize]                       // every endpoint here needs a logged-in user
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    public UsersController(RaceDayDbContext db) => _db = db;

    // The user's ID comes from the login cookie, never from the request,
    // so a user can only ever read or change their own profile
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Returns the logged-in user's own profile.</summary>
    /// <response code="200">Profile returned.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), 200)]
    public async Task<IActionResult> GetMe()
    {
        var u = await _db.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.UserId == CurrentUserId);
        if (u == null) return NotFound();

        return Ok(new UserDto { UserId = u.UserId, FullName = u.FullName, Email = u.Email, Role = u.Role.RoleName });
    }

    /// <summary>Updates the logged-in user's name and email.</summary>
    /// <response code="200">Profile updated.</response>
    /// <response code="400">Validation failure.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="409">Email belongs to another account.</response>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserDto), 200)]
    public async Task<IActionResult> UpdateMe(UpdateProfileDto dto)
    {
        var u = await _db.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.UserId == CurrentUserId);
        if (u == null) return NotFound();

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(x => x.Email == email && x.UserId != u.UserId))
            return Conflict("That email is already in use.");

        u.FullName = dto.FullName.Trim();
        u.Email = email;
        await _db.SaveChangesAsync();

        return Ok(new UserDto { UserId = u.UserId, FullName = u.FullName, Email = u.Email, Role = u.Role.RoleName });
    }
}
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Authorize]                       // every endpoint here needs a logged-in user
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IBlobStorageService _blobs;

    public UsersController(RaceDayDbContext db, IBlobStorageService blobs)
    {
        _db = db;
        _blobs = blobs;
    }

    // The user's ID comes from the login cookie, never from the request,
    // so a user can only ever read or change their own profile
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static UserDto ToDto(User u) => new()
    {
        UserId = u.UserId,
        FullName = u.FullName,
        Email = u.Email,
        Role = u.Role.RoleName,
        ProfilePictureUrl = u.ProfilePictureUrl
    };

    /// <summary>Returns the logged-in user's own profile.</summary>
    /// <response code="200">Profile returned.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), 200)]
    public async Task<IActionResult> GetMe()
    {
        var u = await _db.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.UserId == CurrentUserId);
        if (u == null) return NotFound();

        return Ok(ToDto(u));
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

        return Ok(ToDto(u));
    }

    /// <summary>Uploads or replaces the logged-in user's profile picture.</summary>
    /// <response code="200">Picture saved. Returns the updated profile.</response>
    /// <response code="400">Missing or invalid image (JPEG, PNG or WebP, max 5 MB).</response>
    /// <response code="401">Not logged in.</response>
    [HttpPost("me/picture")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UserDto), 200)]
    public async Task<IActionResult> UploadPicture(IFormFile file)
    {
        var u = await _db.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.UserId == CurrentUserId);
        if (u == null) return NotFound();

        string newUrl;
        try
        {
            newUrl = await _blobs.UploadAsync("profile-pictures", file);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        var oldUrl = u.ProfilePictureUrl;
        u.ProfilePictureUrl = newUrl;
        await _db.SaveChangesAsync();

        if (!string.IsNullOrEmpty(oldUrl))
        {
            await _blobs.DeleteAsync("profile-pictures", oldUrl);
        }

        return Ok(ToDto(u));
    }

    /// <summary>Removes the logged-in user's profile picture.</summary>
    /// <response code="204">Picture removed.</response>
    /// <response code="401">Not logged in.</response>
    [HttpDelete("me/picture")]
    public async Task<IActionResult> DeletePicture()
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.UserId == CurrentUserId);
        if (u == null) return NotFound();

        if (!string.IsNullOrEmpty(u.ProfilePictureUrl))
        {
            await _blobs.DeleteAsync("profile-pictures", u.ProfilePictureUrl);
            u.ProfilePictureUrl = null;
            await _db.SaveChangesAsync();
        }

        return NoContent();
    }
}
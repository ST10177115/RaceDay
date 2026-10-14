using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IPasswordHasher<User> _hasher;
    private readonly IConfiguration _config;

    public AuthController(RaceDayDbContext db, IPasswordHasher<User> hasher, IConfiguration config)
    {
        _db = db;
        _hasher = hasher;
        _config = config;
    }

    /// <summary>Registers a new Organiser or Participant account.</summary>
    /// <response code="201">Account created.</response>
    /// <response code="400">Invalid role or validation failure.</response>
    /// <response code="403">Wrong or missing organiser invite code.</response>
    /// <response code="409">Email already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), 201)]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (dto.Role != "Organiser" && dto.Role != "Participant")
            return BadRequest("Role must be 'Organiser' or 'Participant'.");

        if (dto.Role == "Organiser" && dto.OrganiserCode != _config["OrganiserInviteCode"])
            return StatusCode(403, "A valid organiser invite code is required.");

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict("That email is already registered.");

        var role = await _db.Roles.FirstAsync(r => r.RoleName == dto.Role);
        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = email,
            RoleId = role.RoleId
        };
        user.PasswordHash = _hasher.HashPassword(user, dto.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Created("/api/users/me", new UserDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = role.RoleName
        });
    }

    /// <summary>Logs in and starts a cookie session carrying the user's role.</summary>
    /// <response code="200">Logged in.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(UserDto), 200)]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);

        if (user == null ||
            _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password) == PasswordVerificationResult.Failed)
            return Unauthorized("Invalid email or password.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role.RoleName)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return Ok(new UserDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.RoleName
        });
    }

    /// <summary>Ends the current session.</summary>
    /// <response code="204">Logged out.</response>
    /// <response code="401">Not logged in.</response>
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}
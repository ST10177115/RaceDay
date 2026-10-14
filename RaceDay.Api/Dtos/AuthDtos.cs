using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class RegisterDto
{
    [Required, MaxLength(100)] public string FullName { get; set; } = "";
    [Required, EmailAddress, MaxLength(150)] public string Email { get; set; } = "";
    [Required, MinLength(8)] public string Password { get; set; } = "";
    [Required] public string Role { get; set; } = "Participant";   // "Organiser" or "Participant"
    public string? OrganiserCode { get; set; }                     // required only for Organiser
}

public class LoginDto
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}

public class UserDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
}

public class UpdateProfileDto
{
    [Required, MaxLength(100)] public string FullName { get; set; } = "";
    [Required, EmailAddress, MaxLength(150)] public string Email { get; set; } = "";
}
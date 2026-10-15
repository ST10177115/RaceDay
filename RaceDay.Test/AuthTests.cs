using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using Xunit;

namespace RaceDay.Test;

public class AuthTests : IDisposable
{
    // A new factory per test class instance, so every test starts with an empty database
    private readonly RaceDayFactory _factory = new();
    public void Dispose() => _factory.Dispose();

    // ---------- Registration ----------

    [Fact]
    public async Task Register_AsOrganiserWithValidCode_ReturnsCreated()
    {
        var client = _factory.CreateClient();

        var response = await TestHelpers.Register(client, "org@test.com", "Organiser",
            organiserCode: RaceDayFactory.OrganiserCode);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.Equal("Organiser", user!.Role);
    }

    [Fact]
    public async Task Register_AsOrganiserWithWrongCode_ReturnsForbidden()
    {
        var client = _factory.CreateClient();

        var response = await TestHelpers.Register(client, "org@test.com", "Organiser",
            organiserCode: "wrong-code");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_AsOrganiserWithoutCode_ReturnsForbidden()
    {
        var client = _factory.CreateClient();

        var response = await TestHelpers.Register(client, "org@test.com", "Organiser");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        await TestHelpers.Register(client, "dup@test.com");

        var response = await TestHelpers.Register(client, "dup@test.com");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmailDifferentCase_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        await TestHelpers.Register(client, "dup@test.com");

        var response = await TestHelpers.Register(client, "DUP@Test.com");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await TestHelpers.Register(client, "short@test.com", password: "123");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await TestHelpers.Register(client, "admin@test.com", "Admin");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ResponseDoesNotExposePassword()
    {
        var client = _factory.CreateClient();

        var response = await TestHelpers.Register(client, "safe@test.com");
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotPlainText()
    {
        var client = _factory.CreateClient();
        await TestHelpers.Register(client, "hash@test.com");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var user = await db.Users.SingleAsync(u => u.Email == "hash@test.com");

        Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash));
        Assert.NotEqual("Password123", user.PasswordHash);
    }

    // ---------- Login ----------

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsOkAndRole()
    {
        var client = _factory.CreateClient();
        await TestHelpers.Register(client, "login@test.com");

        var response = await TestHelpers.Login(client, "login@test.com");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.Equal("Participant", user!.Role);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        await TestHelpers.Register(client, "login@test.com");

        var response = await TestHelpers.Login(client, "login@test.com", "WrongPassword");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await TestHelpers.Login(client, "nobody@test.com");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Session: protected areas need a login ----------

    [Fact]
    public async Task Profile_WhenNotLoggedIn_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Profile_AfterLogin_ReturnsOwnProfile()
    {
        var client = _factory.CreateClient();
        await TestHelpers.RegisterAndLogin(client, "me@test.com", "Participant");

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.Equal("me@test.com", user!.Email);
    }

    [Fact]
    public async Task Logout_ThenProfile_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        await TestHelpers.RegisterAndLogin(client, "out@test.com", "Participant");

        var logout = await client.PostAsync("/api/auth/logout", null);
        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
using System.Net.Http.Json;

namespace RaceDay.Test;

public static class TestHelpers
{
    public static Task<HttpResponseMessage> Register(
        HttpClient client, string email, string role = "Participant",
        string password = "Password123", string? organiserCode = null)
        => client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Test User",
            email,
            password,
            role,
            organiserCode
        });

    public static Task<HttpResponseMessage> Login(
        HttpClient client, string email, string password = "Password123")
        => client.PostAsJsonAsync("/api/auth/login", new { email, password });

    /// <summary>Registers a user and logs them in. The client keeps the session cookie.</summary>
    public static async Task RegisterAndLogin(HttpClient client, string email, string role)
    {
        var code = role == "Organiser" ? RaceDayFactory.OrganiserCode : null;

        (await Register(client, email, role, organiserCode: code)).EnsureSuccessStatusCode();
        (await Login(client, email)).EnsureSuccessStatusCode();
    }

    public static Task<HttpResponseMessage> CreateEvent(HttpClient client, string name = "Test 10K")
        => client.PostAsJsonAsync("/api/events", new
        {
            name,
            description = "A test event.",
            eventDate = DateTime.UtcNow.Date.AddDays(30),
            location = "Pietermaritzburg",
            distanceKm = 10,
            eventType = "Run",
            routeInfo = "Out and back."
        });
}
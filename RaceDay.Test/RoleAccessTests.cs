using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace RaceDay.Test;

public class RoleAccessTests : IDisposable
{
    private readonly RaceDayFactory _factory = new();
    public void Dispose() => _factory.Dispose();

    private async Task<HttpClient> LoginAs(string email, string role)
    {
        var client = _factory.CreateClient();
        await TestHelpers.RegisterAndLogin(client, email, role);
        return client;
    }

    // ---------- Not logged in: 401 ----------

    [Fact]
    public async Task CreateEvent_WhenNotLoggedIn_ReturnsUnauthorized()
    {
        var response = await TestHelpers.CreateEvent(_factory.CreateClient());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_WhenNotLoggedIn_ReturnsUnauthorized()
    {
        var response = await _factory.CreateClient().PostAsync("/api/categories/1/enrolments", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Participant tries Organiser actions: 403 ----------

    [Fact]
    public async Task CreateEvent_AsParticipant_ReturnsForbidden()
    {
        var participant = await LoginAs("p@test.com", "Participant");

        var response = await TestHelpers.CreateEvent(participant);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_AsParticipant_ReturnsForbidden()
    {
        var organiser = await LoginAs("org@test.com", "Organiser");
        var id = await EventHelpers.CreateEventAndGetId(organiser);
        var participant = await LoginAs("p@test.com", "Participant");

        var response = await EventHelpers.UpdateEvent(participant, id, "Hijacked");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_AsParticipant_ReturnsForbidden()
    {
        var organiser = await LoginAs("org@test.com", "Organiser");
        var id = await EventHelpers.CreateEventAndGetId(organiser);
        var participant = await LoginAs("p@test.com", "Participant");

        var response = await participant.DeleteAsync($"/api/events/{id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddCategory_AsParticipant_ReturnsForbidden()
    {
        var organiser = await LoginAs("org@test.com", "Organiser");
        var id = await EventHelpers.CreateEventAndGetId(organiser);
        var participant = await LoginAs("p@test.com", "Participant");

        var response = await participant.PostAsJsonAsync($"/api/events/{id}/categories", new
        {
            name = "Sneaky",
            distanceKm = 5,
            maxParticipants = 10
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ViewEventEnrolments_AsParticipant_ReturnsForbidden()
    {
        var organiser = await LoginAs("org@test.com", "Organiser");
        var id = await EventHelpers.CreateEventAndGetId(organiser);
        var participant = await LoginAs("p@test.com", "Participant");

        var response = await participant.GetAsync($"/api/events/{id}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_AsParticipant_ReturnsForbidden()
    {
        var participant = await LoginAs("p@test.com", "Participant");

        var response = await participant.PostAsJsonAsync("/api/enrolments/1/result", new
        {
            finishTime = "00:50:00",
            position = 1,
            status = "Finished"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- Organiser tries Participant actions: 403 ----------

    [Fact]
    public async Task Enrol_AsOrganiser_ReturnsForbidden()
    {
        var organiser = await LoginAs("org@test.com", "Organiser");
        var eventId = await EventHelpers.CreateEventAndGetId(organiser);
        var categoryId = await EventHelpers.CreateCategoryAndGetId(organiser, eventId);

        var response = await organiser.PostAsync($"/api/categories/{categoryId}/enrolments", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MyEnrolments_AsOrganiser_ReturnsForbidden()
    {
        var organiser = await LoginAs("org@test.com", "Organiser");

        var response = await organiser.GetAsync("/api/enrolments/me");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MyResults_AsOrganiser_ReturnsForbidden()
    {
        var organiser = await LoginAs("org@test.com", "Organiser");

        var response = await organiser.GetAsync("/api/results/me");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
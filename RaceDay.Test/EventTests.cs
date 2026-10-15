using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using Xunit;

namespace RaceDay.Test;

public class EventTests : IDisposable
{
    private readonly RaceDayFactory _factory = new();
    public void Dispose() => _factory.Dispose();

    // Creates a client that is registered and logged in, with its own session cookie
    private async Task<HttpClient> LoginAs(string email, string role)
    {
        var client = _factory.CreateClient();
        await TestHelpers.RegisterAndLogin(client, email, role);
        return client;
    }

    // ---------- Create ----------

    [Fact]
    public async Task Create_AsOrganiser_ReturnsCreatedWithEventDetails()
    {
        var organiser = await LoginAs("org@test.com", "Organiser");

        var response = await TestHelpers.CreateEvent(organiser, "Durban 10K");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var ev = await response.Content.ReadFromJsonAsync<EventDto>(EventHelpers.Json);
        Assert.Equal("Durban 10K", ev!.Name);
        Assert.Equal(EventType.Run, ev.EventType);
        Assert.Equal(10, ev.DistanceKm);
    }

    [Fact]
    public async Task Create_WithMissingName_ReturnsBadRequest()
    {
        var organiser = await LoginAs("org@test.com", "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", new
        {
            name = "",
            eventDate = DateTime.UtcNow.Date.AddDays(30),
            location = "Durban",
            distanceKm = 10,
            eventType = "Run"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithZeroDistance_ReturnsBadRequest()
    {
        var organiser = await LoginAs("org@test.com", "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", new
        {
            name = "Bad Event",
            eventDate = DateTime.UtcNow.Date.AddDays(30),
            location = "Durban",
            distanceKm = 0,
            eventType = "Run"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- View (public) ----------

    [Fact]
    public async Task GetAll_WhenNotLoggedIn_ReturnsOkWithEvents()
    {
        var organiser = await LoginAs("org@test.com", "Organiser");
        await EventHelpers.CreateEventAndGetId(organiser, "Public 10K");
        var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var events = await response.Content.ReadFromJsonAsync<List<EventDto>>(EventHelpers.Json);
        Assert.Single(events!);
        Assert.Equal("Public 10K", events![0].Name);
    }

    [Fact]
    public async Task GetById_WhenEventExists_ReturnsOk()
    {
        var organiser = await LoginAs("org@test.com", "Organiser");
        var id = await EventHelpers.CreateEventAndGetId(organiser);

        var response = await _factory.CreateClient().GetAsync($"/api/events/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WhenEventMissing_ReturnsNotFound()
    {
        var response = await _factory.CreateClient().GetAsync("/api/events/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- Update ----------

    [Fact]
    public async Task Update_AsOwner_ReturnsOkAndChangesName()
    {
        var organiser = await LoginAs("org@test.com", "Organiser");
        var id = await EventHelpers.CreateEventAndGetId(organiser);

        var response = await EventHelpers.UpdateEvent(organiser, id, "Renamed 21K");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var ev = await response.Content.ReadFromJsonAsync<EventDto>(EventHelpers.Json);
        Assert.Equal("Renamed 21K", ev!.Name);
        Assert.Equal(21, ev.DistanceKm);
    }

    [Fact]
    public async Task Update_AsDifferentOrganiser_ReturnsForbidden()
    {
        var owner = await LoginAs("owner@test.com", "Organiser");
        var id = await EventHelpers.CreateEventAndGetId(owner);
        var otherOrganiser = await LoginAs("other@test.com", "Organiser");

        var response = await EventHelpers.UpdateEvent(otherOrganiser, id, "Hijacked");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_WhenEventMissing_ReturnsNotFound()
    {
        var organiser = await LoginAs("org@test.com", "Organiser");

        var response = await EventHelpers.UpdateEvent(organiser, 999, "Ghost");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- Delete ----------

    [Fact]
    public async Task Delete_AsOwner_ReturnsNoContentAndEventIsGone()
    {
        var organiser = await LoginAs("org@test.com", "Organiser");
        var id = await EventHelpers.CreateEventAndGetId(organiser);

        var response = await organiser.DeleteAsync($"/api/events/{id}");
        var afterwards = await organiser.GetAsync($"/api/events/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterwards.StatusCode);
    }

    [Fact]
    public async Task Delete_AsDifferentOrganiser_ReturnsForbidden()
    {
        var owner = await LoginAs("owner@test.com", "Organiser");
        var id = await EventHelpers.CreateEventAndGetId(owner);
        var otherOrganiser = await LoginAs("other@test.com", "Organiser");

        var response = await otherOrganiser.DeleteAsync($"/api/events/{id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WhenEventMissing_ReturnsNotFound()
    {
        var organiser = await LoginAs("org@test.com", "Organiser");

        var response = await organiser.DeleteAsync("/api/events/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
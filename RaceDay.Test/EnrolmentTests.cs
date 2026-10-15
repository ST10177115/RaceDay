using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using Xunit;

namespace RaceDay.Test;

public class EnrolmentTests : IDisposable
{
    private readonly RaceDayFactory _factory = new();
    public void Dispose() => _factory.Dispose();

    private async Task<HttpClient> LoginAs(string email, string role)
    {
        var client = _factory.CreateClient();
        await TestHelpers.RegisterAndLogin(client, email, role);
        return client;
    }

    // An Organiser creates a future event with one category
    private async Task<(HttpClient Organiser, int EventId, int CategoryId)> SetUpEventWithCategory(int maxParticipants = 100)
    {
        var organiser = await LoginAs("org@test.com", "Organiser");
        var eventId = await EventHelpers.CreateEventAndGetId(organiser);
        var categoryId = await AddCategory(organiser, eventId, "10km", maxParticipants);
        return (organiser, eventId, categoryId);
    }

    private static async Task<int> AddCategory(HttpClient organiser, int eventId, string name, int maxParticipants = 100)
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            name,
            distanceKm = 10,
            maxParticipants
        });
        response.EnsureSuccessStatusCode();
        var cat = await response.Content.ReadFromJsonAsync<CategoryDto>(EventHelpers.Json);
        return cat!.CategoryId;
    }

    private static Task<HttpResponseMessage> Enrol(HttpClient participant, int categoryId)
        => participant.PostAsync($"/api/categories/{categoryId}/enrolments", null);

    // ---------- Enrolling ----------

    [Fact]
    public async Task Enrol_AsParticipant_ReturnsCreatedWithDetails()
    {
        var (_, _, categoryId) = await SetUpEventWithCategory();
        var participant = await LoginAs("p@test.com", "Participant");

        var response = await Enrol(participant, categoryId);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var enrolment = await response.Content.ReadFromJsonAsync<EnrolmentDto>(EventHelpers.Json);
        Assert.Equal("Test 10K", enrolment!.EventName);
        Assert.Equal("10km", enrolment.CategoryName);
        Assert.Equal("Confirmed", enrolment.Status);
    }

    [Fact]
    public async Task Enrol_IsRecordedAgainstTheCorrectParticipantAndCategory()
    {
        var (_, _, categoryId) = await SetUpEventWithCategory();
        var participant = await LoginAs("p@test.com", "Participant");
        var profile = await (await participant.GetAsync("/api/users/me"))
            .Content.ReadFromJsonAsync<UserDto>(EventHelpers.Json);

        await Enrol(participant, categoryId);

        // Check the database directly
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var saved = await db.Enrolments.SingleAsync();
        Assert.Equal(profile!.UserId, saved.ParticipantId);
        Assert.Equal(categoryId, saved.CategoryId);
        Assert.Equal("Confirmed", saved.Status);
    }

    [Fact]
    public async Task Enrol_TwiceInTheSameCategory_ReturnsConflict()
    {
        var (_, _, categoryId) = await SetUpEventWithCategory();
        var participant = await LoginAs("p@test.com", "Participant");
        await Enrol(participant, categoryId);

        var response = await Enrol(participant, categoryId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_InTwoCategoriesOfTheSameEvent_ReturnsConflict()
    {
        var (organiser, eventId, firstCategory) = await SetUpEventWithCategory();
        var secondCategory = await AddCategory(organiser, eventId, "5km");
        var participant = await LoginAs("p@test.com", "Participant");
        await Enrol(participant, firstCategory);

        var response = await Enrol(participant, secondCategory);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_WhenCategoryIsFull_ReturnsConflict()
    {
        var (_, _, categoryId) = await SetUpEventWithCategory(maxParticipants: 1);
        var first = await LoginAs("p1@test.com", "Participant");
        var second = await LoginAs("p2@test.com", "Participant");
        await Enrol(first, categoryId);

        var response = await Enrol(second, categoryId);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_InMissingCategory_ReturnsNotFound()
    {
        var participant = await LoginAs("p@test.com", "Participant");

        var response = await Enrol(participant, 999);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_InAnEventThatAlreadyHappened_ReturnsBadRequest()
    {
        var organiser = await LoginAs("org@test.com", "Organiser");
        var pastEvent = await organiser.PostAsJsonAsync("/api/events", new
        {
            name = "Old Race",
            eventDate = DateTime.UtcNow.Date.AddDays(-5),
            location = "Durban",
            distanceKm = 10,
            eventType = "Run"
        });
        var ev = await pastEvent.Content.ReadFromJsonAsync<EventDto>(EventHelpers.Json);
        var categoryId = await AddCategory(organiser, ev!.EventId, "10km");
        var participant = await LoginAs("p@test.com", "Participant");

        var response = await Enrol(participant, categoryId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- Viewing enrolments ----------

    [Fact]
    public async Task MyEnrolments_ReturnsOnlyTheLoggedInParticipantsEnrolments()
    {
        var (_, _, categoryId) = await SetUpEventWithCategory();
        var first = await LoginAs("p1@test.com", "Participant");
        var second = await LoginAs("p2@test.com", "Participant");
        await Enrol(first, categoryId);
        await Enrol(second, categoryId);

        var response = await first.GetAsync("/api/enrolments/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<EnrolmentDto>>(EventHelpers.Json);
        Assert.Single(list!);
        Assert.Equal("p1@test.com", list![0].ParticipantEmail);
    }

    [Fact]
    public async Task EventEnrolments_AsOwningOrganiser_ReturnsAllEnrolments()
    {
        var (organiser, eventId, categoryId) = await SetUpEventWithCategory();
        await Enrol(await LoginAs("p1@test.com", "Participant"), categoryId);
        await Enrol(await LoginAs("p2@test.com", "Participant"), categoryId);

        var response = await organiser.GetAsync($"/api/events/{eventId}/enrolments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<EnrolmentDto>>(EventHelpers.Json);
        Assert.Equal(2, list!.Count);
    }

    [Fact]
    public async Task EventEnrolments_AsDifferentOrganiser_ReturnsForbidden()
    {
        var (_, eventId, _) = await SetUpEventWithCategory();
        var otherOrganiser = await LoginAs("other@test.com", "Organiser");

        var response = await otherOrganiser.GetAsync($"/api/events/{eventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- Withdrawing ----------

    [Fact]
    public async Task Withdraw_OwnEnrolment_ReturnsNoContentAndRemovesIt()
    {
        var (_, _, categoryId) = await SetUpEventWithCategory();
        var participant = await LoginAs("p@test.com", "Participant");
        var created = await (await Enrol(participant, categoryId))
            .Content.ReadFromJsonAsync<EnrolmentDto>(EventHelpers.Json);

        var response = await participant.DeleteAsync($"/api/enrolments/{created!.EnrolmentId}");
        var remaining = await (await participant.GetAsync("/api/enrolments/me"))
            .Content.ReadFromJsonAsync<List<EnrolmentDto>>(EventHelpers.Json);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(remaining!);
    }

    [Fact]
    public async Task Withdraw_AnotherParticipantsEnrolment_ReturnsForbidden()
    {
        var (_, _, categoryId) = await SetUpEventWithCategory();
        var owner = await LoginAs("owner@test.com", "Participant");
        var created = await (await Enrol(owner, categoryId))
            .Content.ReadFromJsonAsync<EnrolmentDto>(EventHelpers.Json);
        var intruder = await LoginAs("intruder@test.com", "Participant");

        var response = await intruder.DeleteAsync($"/api/enrolments/{created!.EnrolmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Withdraw_MissingEnrolment_ReturnsNotFound()
    {
        var participant = await LoginAs("p@test.com", "Participant");

        var response = await participant.DeleteAsync("/api/enrolments/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
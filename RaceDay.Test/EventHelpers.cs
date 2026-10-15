using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using RaceDay.Api.Dtos;

namespace RaceDay.Test;

public static class EventHelpers
{
    // The API sends enums such as EventType as text ("Run"), so the tests must read them the same way
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task<int> CreateEventAndGetId(HttpClient organiserClient, string name = "Test 10K")
    {
        var response = await TestHelpers.CreateEvent(organiserClient, name);
        response.EnsureSuccessStatusCode();
        var ev = await response.Content.ReadFromJsonAsync<EventDto>(Json);
        return ev!.EventId;
    }

    public static Task<HttpResponseMessage> UpdateEvent(HttpClient client, int eventId, string name)
        => client.PutAsJsonAsync($"/api/events/{eventId}", new
        {
            name,
            description = "An updated event.",
            eventDate = DateTime.UtcNow.Date.AddDays(30),
            location = "Durban",
            distanceKm = 21,
            eventType = "Run",
            routeInfo = "Along the beachfront."
        });

    public static async Task<int> CreateCategoryAndGetId(HttpClient organiserClient, int eventId)
    {
        var response = await organiserClient.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            name = "10km",
            distanceKm = 10,
            maxParticipants = 100
        });
        response.EnsureSuccessStatusCode();
        var cat = await response.Content.ReadFromJsonAsync<CategoryDto>(Json);
        return cat!.CategoryId;
    }
}
using System.Net;
using Xunit;

namespace RaceDay.Test;

public class SetupTests : IDisposable
{
    private readonly RaceDayFactory _factory = new();
    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetEvents_WhenNoEventsExist_ReturnsOkAndEmptyList()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Register_AsParticipant_ReturnsCreated()
    {
        var client = _factory.CreateClient();

        var response = await TestHelpers.Register(client, "setup@test.com");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
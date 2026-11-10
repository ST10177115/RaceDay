namespace RaceDay.Web.Models;

// The shapes of the JSON the API sends back
public record CategoryInfo(int CategoryId, int EventId, string Name, decimal DistanceKm, int MaxParticipants);

public record EventInfo(
    int EventId,
    int OrganiserId,
    string OrganiserName,
    string Name,
    string? Description,
    DateTime EventDate,
    string Location,
    decimal DistanceKm,
    string EventType,
    string? RouteInfo,
    string? BannerUrl,
    int? EnrolmentCount,
    List<CategoryInfo> Categories);

public record EnrolmentInfo(
    int EnrolmentId,
    int ParticipantId,
    string ParticipantName,
    string ParticipantEmail,
    int EventId,
    string EventName,
    DateTime EventDate,
    int CategoryId,
    string CategoryName,
    DateTime EnrolmentDate,
    string Status);

public record ResultInfo(
    int ResultId,
    int EnrolmentId,
    int ParticipantId,
    string ParticipantName,
    int EventId,
    string EventName,
    DateTime EventDate,
    int CategoryId,
    string CategoryName,
    TimeSpan? FinishTime,
    int? Position,
    string Status,
    int TotalFinishers);

public class HomeViewModel
{
    public List<EventInfo> Events { get; set; } = new();
    public string? Search { get; set; }
    public string? EventType { get; set; }
    public string? Error { get; set; }
}

public class EventDetailsViewModel
{
    public EventInfo? Event { get; set; }
    public string? Error { get; set; }
}
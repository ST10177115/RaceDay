using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public class EventUpsertDto
{
    [Required, MaxLength(150)] public string Name { get; set; } = "";
    [MaxLength(1000)] public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    [Required, MaxLength(150)] public string Location { get; set; } = "";
    [Range(0.1, 1000)] public decimal DistanceKm { get; set; }
    public EventType EventType { get; set; }
    [MaxLength(1000)] public string? RouteInfo { get; set; }
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public int MaxParticipants { get; set; }
}

public class EventDto
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public EventType EventType { get; set; }
    public string? RouteInfo { get; set; }
    public List<CategoryDto> Categories { get; set; } = new();
}
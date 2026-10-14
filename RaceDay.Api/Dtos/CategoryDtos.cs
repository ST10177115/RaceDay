using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class CategoryUpsertDto
{
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    [Range(0.1, 1000)] public decimal DistanceKm { get; set; }
    [Range(1, 100000)] public int MaxParticipants { get; set; } = 500;
}
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class ResultUpsertDto
{
    /// <summary>Format hh:mm:ss, for example 00:52:30. Required when Status is Finished.</summary>
    public TimeSpan? FinishTime { get; set; }

    [Range(1, 100000)] public int? Position { get; set; }

    /// <summary>Finished, DNF (did not finish) or DNS (did not start).</summary>
    [Required, RegularExpression("^(Finished|DNF|DNS)$", ErrorMessage = "Status must be Finished, DNF or DNS.")]
    public string Status { get; set; } = "Finished";
}

public class ResultDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = "";
    public int EventId { get; set; }
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = "";
}
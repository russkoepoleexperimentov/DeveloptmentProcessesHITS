// Application/DTOs/GradeDistribution/GradeDistributionResponseDto.cs
using System.ComponentModel.DataAnnotations;

namespace GoogleClass.DTOs.GradeDistribution;

public class GradeDistributionResponseDto
{
    [Required] public Guid TeamId { get; set; }
    [Required] public Guid AssignmentId { get; set; }
    [Required] public decimal TeamRawScore { get; set; }
    [Required] public List<GradeDistributionEntryDto> Entries { get; set; } = new();
    [Required] public decimal SumDistributed { get; set; }
    [Required] public bool DistributionChanged { get; set; }
}

public class GradeDistributionEntryDto
{
    [Required] public Guid UserId { get; set; }
    [Required] public decimal Points { get; set; }
}

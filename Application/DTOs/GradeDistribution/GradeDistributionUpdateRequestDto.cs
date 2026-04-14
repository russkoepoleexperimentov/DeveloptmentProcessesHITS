// Application/DTOs/GradeDistribution/GradeDistributionResponseDto.cs
using System.ComponentModel.DataAnnotations;

namespace GoogleClass.DTOs.GradeDistribution;

public class GradeDistributionUpdateRequestDto
{
    [Required] public List<GradeDistributionEntryDto> Entries { get; set; } = new();
}
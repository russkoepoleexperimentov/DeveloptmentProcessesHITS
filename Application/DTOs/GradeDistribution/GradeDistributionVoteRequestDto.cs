// Application/DTOs/GradeDistribution/GradeDistributionResponseDto.cs
using System.ComponentModel.DataAnnotations;
using Domain.Models;

namespace GoogleClass.DTOs.GradeDistribution;

public class GradeDistributionVoteRequestDto
{
    [Required] public GradeVoteType Vote { get; set; }
}

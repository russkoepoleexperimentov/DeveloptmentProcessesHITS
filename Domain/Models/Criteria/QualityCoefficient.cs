using GoogleClass.DTOs.Common;

namespace Domain.Models.Criteria;

public class QualityCoefficient : Criterion
{
    public float Threshold { get; set; }
    public float Score { get; set; }
    public CriterionDirection Direction { get; set; }
}

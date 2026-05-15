using GoogleClass.DTOs.Common;

namespace Domain.Models.Criteria;

public class BonusPenaltyCriterion : ToggledCriterion
{
    public float Score { get; set; }
    public CriterionDirection Direction { get; set; }
}

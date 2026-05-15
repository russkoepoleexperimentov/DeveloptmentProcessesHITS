namespace Domain.Models.Criteria;

public class BlockingModifier : ToggledCriterion
{
    public float MaxAllowedScore { get; set; }
}

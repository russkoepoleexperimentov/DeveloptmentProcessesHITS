namespace Domain.Models.Criteria;

public class WeightedCriterion : Criterion
{
    public float MaxScore { get; set; }
    public float Weight { get; set; }
}

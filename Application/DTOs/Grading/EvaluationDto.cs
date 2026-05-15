namespace Application.DTOs.Grading;

public class EvaluationDto
{
    public List<WeightedValueDto> WeightedValues { get; set; } = new();
    public List<ToggledValueDto> ToggledValues { get; set; } = new();
}

public class WeightedValueDto
{
    public Guid CriterionId { get; set; }
    public float Score { get; set; }
}

public class ToggledValueDto
{
    public Guid CriterionId { get; set; }
    public bool Enabled { get; set; }
}

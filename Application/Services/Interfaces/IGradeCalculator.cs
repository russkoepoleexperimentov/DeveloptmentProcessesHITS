using Application.DTOs.Grading;
using Domain.Models.Criteria;

namespace Application.Services.Interfaces;

public interface IGradeCalculator
{
    GradeBreakdownDto Calculate(GradeCalculationInput input);
}

public class GradeCalculationInput
{
    public uint MaxScore { get; set; }
    public float? FailThreshold { get; set; }
    public float? SuccessThreshold { get; set; }
    public float StudentScoreWeight { get; set; }
    public float? PenaltyPerDay { get; set; }
    public int MaxDays { get; set; }
    public DateTime? Deadline { get; set; }
    public DateTime SolutionTimestamp { get; set; }

    public IReadOnlyList<Criterion> Criteria { get; set; } = new List<Criterion>();
    public EvaluationInput TeacherEvaluation { get; set; } = new();
    public IReadOnlyList<EvaluationInput> SelfEvaluations { get; set; } = new List<EvaluationInput>();
}

public class EvaluationInput
{
    public IReadOnlyList<WeightedValueInput> WeightedValues { get; set; } = new List<WeightedValueInput>();
    public IReadOnlyList<ToggledValueInput> ToggledValues { get; set; } = new List<ToggledValueInput>();
}

public record WeightedValueInput(Guid CriterionId, float Score);

public record ToggledValueInput(Guid CriterionId, bool Enabled);

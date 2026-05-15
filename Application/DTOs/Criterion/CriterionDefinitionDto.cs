using GoogleClass.DTOs.Common;

namespace Application.DTOs.Criterion;

public class CriterionDefinitionDto
{
    public Guid? Id { get; set; }
    public CriterionTypeDto Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public int OrderIndex { get; set; }

    public float? MaxScore { get; set; }
    public float? Weight { get; set; }

    public float? Threshold { get; set; }
    public float? Score { get; set; }
    public CriterionDirection? Direction { get; set; }

    public float? MaxAllowedScore { get; set; }
}

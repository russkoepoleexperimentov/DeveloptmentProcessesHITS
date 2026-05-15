namespace Application.DTOs.Grading;

public class GradeBreakdownDto
{
    public float BaseTeacherScore { get; set; }
    public float? BaseStudentScore { get; set; }
    public float BaseScore { get; set; }
    public float AfterQualityCoefficient { get; set; }
    public float LatePenalty { get; set; }
    public float AfterLatePenalty { get; set; }
    public float AfterBlocking { get; set; }
    public float FinalScore { get; set; }
    public int ExpiredDays { get; set; }
    public bool ThresholdApplied { get; set; }
    public string? ThresholdReason { get; set; }
}

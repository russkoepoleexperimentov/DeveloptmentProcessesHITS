using Application.DTOs.Grading;
using Application.Services.Interfaces;
using Domain.Models.Criteria;
using GoogleClass.DTOs.Common;

namespace Application.Services.Implementations;

public class GradeCalculator : IGradeCalculator
{
    public GradeBreakdownDto Calculate(GradeCalculationInput input)
    {
        var result = new GradeBreakdownDto();

        result.BaseTeacherScore = ComputeBase(input.TeacherEvaluation, input.Criteria);

        float baseScore;
        if (input.StudentScoreWeight <= 0f || input.SelfEvaluations.Count == 0)
        {
            baseScore = result.BaseTeacherScore;
            result.BaseStudentScore = null;
        }
        else
        {
            var studentScores = input.SelfEvaluations
                .Select(e => ComputeBase(e, input.Criteria))
                .ToList();
            var baseStudent = studentScores.Count > 0 ? studentScores.Average() : 0f;
            result.BaseStudentScore = baseStudent;

            var w = Math.Clamp(input.StudentScoreWeight, 0f, 1f);
            baseScore = result.BaseTeacherScore * (1f - w) + baseStudent * w;
        }
        result.BaseScore = baseScore;

        var score = baseScore;

        if (input.MaxScore > 0)
        {
            var ratio = baseScore / input.MaxScore;
            foreach (var qc in input.Criteria.OfType<QualityCoefficient>())
            {
                switch (qc.Direction)
                {
                    case CriterionDirection.Add when ratio > qc.Threshold:
                        score += qc.Score;
                        break;
                    case CriterionDirection.Subtract when ratio < qc.Threshold:
                        score -= qc.Score;
                        break;
                }
            }
        }
        result.AfterQualityCoefficient = score;

        result.ExpiredDays = 0;
        result.LatePenalty = 0f;
        if (input.PenaltyPerDay.HasValue && input.Deadline.HasValue)
        {
            var diff = input.SolutionTimestamp - input.Deadline.Value;
            var expired = diff.Days;
            if (expired > 0)
            {
                var cap = Math.Min(expired, input.MaxDays);
                result.ExpiredDays = expired;
                result.LatePenalty = input.PenaltyPerDay.Value * cap;
                score -= result.LatePenalty;
            }
        }
        result.AfterLatePenalty = score;

        var teacherToggled = input.TeacherEvaluation.ToggledValues
            .ToDictionary(v => v.CriterionId, v => v.Enabled);
        foreach (var bm in input.Criteria.OfType<BlockingModifier>())
        {
            if (teacherToggled.TryGetValue(bm.Id, out var enabled) && enabled)
            {
                score = Math.Min(score, bm.MaxAllowedScore);
            }
        }
        result.AfterBlocking = score;

        result.ThresholdApplied = false;
        if (input.MaxScore > 0)
        {
            var ratio = score / input.MaxScore;
            if (input.FailThreshold.HasValue && ratio < input.FailThreshold.Value)
            {
                score = 0f;
                result.ThresholdApplied = true;
                result.ThresholdReason = "failThreshold";
            }
            else if (input.SuccessThreshold.HasValue && ratio > input.SuccessThreshold.Value)
            {
                score = input.MaxScore;
                result.ThresholdApplied = true;
                result.ThresholdReason = "successThreshold";
            }
        }

        score = Math.Max(0f, score);
        if (input.MaxScore > 0)
            score = Math.Min(score, input.MaxScore);

        result.FinalScore = score;
        return result;
    }

    private static float ComputeBase(EvaluationInput eval, IReadOnlyList<Criterion> criteria)
    {
        var weightedById = eval.WeightedValues.ToDictionary(v => v.CriterionId, v => v.Score);
        var toggledById = eval.ToggledValues.ToDictionary(v => v.CriterionId, v => v.Enabled);

        var weighted = 0f;
        foreach (var wc in criteria.OfType<WeightedCriterion>())
        {
            if (weightedById.TryGetValue(wc.Id, out var s))
                weighted += wc.Weight * s;
        }

        var bp = 0f;
        foreach (var bpc in criteria.OfType<BonusPenaltyCriterion>())
        {
            if (toggledById.TryGetValue(bpc.Id, out var enabled) && enabled)
            {
                bp += bpc.Direction == CriterionDirection.Add ? bpc.Score : -bpc.Score;
            }
        }

        return weighted + bp;
    }
}

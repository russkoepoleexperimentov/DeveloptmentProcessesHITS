using Application.DTOs.Criterion;
using Application.DTOs.Grading;
using Application.Services.Interfaces;
using Common.Exceptions;
using Domain.Models.Criteria;

namespace Application.Services.Implementations;

internal static class CriterionMapper
{
    public static Criterion ToEntity(CriterionDefinitionDto dto, Guid postId)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new BadRequestException("Criterion title is required");

        Criterion entity = dto.Type switch
        {
            CriterionTypeDto.Weighted => new WeightedCriterion
            {
                MaxScore = dto.MaxScore ?? throw new BadRequestException("MaxScore is required for weighted criterion"),
                Weight = dto.Weight ?? throw new BadRequestException("Weight is required for weighted criterion")
            },
            CriterionTypeDto.Quality => new QualityCoefficient
            {
                Threshold = dto.Threshold ?? throw new BadRequestException("Threshold is required for quality coefficient"),
                Score = dto.Score ?? throw new BadRequestException("Score is required for quality coefficient"),
                Direction = dto.Direction ?? throw new BadRequestException("Direction is required for quality coefficient")
            },
            CriterionTypeDto.BonusPenalty => new BonusPenaltyCriterion
            {
                Score = dto.Score ?? throw new BadRequestException("Score is required for bonus/penalty"),
                Direction = dto.Direction ?? throw new BadRequestException("Direction is required for bonus/penalty")
            },
            CriterionTypeDto.Blocking => new BlockingModifier
            {
                MaxAllowedScore = dto.MaxAllowedScore ?? throw new BadRequestException("MaxAllowedScore is required for blocking modifier")
            },
            _ => throw new BadRequestException($"Unknown criterion type: {dto.Type}")
        };

        entity.Id = dto.Id ?? Guid.NewGuid();
        entity.Title = dto.Title;
        entity.PostId = postId;
        entity.OrderIndex = dto.OrderIndex;
        entity.CreatedDate = DateTime.UtcNow;
        entity.UpdatedDate = DateTime.UtcNow;
        return entity;
    }

    public static CriterionDto ToDto(Criterion entity)
    {
        var dto = new CriterionDto
        {
            Id = entity.Id,
            Title = entity.Title,
            OrderIndex = entity.OrderIndex
        };

        switch (entity)
        {
            case WeightedCriterion w:
                dto.Type = CriterionTypeDto.Weighted;
                dto.MaxScore = w.MaxScore;
                dto.Weight = w.Weight;
                break;
            case QualityCoefficient q:
                dto.Type = CriterionTypeDto.Quality;
                dto.Threshold = q.Threshold;
                dto.Score = q.Score;
                dto.Direction = q.Direction;
                break;
            case BonusPenaltyCriterion bp:
                dto.Type = CriterionTypeDto.BonusPenalty;
                dto.Score = bp.Score;
                dto.Direction = bp.Direction;
                break;
            case BlockingModifier bm:
                dto.Type = CriterionTypeDto.Blocking;
                dto.MaxAllowedScore = bm.MaxAllowedScore;
                break;
        }

        return dto;
    }

    public static EvaluationInput ToCalculatorInput(EvaluationDto? dto)
    {
        if (dto == null)
            return new EvaluationInput();

        return new EvaluationInput
        {
            WeightedValues = dto.WeightedValues
                .Select(v => new WeightedValueInput(v.CriterionId, v.Score))
                .ToList(),
            ToggledValues = dto.ToggledValues
                .Select(v => new ToggledValueInput(v.CriterionId, v.Enabled))
                .ToList()
        };
    }

    public static EvaluationDto ToEvaluationDto(
        IEnumerable<WeightedCriterionValue> weighted,
        IEnumerable<ToggledCriterionValue> toggled)
    {
        return new EvaluationDto
        {
            WeightedValues = weighted.Select(v => new WeightedValueDto
            {
                CriterionId = v.CriterionId,
                Score = v.Score
            }).ToList(),
            ToggledValues = toggled.Select(v => new ToggledValueDto
            {
                CriterionId = v.CriterionId,
                Enabled = v.Enabled
            }).ToList()
        };
    }
}

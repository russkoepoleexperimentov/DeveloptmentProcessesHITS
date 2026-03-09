using Application.DTOs.Post;
using AutoMapper;
using Domain.Models;
using GoogleClass.Models;

public class PostMappingProfile : Profile
{
    public PostMappingProfile()
    {
        CreateMap<CreateUpdatePostDto, RegularPost>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CourseId, opt => opt.Ignore())
            .ForMember(dest => dest.AuthorId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore());

        CreateMap<CreateUpdatePostDto, Assignment>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CourseId, opt => opt.Ignore())
            .ForMember(dest => dest.AuthorId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.TaskType,
                opt => opt.MapFrom(src => src.TaskType.HasValue ? src.TaskType.Value.ToString() : null));
    }
}
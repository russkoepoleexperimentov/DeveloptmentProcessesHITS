using Application.DTOs;
using Application.DTOs.Auth;
using AutoMapper;
using Domain.Models;
using GoogleClass.DTOs.Auth;
using GoogleClass.DTOs.User;
using GoogleClass.Models;

namespace Application.Profiles
{
    public class UserMapProfile : Profile
    {
        public UserMapProfile()
        {

            CreateMap<UserRegisterDto, User>();

            CreateMap<User, UserDto>();

            CreateMap<UserUpdateDto, User>()
                .ForAllMembers(opts =>
                    opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<UserChangePassword, User>();

        }
    }
}
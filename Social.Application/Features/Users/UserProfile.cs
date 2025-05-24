using AutoMapper;
using Social.Application.Features.Comments.DTOs;
using Social.Application.Features.Like.DTOs;
using Social.Application.Features.Posts.DTOs;
using Social.Application.Features.Users.DTOs;
using Social.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Users
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<User, UserDto>().ReverseMap();
            CreateMap<CreateUserRequest, User>()
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.IsVerified, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.IsPrivate, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.FollowersCount, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.FollowingCount, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.PostsCount, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.Posts, opt => opt.Ignore())
                .ForMember(dest => dest.Followers, opt => opt.Ignore())
                .ForMember(dest => dest.Following, opt => opt.Ignore()).ReverseMap();

            CreateMap<UpdateUserDto, User>().ReverseMap();
            CreateMap<PostUserDto, User>().ReverseMap();
            CreateMap<CommetUserDto, User>().ReverseMap();
            CreateMap<LikeUserDto, User>().ReverseMap();
        }
    }
}

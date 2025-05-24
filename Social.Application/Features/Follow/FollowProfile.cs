using AutoMapper;
using Social.Application.Features.Followers.DTOs;
using Social.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Social.Application.Features.Follow
{
    public class FollowProfile : Profile
    {
        public FollowProfile()
        {
            //CreateMap<Follower, FollowerDto>()
            //    .ForMember(dest => dest.Follower, opt => opt.MapFrom(src => src.FollowerUser))
            //    .ForMember(dest => dest.Following, opt => opt.MapFrom(src => src.FollowingUser));

            CreateMap<CreateFollowerRequest, Follower>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.FollowerId, opt => opt.Ignore())
                .ForMember(dest => dest.FollowerUser, opt => opt.Ignore())
                .ForMember(dest => dest.FollowingUser, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

            CreateMap<FollowerDto, Follower>().ReverseMap();
            CreateMap<Follower, FollowerDto>()
                    .ForMember(dest => dest.Follower, opt => opt.MapFrom(src => new UserSummaryDto
                    {
                        Id = src.FollowerId,
                        UserName = src.FollowerUser.UserName,
                        ProfileImageUrl = src.FollowerUser.ProfileImageUrl
                    }))
                    .ForMember(dest => dest.Following, opt => opt.MapFrom(src => new UserSummaryDto
                    {
                        Id = src.FollowingId,
                        UserName = src.FollowingUser.UserName,
                        ProfileImageUrl = src.FollowingUser.ProfileImageUrl
                    })).ReverseMap();
        }
    }
}

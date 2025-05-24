using AutoMapper;
using Social.Application.Features.Like.DTOs;

namespace Social.Application.Features.Like
{
    public class LikeProfile : Profile
    {
        public LikeProfile()
        {
            CreateMap<Social.Core.Entities.Like, LikeDto>()
                .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User));

            CreateMap<LikeRequest, Social.Core.Entities.Like>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Post, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
        }
    }
}

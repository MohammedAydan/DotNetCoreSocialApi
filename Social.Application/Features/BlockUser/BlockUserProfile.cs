using AutoMapper;
using Social.Application.Features.BlockUser.DTOs;

namespace Social.Application.Features.BlockUser
{
    public class BlockUserProfile : Profile
    {
        public BlockUserProfile()
        {
            CreateMap<Social.Core.Entities.BlockUser, BlockUserDto>().ReverseMap()
                .ForMember(dest => dest.BlockedUser, opt => opt.MapFrom(src => src.BlockedUser));
        }
    }
}
using AutoMapper;
using Social.Application.Features.Posts.DTOs;
using Social.Core.Entities;

namespace Social.Application.Features.Posts
{
    public class PostProfile : Profile
    {
        public PostProfile()
        {
            CreateMap<Post, PostDto>()
                .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User))
                .ForMember(dest => dest.Media, opt => opt.MapFrom(src => src.Media))
                .ReverseMap();

            CreateMap<CreatePostRequest, Post>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.LikesCount, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.ShareingsCount, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.CommentsCount, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.Media, opt => opt.Ignore())
                .ForMember(dest => dest.Likes, opt => opt.Ignore())
                .ForMember(dest => dest.Comments, opt => opt.Ignore()).ReverseMap();

            CreateMap<Media, MediaDto>().ReverseMap();
            CreateMap<CreateMediaRequest, Media>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Post, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow)).ReverseMap();

            CreateMap<UpdatePostRequest, Post>().ReverseMap();
            CreateMap<SharePostRequest, Post>().ReverseMap();

        }
    }
}

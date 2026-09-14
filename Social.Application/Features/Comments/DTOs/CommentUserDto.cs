namespace Social.Application.Features.Comments.DTOs
{
    public class CommentUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? CoverImageUrl { get; set; }
        public bool IsVerified { get; set; }
        public bool IsPrivate { get; set; }

        // roles
        public List<string> Roles { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; }
    }

    [Obsolete("Use CommentUserDto instead.")]
    public class CommetUserDto : CommentUserDto
    {
    }
}

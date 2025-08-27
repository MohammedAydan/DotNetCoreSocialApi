using Social.Core.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class RefreshToken
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Token { get; set; } = string.Empty;

    public DateTime Expires { get; set; } = DateTime.UtcNow.AddDays(7);

    public bool IsExpired => DateTime.UtcNow >= Expires;

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public DateTime? Revoked { get; set; }

    public bool IsActive => Revoked == null && !IsExpired;

    // Foreign key
    public string UserId { get; set; } = string.Empty;
    [ForeignKey("UserId")]
    public User User { get; set; } = null!;
}

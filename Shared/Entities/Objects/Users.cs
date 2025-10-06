using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Objects;

[Table("users")]
public class Users
{
    [Key, Column("id")] public uint Id { get; set; }

    [Required, Column("role_id")] public int RoleId { get; set; }
    public Roles Role { get; set; } = null!;

    [Required, Column("username"), MaxLength(255)] public string Username { get; set; } = null!;

    [Required, Column("password_hash"), MaxLength(255)] public string PasswordHash { get; set; } = null!;

    [Column("is_active")] public bool IsActive { get; set; } = true;

    [Column("is_deleted")] public bool IsDeleted { get; set; } = false;

    [Required, Column("created_at")] public DateTime CreatedAt { get; set; }

    [Column("creator_id")] public uint? CreatorId { get; set; }

    [Column("updated_at")] public DateTime? UpdatedAt { get; set; }

    [Column("updater_id")] public uint? UpdaterId { get; set; }
}

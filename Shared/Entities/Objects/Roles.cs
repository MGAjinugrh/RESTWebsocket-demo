using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Objects;

[Table("roles")]
public class Roles
{
    [Key, Column("id")] public int Id { get; set; }
    [Required, Column("name"), MaxLength(25)] public string Name { get; set; } = null!;
    [Column("description")] public string? Description { get; set; }
    [Column("is_active")] public bool IsActive { get; set; } = true;
    [Required, Column("created_at")] public DateTime CreatedAt { get; set; }
    [Column("creator_id")] public uint? CreatorId { get; set; }
    [Column("updated_at")] public DateTime? UpdatedAt { get; set; }
    [Column("updater_id")] public uint? UpdaterId { get; set; }
}

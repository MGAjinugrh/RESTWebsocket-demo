using Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Objects;

[Table("books")]
public class Books
{
    [Key, Column("id")] public uint Id { get; set; }
    [Required, Column("title"), MaxLength(255)] public string Title { get; set; } = null!;
    [Column("author"), MaxLength(255)] public string? Author { get; set; }
    [Column("date_published")] public DateTime? DatePublished { get; set; }
    [Column("summary"), MaxLength(255)] public string? Summary { get; set; }
    [Required, Column("status")] public int Status { get; set; } = (int)BookStatus.Available;
    [Column("is_active")] public bool IsActive { get; set; } = true;
    [Column("is_deleted")] public bool IsDeleted { get; set; } = false;
    [Required, Column("created_at")] public DateTime CreatedAt { get; set; }
    [Required, Column("creator_id")] public uint CreatorId { get; set; }
    [Column("updated_at")] public DateTime? UpdatedAt { get; set; }
    [Column("updater_id")] public uint? UpdaterId { get; set; }

}

using System.ComponentModel.DataAnnotations;

namespace WarmachineAPI.Models;

public class Army : IEntity
{
    public Guid Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public Guid FactionId { get; set; }

    [Range(0, int.MaxValue)]
    public int PointLimit { get; set; }

    public Guid? OwnerAccountId { get; set; }
}

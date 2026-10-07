using System.ComponentModel.DataAnnotations;

namespace WarmachineAPI.Models;

public class ArmyEntry : IEntity
{
    public Guid Id { get; set; }
    public Guid ArmyId { get; set; }
    public Guid UnitDefinitionId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    public Guid? UnitAttachmentId { get; set; }
}

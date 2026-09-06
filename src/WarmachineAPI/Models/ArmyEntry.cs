namespace WarmachineAPI.Models;

public class ArmyEntry : IEntity
{
    public Guid Id { get; set; }
    public Guid ArmyId { get; set; }
    public Guid UnitDefinitionId { get; set; }
    public int Quantity { get; set; } = 1;
}

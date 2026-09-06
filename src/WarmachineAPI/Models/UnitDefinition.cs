namespace WarmachineAPI.Models;

public class UnitDefinition : IEntity
{
    public Guid Id { get; set; }
    public Guid FactionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public UnitCategory Category { get; set; }
    public int PointCost { get; set; }
    public BaseSize BaseSize { get; set; } = BaseSize.Medium;
    public int Speed { get; set; }
    public int MeleeAttack { get; set; }
    public int RangedAttack { get; set; }
    public int ArcaneAttack { get; set; }
    public int Defense { get; set; }
    public int Armor { get; set; }
    public int DamageCapacity { get; set; }
    public bool IsCharacter { get; set; }
}

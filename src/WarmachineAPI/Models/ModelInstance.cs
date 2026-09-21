namespace WarmachineAPI.Models;

public class ModelInstance : IEntity
{
    public Guid Id { get; set; }
    public Guid GameSessionId { get; set; }
    public Guid ArmyEntryId { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Facing { get; set; }
    public int DamageTaken { get; set; }
    public bool IsDestroyed { get; set; }
    public List<StatusEffect> StatusEffects { get; set; } = new();
    public List<DamageColumnState> DamageGrid { get; set; } = new();
    public bool HasActivated { get; set; }
}

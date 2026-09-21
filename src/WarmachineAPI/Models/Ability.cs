namespace WarmachineAPI.Models;

public class Ability : IEntity
{
    public Guid Id { get; set; }
    public Guid UnitDefinitionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

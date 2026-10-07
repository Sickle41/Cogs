using System.ComponentModel.DataAnnotations;

namespace WarmachineAPI.Models;

public class Scenario : IEntity
{
    public Guid Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}

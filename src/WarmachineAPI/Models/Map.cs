using System.ComponentModel.DataAnnotations;

namespace WarmachineAPI.Models;

public class Map : IEntity
{
    public Guid Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(typeof(double), "0.01", "100000")]
    public double Width { get; set; } = 1219.2;

    [Range(typeof(double), "0.01", "100000")]
    public double Height { get; set; } = 1219.2;
}

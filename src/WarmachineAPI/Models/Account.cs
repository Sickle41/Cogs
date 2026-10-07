using System.ComponentModel.DataAnnotations;

namespace WarmachineAPI.Models;

public class Account : IEntity
{
    public Guid Id { get; set; }

    [Required, MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;
}

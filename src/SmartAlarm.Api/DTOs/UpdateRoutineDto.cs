using System.ComponentModel.DataAnnotations;

namespace SmartAlarm.Api.DTOs;

public class UpdateRoutineDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public List<Guid> AlarmIds { get; set; } = new();

    public bool IsActive { get; set; } = true;
}

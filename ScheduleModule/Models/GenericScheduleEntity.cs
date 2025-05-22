using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace TBD.ScheduleModule.Models;

public class GenericScheduleEntity
{
    [Key] public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Precision(1)] public DateTime? UpdatedAt { get; set; }
}
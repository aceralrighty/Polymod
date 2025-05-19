using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace TBD.Models.Entities;

public class GenericEntity
{
    [Key]
    public Guid Id { get; set; }
    [Timestamp]
    public DateTime CreatedAt { get; set; }
    [Precision(1)]
    public DateTime? UpdatedAt { get; set; }
}
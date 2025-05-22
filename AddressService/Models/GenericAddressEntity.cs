using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace TBD.AddressService.Models;

public class GenericAddressEntity
{
    [Key] public Guid Id { get; set; }
     public DateTime? CreatedAt { get; set; }
    [Precision(1)] public DateTime? UpdatedAt { get; set; }
}
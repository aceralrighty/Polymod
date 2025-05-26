using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TBD.GenericDBProperties;

public abstract class DateableObject
{
    [DisplayName("Created At")]
    [DisplayFormat(DataFormatString = "{0:d}")]
    public DateTime CreatedAt { get; set; }

    [DisplayName("Update At")] public DateTime UpdatedAt { get; set; }

    [DisplayName("Deleted At")] public DateTime? DeletedAt { get; set; } = null;
}
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TBD.GenericDBProperties;

public abstract class DateableObject
{
    [DisplayName("Created At")]
    [DisplayFormat(DataFormatString = "{0:d}")]
    public virtual DateTime CreatedAt { get; set; }

    [DisplayName("Update At")] public virtual DateTime UpdatedAt { get; set; }

    [DisplayName("Deleted At")] public virtual DateTime? DeletedAt { get; set; }
}

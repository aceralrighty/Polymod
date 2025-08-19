using System.ComponentModel.DataAnnotations;

namespace TBD.RecommendationModule.Exceptions;

public class GuidNotEmptyAttribute: ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is Guid guid)
            return guid != Guid.Empty;
        return false;
    }

}

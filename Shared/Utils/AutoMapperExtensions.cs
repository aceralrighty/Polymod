namespace TBD.Shared.Utils;

public static class AutoMapperExtensions
{
    public static IServiceCollection AddAutoMapperExtension(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(ServiceMapping));
        services.AddAutoMapper(typeof(UserAddressMapping));
        services.AddAutoMapper(typeof(UserMapping));
        services.AddAutoMapper(typeof(UserScheduleMapping));
        services.AddAutoMapper(typeof(AuthUserMapping));
        return services;
    }
}

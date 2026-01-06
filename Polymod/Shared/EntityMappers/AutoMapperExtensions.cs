namespace PolyMod.Shared.EntityMappers;

public static class AutoMapperExtensions
{
    public static IServiceCollection AddAutoMapperExtension(this IServiceCollection services)
    {
        services.AddAutoMapper(_ =>
        {
        }, typeof(ServiceMapping));
        services.AddAutoMapper(_ =>
        {
        }, typeof(UserAddressMapping));
        services.AddAutoMapper(_ =>
        {
        }, typeof(UserMapping));
        services.AddAutoMapper(_ =>
        {
        }, typeof(UserScheduleMapping));
        services.AddAutoMapper(_ =>
        {
        }, typeof(AuthUserMapping));
        return services;
    }
}

using TBD.GrpcModule.Interfaces;

namespace TBD.GrpcModule;

public static class GrpcModule
{
    public static IServiceCollection AddGrpcServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add gRPC services
        services.AddGrpc(options =>
        {
            options.EnableDetailedErrors = true;
            options.MaxReceiveMessageSize = 4 * 1024 * 1024; // 4MB
            options.MaxSendMessageSize = 4 * 1024 * 1024; // 4MB
        });

        // Register gRPC service implementations
        services.AddScoped<UserAddressGrpcService>();

        // Register gRPC client
        services.AddScoped<IUserAddressGrpcClient, UserAddressGrpcClient>();

        // Add gRPC reflection for development
        services.AddGrpcReflection();

        return services;
    }

    private static readonly string[] ServiceNames = ["UserAddressService", "ScheduleService"];

    public static WebApplication UseGrpcServices(this WebApplication app)
    {
        // Map gRPC services
        app.MapGrpcService<UserAddressGrpcService>();

        // Add gRPC reflection in development
        if (app.Environment.IsDevelopment())
        {
            app.MapGrpcReflectionService();
        }

        // Add a default route for browsers
        app.MapGet("/grpc",
            () => Results.Ok(new
            {
                message = "gRPC services are running",
                services = ServiceNames,
                endpoint = "Use a gRPC client to connect"
            }));

        return app;
    }
}

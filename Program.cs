using TBD.AddressModule;
using TBD.Data.Seeding;
using TBD.ScheduleModule;
using TBD.ServiceModule;
using TBD.Shared.Utils;
using TBD.UserModule;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddUserService(builder.Configuration);
builder.Services.AddAddressService(builder.Configuration);
builder.Services.AddScheduleModule(builder.Configuration);
builder.Services.AddScheduleModule(builder.Configuration);
builder.Services.AddServiceModule(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddAutoMapper(typeof(ServiceMapping));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Testing Only
    await DataSeeder.ReseedForTestingAsync(app.Services);
    await ScheduleSeeder.ReseedForTestingAsync(app.Services);
    await ServiceSeeder.ReseedForTestingAsync(app.Services);
    app.MapOpenApi();
}

app.UseAuthorization();
app.MapControllers();

Console.WriteLine("Starting Server😁\n");
await app.RunAsync();
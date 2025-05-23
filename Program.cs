using TBD.AddressService;
using TBD.Data.Seeding;
using TBD.ScheduleModule;
using TBD.UserModule;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddUserService(builder.Configuration);
builder.Services.AddAddressService(builder.Configuration);
builder.Services.AddScheduleModule(builder.Configuration);
builder.Services.AddScheduleModule(builder.Configuration);

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Testing Only
    await DataSeeder.ReseedForTestingAsync(app.Services);
    await ScheduleSeeder.ReseedForTestingAsync(app.Services);
    app.MapOpenApi();
}

app.UseAuthorization();
app.MapControllers();

Console.WriteLine("Starting Server");
await app.RunAsync();
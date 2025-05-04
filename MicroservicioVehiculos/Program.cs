using Microsoft.EntityFrameworkCore;
using MicroservicioVehiculos.Data;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configurar gRPC
builder.Services.AddGrpc();
//dotnet dev-certs https --trust
bool isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

builder.WebHost.ConfigureKestrel(options =>
{
    if (isDocker)
    {
        // Solo en Docker
        options.ListenAnyIP(8080, o =>
        {
            o.Protocols = HttpProtocols.Http2;
        });

        options.ListenAnyIP(8081, o =>
        {
            o.UseHttps("certs/devcert.pfx", "1234");
            o.Protocols = HttpProtocols.Http2;
        });
    }
    else
    {
        options.ListenAnyIP(7297, listenOptions =>
        {
            listenOptions.UseHttps(); // ✅ Usa el certificado por defecto o personalizado
            listenOptions.Protocols = HttpProtocols.Http2;
        });
    }
});
var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<DataContext>();

    var logger = services.GetRequiredService<ILogger<Program>>();
    int maxRetries = 3;
    int delayMs = 3000;
    int retries = 0;

    while (retries < maxRetries)
    {
        try
        {
            logger.LogInformation("Intentando aplicar migraciones...");
            context.Database.Migrate();     
            break;
        }
        catch (Exception ex)
        {
            retries++;
            logger.LogWarning(ex, $"Error al aplicar migraciones. Reintento {retries}/{maxRetries}...");
            Thread.Sleep(delayMs);
        }
    }
}




// Configura endpoints HTTP y gRPC
app.MapGrpcService<MicroservicioVehiculos.Services.VehiculoServiceImpl>();

app.MapGet("/", () => "Comunicación gRPC habilitada. Use un cliente gRPC para acceder.");


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

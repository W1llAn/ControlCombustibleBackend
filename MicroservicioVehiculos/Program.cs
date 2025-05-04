using Microsoft.EntityFrameworkCore;
using MicroservicioVehiculos.Data;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configurar gRPC
builder.Services.AddGrpc();
builder.Services.AddGrpc();
//dotnet dev-certs https --trust
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(7297, listenOptions =>
    {
        listenOptions.UseHttps(); // ✅ Usa el certificado por defecto o personalizado
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

var app = builder.Build();

// Configura endpoints HTTP y gRPC
app.MapGrpcService<MicroservicioVehiculos.Services.VehiculoServiceImpl>();

app.MapGet("/", () => "Comunicación gRPC habilitada. Use un cliente gRPC para acceder.");


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

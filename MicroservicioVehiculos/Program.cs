using Microsoft.EntityFrameworkCore;
using MicroservicioVehiculos.Data;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//jwt
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Error de autenticación: {context.Exception}");
            return Task.CompletedTask;
        },
        OnForbidden = context =>
        {
            Console.WriteLine($"Error de autenticación: {context.Response}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine($"Token validado: {context.SecurityToken}");
            return Task.CompletedTask;
        }
    };

    o.RequireHttpsMetadata = false;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)),
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidIssuer = builder.Configuration["Jwt:Issuer"],

        ClockSkew = TimeSpan.Zero
    };

});

builder.Services.AddAuthorization(options => {
    options.AddPolicy("AdministradorPolitica", policy =>
        policy.RequireAssertion(
                context =>
                context.User.HasClaim("Rol", "Administrador")
            )
        );
    options.AddPolicy("SupervisorAdministradorPolitica", policy =>
        policy.RequireAssertion(
                context =>
                context.User.HasClaim("Rol", "Supervisor") ||
                context.User.HasClaim("Rol", "Administrador")
            )
        );
});

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

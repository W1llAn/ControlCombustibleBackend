using Microservicio_Administracion.Data;
using MicroservicioAutenticacion.Controllers;
using MicroservicioAutenticacion.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine("CONNECTION: " + builder.Configuration.GetConnectionString("DefaultConnection"));

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure()));

builder.Services.AddGrpc();
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080, o => o.Protocols = HttpProtocols.Http2);

    options.ListenAnyIP(8081, o =>
    {
        o.UseHttps("certs/devcert.pfx", "1234"); // Ruta del certificado y contraseña
        o.Protocols = HttpProtocols.Http2;
    });
});

//JWT
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



builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();

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

            // Insertar roles si no existen
            if (!context.Roles.Any())
            {
                context.Roles.AddRange(
                    new Rol { nombre = "Administrador", descripcion = "Acceso total" },
                    new Rol { nombre = "Supervisor", descripcion = "Supervisa operaciones" },
                    new Rol { nombre = "Operador", descripcion = "Operación básica del sistema" }
                );
                context.SaveChanges();
            }

            // Insertar usuario administrador si no existe
            if (!context.Usuarios.Any(u => u.Nombre_usuario == "root"))
            {
                var rolAdmin = context.Roles.First(r => r.nombre == "Administrador");

                context.Usuarios.Add(new Usuario
                {
                    email = "admin@sistema.com",
                    Nombre_usuario = "root",
                    hash_contrasena = "1234",
                    rol = rolAdmin,
                });
            }
                context.SaveChanges();     // Inserta datos iniciales
                break;
        }
        catch (Exception ex)
        {
            retries++;
            logger.LogWarning(ex, $"Error al aplicar migraciones o insertar datos iniciales. Reintento {retries}/{maxRetries}...");
            Thread.Sleep(delayMs);
        }
    }
}




app.MapGrpcService<UsuariosProtoImpl>();

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

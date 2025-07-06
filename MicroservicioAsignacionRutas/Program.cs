using MicroservicioRutas.Controllers;
using MicroservicioRutas.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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
builder.Services.AddAuthorization(options =>
{
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.MapGrpcService<AsignacionRutasController>();

app.UseAuthorization();

app.MapControllers();

app.Run();

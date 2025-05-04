using MicroservicioAutenticacion.Entities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace Microservicio_Autenticación.Auth
{
    internal sealed class TokenProvider(IConfiguration configuration)
    {
        public string Create(Usuario usuario)
        {
            string secretKey = configuration["Jwt:Secret"];
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] {
                new Claim(JwtRegisteredClaimNames.Sub,usuario.id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName,usuario.Nombre_usuario.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.email.ToString()),
                new Claim("Rol",usuario.rol.nombre),

                }),
                Expires = DateTime.UtcNow.AddMinutes(configuration.GetValue<int>("Jwt:TiempoExpira")),
                SigningCredentials = credentials,
                Audience = configuration["Jwt:Audience"],
                Issuer = configuration["Jwt:Issuer"]
            };
            var handler = new JsonWebTokenHandler();
            var token = handler.CreateToken(tokenDescriptor);
            return token;

        }
    }



}

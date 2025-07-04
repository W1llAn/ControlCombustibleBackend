using Grpc.Core;
using Grpc.Net.Client;
using MicroservicioAutenticacion.Protos;

namespace MicroservicioChoferes.Controllers
{
    public class UsuariosGrpcClient
    {
        private readonly UsuariosService.UsuariosServiceClient _client;

        public UsuariosGrpcClient(string direccion)
        {
            var channel = GrpcChannel.ForAddress(direccion);
            _client = new UsuariosService.UsuariosServiceClient(channel);
        }

        public async Task<Usuario> RegistrarUsuarioAsync(string email, string nombre, string hash, int rolId, string tokenJwt)
        {
            var request = new UsuarioRegistro
            {
                Email = email,
                NombreUsuario = nombre,
                HashContrasena = hash,
                RolId = rolId
            };

            // Agregar el token JWT en los metadatos
            var metadata = new Metadata
            {
                { "Authorization", $"Bearer {tokenJwt}" }
            };

            return await _client.RegistrarUsuarioAsync(request, headers: metadata);
        }
    }
}

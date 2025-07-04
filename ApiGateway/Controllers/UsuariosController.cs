using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MicroservicioAutenticacion.Protos;
using GrpcStatusCode = Grpc.Core.StatusCode;


namespace MicroservicioPuente.Controllers
{
    [ApiController]
    [Route("Usuarios")]
    public class UsuariosController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public UsuariosController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private UsuariosService.UsuariosServiceClient CrearClienteGrpc(out Metadata metadata)
        {
            var url = _configuration["grcp:autenticacion"];

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            var channel = GrpcChannel.ForAddress(url);
            var client = new UsuariosService.UsuariosServiceClient(channel);

            metadata = new Metadata();
            var authHeader = Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length);
                metadata.Add("Authorization", $"Bearer {token}");
            }

            return client;
        }

        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar([FromBody] UsuarioRegistro request)
        {
            var cliente = CrearClienteGrpc(out var metadata);
            try
            {
                var respuesta = await cliente.RegistrarUsuarioAsync(request, headers: metadata);
                return Ok(respuesta);
            }
            catch (RpcException ex)
            {
                return HandleRpcException(ex);
            }
        }

        [HttpDelete("borrar/{id}")]
        public async Task<IActionResult> Borrar(int id)
        {
            var cliente = CrearClienteGrpc(out var metadata);
            try
            {
                await cliente.BorrarUsuarioAsync(new UsuarioBorrar { Id = id }, headers: metadata);
                return NoContent();
            }
            catch (RpcException ex)
            {
                return HandleRpcException(ex);
            }
        }

        [HttpGet("listar")]
        public async Task<IActionResult> Listar()
        {
            var cliente = CrearClienteGrpc(out var metadata);
            try
            {
                var lista = await cliente.SeleccionarUsuariosAsync(new RespuestaVacia(), headers: metadata);
                return Ok(lista.Usuarios);
            }
            catch (RpcException ex)
            {
                return HandleRpcException(ex);
            }
        }

        [HttpPut("actualizar")]
        public async Task<IActionResult> Actualizar([FromBody] UsuarioActualizar request)
        {
            var cliente = CrearClienteGrpc(out var metadata);
            try
            {
                var actualizado = await cliente.ActualizarUsuarioAsync(request, headers: metadata);
                return Ok(actualizado);
            }
            catch (RpcException ex)
            {
                return HandleRpcException(ex);
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UsuarioLogin request)
        {
            var cliente = CrearClienteGrpc(out _);
            try
            {
                var token = await cliente.LoginAsync(request);
                return Ok(token);
            }
            catch (RpcException ex)
            {
                return HandleRpcException(ex);
            }
        }

        // Función que traduce errores gRPC a respuestas REST
        private IActionResult HandleRpcException(RpcException ex)
        {
            return ex.Status.StatusCode switch
            {
                GrpcStatusCode.NotFound => NotFound(new { error = ex.Status.Detail }),
                GrpcStatusCode.InvalidArgument => BadRequest(new { error = ex.Status.Detail }),
                GrpcStatusCode.PermissionDenied => StatusCode(403, new { error = ex.Status.Detail }),
                GrpcStatusCode.Unauthenticated => Unauthorized(new { error = ex.Status.Detail }),
                _ => StatusCode(500, new { error = $"Error interno: {ex.Status.Detail}" })
            };

        }
    }
}

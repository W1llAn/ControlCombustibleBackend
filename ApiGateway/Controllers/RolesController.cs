using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MicroservicioAutenticacion.Protos;
using GrpcStatusCode = Grpc.Core.StatusCode;

namespace ApiGateway.Controllers
{
    [ApiController]
    [Route("Roles")]
    public class RolesController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public RolesController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private RolesService.RolesServiceClient CrearClienteGrpc(out Metadata metadata)
        {
            var url = _configuration["grcp:autenticacion"];

            Console.WriteLine($"URL Roles gRPC: {url}");
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            var channel = GrpcChannel.ForAddress(url);
            var client = new RolesService.RolesServiceClient(channel);

            metadata = new Metadata();
            var authHeader = Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length);
                metadata.Add("Authorization", $"Bearer {token}");
            }

            return client;
        }

        [HttpGet("listar")]
        public async Task<IActionResult> ListarRoles()
        {
            var cliente = CrearClienteGrpc(out var metadata);
            try
            {
                var respuesta = await cliente.ListarRolesAsync(new RespuestaVacia(), headers: metadata);
                return Ok(respuesta.Roles);
            }
            catch (RpcException ex)
            {
                return HandleRpcException(ex);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerRol(int id)
        {
            var cliente = CrearClienteGrpc(out var metadata);
            try
            {
                var respuesta = await cliente.ObtenerRolAsync(new RolObtener { Id = id }, headers: metadata);
                return Ok(respuesta);
            }
            catch (RpcException ex)
            {
                return HandleRpcException(ex);
            }
        }

        [HttpPost("crear")]
        [Authorize(Policy = "AdministradorPolitica")]
        public async Task<IActionResult> CrearRol([FromBody] RolCrear request)
        {
            var cliente = CrearClienteGrpc(out var metadata);
            try
            {
                var respuesta = await cliente.CrearRolAsync(request, headers: metadata);
                return Ok(respuesta);
            }
            catch (RpcException ex)
            {
                return HandleRpcException(ex);
            }
        }

        [HttpPut("actualizar")]
        [Authorize(Policy = "AdministradorPolitica")]
        public async Task<IActionResult> ActualizarRol([FromBody] RolActualizar request)
        {
            var cliente = CrearClienteGrpc(out var metadata);
            try
            {
                var respuesta = await cliente.ActualizarRolAsync(request, headers: metadata);
                return Ok(respuesta);
            }
            catch (RpcException ex)
            {
                return HandleRpcException(ex);
            }
        }

        [HttpDelete("eliminar/{id}")]
        [Authorize(Policy = "AdministradorPolitica")]
        public async Task<IActionResult> EliminarRol(int id)
        {
            var cliente = CrearClienteGrpc(out var metadata);
            try
            {
                await cliente.EliminarRolAsync(new RolEliminar { Id = id }, headers: metadata);
                return Ok(new { Exito = true });
            }
            catch (RpcException ex)
            {
                return HandleRpcException(ex);
            }
        }

        // Método reutilizable para traducir errores gRPC a REST
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

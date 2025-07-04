using Grpc.Core;
using Grpc.Net.Client;
using MicroservicioChoferes.Protos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GrpcStatusCode = Grpc.Core.StatusCode;

namespace ApiGateway.Controllers
{
    [ApiController]
    [Route("Choferes")]
    public class ChoferesController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ChoferesController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private ChoferesService.ChoferesServiceClient CrearClienteGrpc(out Metadata metadata)
        {
            var url = _configuration["grcp:choferes"];
            Console.WriteLine(url);
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            var channel = GrpcChannel.ForAddress(url);
            var client = new ChoferesService.ChoferesServiceClient(channel);

            metadata = new Metadata();
            var authHeader = Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length);
                metadata.Add("Authorization", $"Bearer {token}");
            }

            return client;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerChofer(int id)
        {
            var cliente = CrearClienteGrpc(out var metadata);
            try
            {
                var respuesta = await cliente.ObtenerChoferAsync(new ObtenerChoferRequest { Id = id }, headers: metadata);
                return Ok(respuesta);
            }
            catch (RpcException ex)
            {
                return HandleRpcException(ex);
            }
        }

        [HttpGet("listar")]
        public async Task<IActionResult> ObtenerTodosChoferes()
        {
            var cliente = CrearClienteGrpc(out var metadata);
            try
            {
                var respuesta = await cliente.ObtenerTodosChoferesAsync(new RespuestaVaciaChofer(), headers: metadata);
                return Ok(respuesta.Usuarios);
            }
            catch (RpcException ex)
            {
                return HandleRpcException(ex);
            }
        }

        [HttpPost("crear")]
        public async Task<IActionResult> CrearChofer([FromBody] CrearChoferRequest request)
        {
            var cliente = CrearClienteGrpc(out var metadata);
            try
            {
                var respuesta = await cliente.CrearChoferAsync(request, headers: metadata);
                return Ok(respuesta);
            }
            catch (RpcException ex)
            {
                return HandleRpcException(ex);
            }
        }

        [HttpPut("actualizar")]
        public async Task<IActionResult> ActualizarChofer([FromBody] ActualizarChoferRequest request)
        {
            var cliente = CrearClienteGrpc(out var metadata);
            try
            {
                var respuesta = await cliente.ActualizarChoferAsync(request, headers: metadata);
                return Ok(respuesta);
            }
            catch (RpcException ex)
            {
                return HandleRpcException(ex);
            }
        }

        [HttpDelete("eliminar/{id}")]
        public async Task<IActionResult> EliminarChofer(int id)
        {
            var cliente = CrearClienteGrpc(out var metadata);
            try
            {
                var respuesta = await cliente.EliminarChoferAsync(new EliminarChoferRequest { Id = id }, headers: metadata);
                return Ok(new { Exito = respuesta.Exito });
            }
            catch (RpcException ex)
            {
                return HandleRpcException(ex);
            }
        }

        // Método auxiliar para traducir errores gRPC a errores HTTP REST
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

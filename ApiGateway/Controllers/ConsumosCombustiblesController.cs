using Grpc.Core;
using Grpc.Net.Client;
using MicroservicioChoferes.Protos;
using MicroservicioConsumoCombustible.Protos;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers
{
    [ApiController]
    [Route("ConsumoCombustible")]
    public class ConsumoCombustibleController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ConsumoCombustibleController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private async Task<ConsumoCombustiblesService.ConsumoCombustiblesServiceClient> CrearClienteGrpc()
        {
            // 1. Obtener idUsuario desde el token
            var claim = User.Claims.FirstOrDefault(c => c.Type == "idUsuario");
            if (claim == null)
                throw new Exception("idUsuario no encontrado en el token.");

            int idUsuario = int.Parse(claim.Value);

            // 2. Obtener tipo maquinaria del microservicio de choferes
            var tipo = await ObtenerTipoMaquinariaDesdeGrpc(idUsuario);

            // 3. Seleccionar la URL del microservicio según el tipo
            var url = tipo switch
            {
                TipoMaquinaria.Pesado => _configuration["grcp:consumocombustiblePesado"],
                TipoMaquinaria.Liviano => _configuration["grcp:consumocombustibleLiviano"],
                _ => throw new Exception("Tipo de maquinaria no reconocido.")
            };

            // 4. Crear cliente gRPC
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            var channel = GrpcChannel.ForAddress(url);
            return new ConsumoCombustiblesService.ConsumoCombustiblesServiceClient(channel);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerConsumo(int id)
        {
            var client =await CrearClienteGrpc();
            var response = await client.ObtenerConsumoCombustibleIdAsync(new ObtenerConsumoCombustibleRequest { Id = id }, headers: CrearMetadataDesdeToken());
            return Ok(response);
        }

        [HttpGet("listar")]
        public async Task<IActionResult> ObtenerTodos()
        {
            var client = await CrearClienteGrpc();
            var response = await client.ObtenerTodosConsumosCombustibleAsync(new RespuestaVaciaConsumoCombustible(), headers: CrearMetadataDesdeToken());
            return Ok(response.AsignacionConsumoCombustible);
        }

        [HttpPost("crear")]
        public async Task<IActionResult> CrearConsumo([FromBody] CrearConsumoCombustibleRequest request)
        {
            var client = await CrearClienteGrpc();
            var response = await client.CrearConsumoCombustibleAsync(request, headers: CrearMetadataDesdeToken());
            return Ok(response);
        }

        [HttpPut("actualizar")]
        public async Task<IActionResult> ActualizarConsumo([FromBody] ActualizarConsumoCombustibleRequest request)
        {
            var client =await CrearClienteGrpc();
            var response = await client.ActualizarConsumoCombustibleAsync(request, headers: CrearMetadataDesdeToken());
            return Ok(response);
        }

        [HttpDelete("eliminar/{id}")]
        public async Task<IActionResult> EliminarConsumo(int id)
        {
            var client = await CrearClienteGrpc();
            var response = await client.EliminarConsumoCombustibleAsync(new EliminarConsumoCombustibleRequest { Id = id }, headers: CrearMetadataDesdeToken());
            return Ok(new { Exito = response.Exito });
        }

        private async Task<TipoMaquinaria> ObtenerTipoMaquinariaDesdeGrpc(int idUsuario)
        {
            var urlChoferes = _configuration["grcp:choferes"];
            var channel = GrpcChannel.ForAddress(urlChoferes);
            var client = new ChoferesService.ChoferesServiceClient(channel);

            try
            {
                var chofer = await client.ObtenerChoferUsuarioIdAsync(new ObtenerChoferUsuarioIdRequest { IdUsuario = idUsuario }, headers: CrearMetadataDesdeToken());

                if (chofer == null || chofer.Id == 0)
                    throw new Exception("Chofer no encontrado para el usuario.");

                return chofer.TipoMaquinaria;
            }
            catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
            {
                throw new Exception("El microservicio de choferes no encontró el chofer.");
            }
            catch (RpcException ex)
            {
                throw new Exception($"Error gRPC al consultar choferes: {ex.Status.Detail}");
            }
        }
        private Metadata CrearMetadataDesdeToken()
        {
            var metadata = new Metadata();
            var authHeader = Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length);
                metadata.Add("Authorization", $"Bearer {token}");
            }
            return metadata;
        }

    }
}

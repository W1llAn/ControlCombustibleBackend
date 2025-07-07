using Grpc.Core;
using Grpc.Net.Client;
using MicroservicioChoferes.Protos;
using MicroservicioConsumoCombustible.Protos;
using MicroservicioPuente.Controllers;
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
                MicroservicioChoferes.Protos.TipoMaquinaria.Pesado => _configuration["grcp:consumocombustiblePesado"],
                MicroservicioChoferes.Protos.TipoMaquinaria.Liviano => _configuration["grcp:consumocombustibleLiviano"],
                _ => throw new Exception("Tipo de maquinaria no reconocido.")
            };

            // 4. Crear cliente gRPC
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            var channel = GrpcChannel.ForAddress(url);
            return new ConsumoCombustiblesService.ConsumoCombustiblesServiceClient(channel);
        }
        private async Task<ConsumoCombustiblesService.ConsumoCombustiblesServiceClient> CrearClienteGrpc(MicroservicioChoferes.Protos.TipoMaquinaria tipoMaquinaria)
        {

            var url = tipoMaquinaria switch
            {
                MicroservicioChoferes.Protos.TipoMaquinaria.Pesado => _configuration["grcp:consumocombustiblePesado"],
                MicroservicioChoferes.Protos.TipoMaquinaria.Liviano => _configuration["grcp:consumocombustibleLiviano"],
                _ => throw new Exception("Tipo de maquinaria no reconocido.")
            };

            // 4. Crear cliente gRPC
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            var channel = GrpcChannel.ForAddress(url);
            return new ConsumoCombustiblesService.ConsumoCombustiblesServiceClient(channel);
        }

        [HttpGet("Pesada/{id}")]
        public async Task<IActionResult> ObtenerConsumoPesado(int id)
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "Rol").Value;
            if (claim != null && (claim.Equals("Administrador") || claim.Equals("Supervisor")))
            {
                var clientPesado = await CrearClienteGrpc(MicroservicioChoferes.Protos.TipoMaquinaria.Pesado);
                var responsePesado = await clientPesado.ObtenerConsumoCombustibleIdAsync(new ObtenerConsumoCombustibleRequest { Id = id }, headers: CrearMetadataDesdeToken());
                return Ok(responsePesado);
            }
            return Unauthorized();
        }
        [HttpGet("Liviana/{id}")]
        public async Task<IActionResult> ObtenerConsumoLiviano(int id)
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "Rol").Value;
            if (claim != null && (claim.Equals("Administrador") || claim.Equals("Supervisor")))
            {
                var clientLiviano = await CrearClienteGrpc(MicroservicioChoferes.Protos.TipoMaquinaria.Liviano);
                var responseLiviano = await clientLiviano.ObtenerConsumoCombustibleIdAsync(new ObtenerConsumoCombustibleRequest { Id = id }, headers: CrearMetadataDesdeToken());
                return Ok(responseLiviano);
            }
            return Unauthorized();
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
            var claim = User.Claims.FirstOrDefault(c => c.Type == "Rol").Value;
            Console.WriteLine("Rol: " + claim);
            if (claim != null && (claim.Equals("Administrador") || claim.Equals("Supervisor")))
            {
                var clientLiviano = await CrearClienteGrpc(MicroservicioChoferes.Protos.TipoMaquinaria.Liviano);
                var responseLiviano = await clientLiviano.ObtenerTodosConsumosCombustibleAsync(new RespuestaVaciaConsumoCombustible(), headers: CrearMetadataDesdeToken());
                var clientPesado = await CrearClienteGrpc(MicroservicioChoferes.Protos.TipoMaquinaria.Pesado);
                var responsePesado = await clientPesado.ObtenerTodosConsumosCombustibleAsync(new RespuestaVaciaConsumoCombustible(), headers: CrearMetadataDesdeToken());
                responseLiviano.AsignacionConsumoCombustible.Add(responsePesado.AsignacionConsumoCombustible);
                return Ok(responseLiviano.AsignacionConsumoCombustible);
            }
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

        [HttpPut("actualizar/Pesada")]
        public async Task<IActionResult> ActualizarConsumo([FromBody] ActualizarConsumoCombustibleRequest request)
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "Rol").Value;
            Console.WriteLine("Rol: " + claim);
            if (claim != null &&( claim.Equals("Administrador")|| claim.Equals("Supervisor")))
            {
                var client =await CrearClienteGrpc(MicroservicioChoferes.Protos.TipoMaquinaria.Pesado);
                var response = await client.ActualizarConsumoCombustibleAsync(request, headers: CrearMetadataDesdeToken());
                return Ok(response);
            }
            return Unauthorized();
        }

        [HttpPut("actualizar/Liviana")]
        public async Task<IActionResult> ActualizarConsumoLiviana([FromBody] ActualizarConsumoCombustibleRequest request)
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "Rol").Value;
            Console.WriteLine("Rol: " + claim);
            if (claim != null && (claim.Equals("Administrador") || claim.Equals("Supervisor")))
            {
                var client = await CrearClienteGrpc(MicroservicioChoferes.Protos.TipoMaquinaria.Liviano);
                var response = await client.ActualizarConsumoCombustibleAsync(request, headers: CrearMetadataDesdeToken());
                return Ok(response);
            }
            return Unauthorized();
        }

        [HttpDelete("eliminar/Liviana/{id}")]
        public async Task<IActionResult> EliminarConsumoLiviana(int id)
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "Rol").Value;
            Console.WriteLine("Rol: " + claim);
            if (claim != null && (claim.Equals("Administrador") || claim.Equals("Supervisor")))
            {
                var client = await CrearClienteGrpc(MicroservicioChoferes.Protos.TipoMaquinaria.Liviano);
                var response = await client.EliminarConsumoCombustibleAsync(new EliminarConsumoCombustibleRequest { Id = id }, headers: CrearMetadataDesdeToken());
                return Ok(new { Exito = response.Exito });
            }
            return Unauthorized();
        }
        [HttpDelete("eliminar/Pesada/{id}")]
        public async Task<IActionResult> EliminarConsumoPesada(int id)
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "Rol").Value;
            Console.WriteLine("Rol: " + claim);
            if (claim != null && (claim.Equals("Administrador") || claim.Equals("Supervisor")))
            {
                var client = await CrearClienteGrpc(MicroservicioChoferes.Protos.TipoMaquinaria.Pesado);
                var response = await client.EliminarConsumoCombustibleAsync(new EliminarConsumoCombustibleRequest { Id = id }, headers: CrearMetadataDesdeToken());
                return Ok(new { Exito = response.Exito });
            }
            return Unauthorized();
        }

        private async Task<MicroservicioChoferes.Protos.TipoMaquinaria> ObtenerTipoMaquinariaDesdeGrpc(int idUsuario)
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

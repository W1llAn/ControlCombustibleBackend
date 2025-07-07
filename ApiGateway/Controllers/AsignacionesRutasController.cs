using Grpc.Core;
using Grpc.Net.Client;
using MicroservicioChoferes.Protos;
using MicroservicioRutas.Protos;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers
{
    [ApiController]
    [Route("AsignacionRutas")]
    public class AsignacionRutasController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AsignacionRutasController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private AsignacionRutasService.AsignacionRutasServiceClient CrearClienteGrpc(out Metadata metadata,string tipoMaquinaria)
        {
            var url = _configuration["grcp:asignacionrutas"+tipoMaquinaria]; // debe estar en appsettings.json o secrets
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            var channel = GrpcChannel.ForAddress(url);
            var client = new AsignacionRutasService.AsignacionRutasServiceClient(channel);

            metadata = new Metadata();
            var authHeader = Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length);
                metadata.Add("Authorization", $"Bearer {token}");
            }

            return client;
        }

        private async Task<AsignacionRutasService.AsignacionRutasServiceClient> CrearClienteGrpcAsync(int id)
        {
            var tipo = await ObtenerTipoMaquinariaDesdeGrpc(id);
            var url = tipo switch
            {
                MicroservicioChoferes.Protos.TipoMaquinaria.Pesado => _configuration["grcp:asignacionrutasPesada"],
                MicroservicioChoferes.Protos.TipoMaquinaria.Liviano => _configuration["grcp:asignacionrutasLiviana"],
                _ => throw new Exception("Tipo de maquinaria no reconocido.")
            };

            var channel = GrpcChannel.ForAddress(url);
            return new AsignacionRutasService.AsignacionRutasServiceClient(channel);
        }



        [HttpGet("Pesada/{id}")]
        public async Task<IActionResult> ObtenerAsignacionPesada(int id)
        {
            var cliente = CrearClienteGrpc(out var metadata, "Pesada");
            var respuesta = await cliente.ObtenerAsignacionRutaIdAsync(new ObtenerRutaAsignadaRequest { Id = id }, headers: metadata);
            return Ok(respuesta);
        }
        [HttpGet("Liviana/{id}")]
        public async Task<IActionResult> ObtenerAsignacionLiviana(int id)
        {
            var cliente = CrearClienteGrpc(out var metadata, "Liviana");
            var respuesta = await cliente.ObtenerAsignacionRutaIdAsync(new ObtenerRutaAsignadaRequest { Id = id }, headers: metadata);
            return Ok(respuesta);
        }

        [HttpGet("listar")]
        public async Task<IActionResult> ObtenerTodas()
        {
            var cliente = CrearClienteGrpc(out var metadata,"Pesada");
            var clienteLiviana = CrearClienteGrpc(out metadata, "Liviana");
            var respuesta = await cliente.ObtenerTodasRutasAsignadasAsync(new RespuestaVaciaAsignadaRuta(), headers: metadata);
            var respuesta2 = await clienteLiviana.ObtenerTodasRutasAsignadasAsync(new RespuestaVaciaAsignadaRuta(), headers: metadata);
            respuesta.AsignacionRuta.Add(respuesta2.AsignacionRuta);
            return Ok(respuesta.AsignacionRuta);
        }

        [HttpPost("crear")]
        public async Task<IActionResult> CrearAsignacion([FromBody] CrearAsignacionRutaRequest request)
        {
            var cliente =await CrearClienteGrpcAsync(request.ChoferId);
            var respuesta = await cliente.CrearAsignacionRutaAsync(request, headers: CrearMetadataDesdeToken());
            return Ok(respuesta);
        }

        [HttpPut("actualizar")]
        public async Task<IActionResult> ActualizarAsignacion([FromBody] ActualizarAsignacionRutaRequestApigateway request)
        {
            var cliente =await CrearClienteGrpcAsync(request.ChoferIdAnterior);
            await cliente.EliminarAsignacionRutaAsync(new EliminarAsignacionRutaRequest { Id= request.Id }, headers: CrearMetadataDesdeToken());
            cliente= await CrearClienteGrpcAsync(request.ChoferId);
            var respuesta = await cliente.CrearAsignacionRutaAsync(new CrearAsignacionRutaRequest { 
            ChoferId=request.ChoferId,
            Estado=request.Estado,
            FechaAsignacion=request.FechaAsignacion,
            RutaId=request.RutaId,
            VehiculoId = request.VehiculoId 
            },    
            headers: CrearMetadataDesdeToken());
            return Ok(respuesta);
        }

        [HttpDelete("eliminar/Liviana/{id}")]
        public async Task<IActionResult> EliminarAsignacionLiviana(int id)
        {
            var cliente = CrearClienteGrpc(out var metadata, "Liviana");
            var respuesta = await cliente.EliminarAsignacionRutaAsync(new EliminarAsignacionRutaRequest { Id = id }, headers: CrearMetadataDesdeToken());
            return Ok(new { Exito = respuesta.Exito });
        }
        [HttpDelete("eliminar/Pesada/{id}")]
        public async Task<IActionResult> EliminarAsignacionPesada(int id)
        {

            var cliente = CrearClienteGrpc(out var metadata, "Pesada");
            var respuesta = await cliente.EliminarAsignacionRutaAsync(new EliminarAsignacionRutaRequest { Id = id }, headers: CrearMetadataDesdeToken());
            return Ok(new { Exito = respuesta.Exito });
        }
        private async Task<MicroservicioChoferes.Protos.TipoMaquinaria> ObtenerTipoMaquinariaDesdeGrpc(int id)
        {
            var urlChoferes = _configuration["grcp:choferes"]; // ej: "http://localhost:5005"

            var channel = GrpcChannel.ForAddress(urlChoferes);
            var client = new ChoferesService.ChoferesServiceClient(channel);

            var response = await client.ObtenerChoferAsync(new ObtenerChoferRequest
            {
                Id = id
            }, headers: CrearMetadataDesdeToken());

            return response.TipoMaquinaria;
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

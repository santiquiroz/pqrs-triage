using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using PqrsTriage.Api.Dominio;
using PqrsTriage.Api.Infraestructura;
using PqrsTriage.Api.Proveedores;

namespace PqrsTriage.Tests;

public sealed class ManejadorFalso(Func<JsonNode?, JsonNode> responder) : HttpMessageHandler
{
    public ConcurrentQueue<JsonNode?> Cuerpos { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage solicitud, CancellationToken ct)
    {
        var cuerpo = solicitud.Content is null ? null : JsonNode.Parse(await solicitud.Content.ReadAsStringAsync(ct));
        Cuerpos.Enqueue(cuerpo);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responder(cuerpo).ToJsonString(), Encoding.UTF8, "application/json"),
        };
    }
}

public static class Apoyo
{
    private static readonly Lazy<RepositorioDatos> Datos = new(() => new RepositorioDatos(RepositorioDatos.BuscarRuta(null)));

    public static RepositorioDatos Repositorio => Datos.Value;

    public static Catalogo Catalogo => Datos.Value.CatalogoInicial;

    public static IReadOnlyList<Pregunta> Preguntas => ConstructorPreguntas.Construir(Catalogo);

    public static Pregunta Pregunta(string id) => Preguntas.First(p => p.Id == id);

    public static CasoEntrada Caso(string descripcion = "Necesito ayuda con mi contrato") =>
        new("PRUEBA-1", "Correo", "rentas@suruworks.example", new Remitente("Ana Pérez", "Tienda Uno", "ana@tiendauno.example", "Externo", null), "Asunto de prueba", descripcion,
            Creado: new DateTimeOffset(2026, 11, 11, 9, 0, 0, TimeSpan.FromHours(-5)).ToUnixTimeMilliseconds());

    public static IReadOnlyDictionary<string, Respuesta> Respuestas(
        string motivo, double confianzaTipologia = 0.9, double encaja = 0.9, double tutela = 0.02, int nivelUrgencia = 0,
        double derechoPeticion = 0.02, double riesgoLegal = 0.02, string tipo = "Petición", string activo = ConstructorPreguntas.NoIdentificado)
    {
        var tipologia = Pregunta("tipologia");
        var claves = tipologia.Opciones!.Select(o => o.Clave).ToList();
        return new Dictionary<string, Respuesta>
        {
            ["tipo"] = FabricaRespuestas.Eleccion(Pregunta("tipo"), new Dictionary<string, double> { [tipo] = 1 }, 1, true),
            ["tipologia"] = FabricaRespuestas.Eleccion(tipologia, FabricaRespuestas.Repartir(claves, motivo, confianzaTipologia), confianzaTipologia, true),
            ["encaja_catalogo"] = FabricaRespuestas.Noul(Pregunta("encaja_catalogo"), encaja, true),
            ["activo"] = FabricaRespuestas.Eleccion(Pregunta("activo"), new Dictionary<string, double> { [activo] = 1 }, 1, true),
            ["urgencia"] = FabricaRespuestas.Puntaje(Pregunta("urgencia"), FabricaRespuestas.RepartirNiveles(4, nivelUrgencia, 1), null, 1, true),
            ["tutela"] = FabricaRespuestas.Noul(Pregunta("tutela"), tutela, true),
            ["derecho_peticion"] = FabricaRespuestas.Noul(Pregunta("derecho_peticion"), derechoPeticion, true),
            ["riesgo_legal"] = FabricaRespuestas.Noul(Pregunta("riesgo_legal"), riesgoLegal, true),
        };
    }

    public static Decision Decidir(IReadOnlyDictionary<string, Respuesta> respuestas, CasoEntrada? caso = null)
    {
        var entrada = caso ?? Caso();
        var motor = new MotorEnrutamiento(new GeneradorCorreos(Repositorio.Plantillas));
        return motor.Decidir(new EntradaMotor(respuestas, entrada, Catalogo, ExtractorDatos.Extraer(entrada), entrada.Creado!.Value));
    }

    public static string MotivoConPrioridad(string prioridad, bool conResponsable = true) =>
        Catalogo.Motivos().First(m => m.Motivo.Prioridad == prioridad && (m.Grupo.Responsable is not null) == conResponsable && m.Motivo.Nombre != ConsultasCatalogo.MotivoTutela).Motivo.Nombre;
}

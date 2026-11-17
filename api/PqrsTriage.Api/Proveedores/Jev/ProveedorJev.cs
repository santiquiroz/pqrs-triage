using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using PqrsTriage.Api.Dominio;

namespace PqrsTriage.Api.Proveedores.Jev;

public sealed class ProveedorJev(HttpClient http, JevOpciones opciones) : IProveedorDecisiones
{
    private static readonly TimeSpan[] Esperas = [TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(1.5)];
    private const HttpStatusCode Sobrecargado = (HttpStatusCode)529;

    public string Id => "jev";

    public ProveedorInfo Info() => new(
        Id,
        "Jev · TypeSafe AI",
        "decision",
        opciones.Modelo,
        "Envía el texto del caso a la API de TypeSafe (EE. UU.). No entrena con datos de clientes; retención cero solo en plan empresarial.",
        JevOpciones.CostoUsdPorMillon,
        "Modelo de decisiones: responde preguntas tipadas con probabilidades calibradas.");

    public Task<string?> MotivoNoDisponibleAsync(CancellationToken ct) =>
        Task.FromResult(string.IsNullOrWhiteSpace(opciones.ClaveEfectiva) ? "Falta JEV_API_KEY (consola: console.typesafe.ai)" : null);

    public async Task<ResultadoDecisiones> DecidirAsync(IReadOnlyDictionary<string, string> estado, IReadOnlyList<Pregunta> preguntas, CancellationToken ct)
    {
        var lotes = SolicitudJev.Lotes(preguntas, opciones.MaxPreguntasPorSolicitud);
        var resultados = await Task.WhenAll(lotes.Select(l => EnviarLoteAsync(estado, l, ct)));
        var tokens = resultados.Sum(r => LectorJev.TokensEntrada(r.Respuesta));
        return new ResultadoDecisiones(
            resultados.SelectMany(r => LectorJev.Respuestas(r.Respuesta, r.Preguntas)).ToDictionary(kv => kv.Key, kv => kv.Value),
            resultados.Length,
            tokens,
            tokens * JevOpciones.CostoUsdPorMillon / 1_000_000,
            opciones.Modelo,
            resultados.Select(r => (JsonNode)r.Solicitud).ToList(),
            resultados.Select(r => r.Respuesta).ToList());
    }

    private async Task<(IReadOnlyList<Pregunta> Preguntas, JsonObject Solicitud, JsonNode Respuesta)> EnviarLoteAsync(
        IReadOnlyDictionary<string, string> estado, IReadOnlyList<Pregunta> lote, CancellationToken ct)
    {
        var solicitud = SolicitudJev.Cuerpo(opciones.Modelo, estado, lote);
        var respuesta = await PostConReintentosAsync(solicitud, ct);
        return (lote, solicitud, respuesta);
    }

    private async Task<JsonNode> PostConReintentosAsync(JsonObject solicitud, CancellationToken ct)
    {
        for (var intento = 0; ; intento++)
        {
            using var mensaje = Mensaje(solicitud);
            using var respuesta = await http.SendAsync(mensaje, ct);
            if (DebeReintentar(respuesta, intento))
            {
                await Task.Delay(Espera(respuesta, intento), ct);
                continue;
            }
            return await LeerAsync(respuesta, ct);
        }
    }

    private HttpRequestMessage Mensaje(JsonObject solicitud)
    {
        var mensaje = new HttpRequestMessage(HttpMethod.Post, $"{opciones.BaseUrl.TrimEnd('/')}{opciones.Ruta}")
        {
            Content = JsonContent.Create(solicitud),
        };
        mensaje.Headers.Authorization = new AuthenticationHeaderValue("Bearer", opciones.ClaveEfectiva);
        return mensaje;
    }

    private static bool DebeReintentar(HttpResponseMessage respuesta, int intento) =>
        intento < Esperas.Length && respuesta.StatusCode is HttpStatusCode.TooManyRequests or Sobrecargado;

    private static TimeSpan Espera(HttpResponseMessage respuesta, int intento) =>
        respuesta.Headers.RetryAfter?.Delta ?? Esperas[intento];

    private static async Task<JsonNode> LeerAsync(HttpResponseMessage respuesta, CancellationToken ct)
    {
        var texto = await respuesta.Content.ReadAsStringAsync(ct);
        if (respuesta.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new ErrorProveedor("Clave de Jev inválida o sin permiso");
        if (!respuesta.IsSuccessStatusCode)
            throw new ErrorProveedor($"Jev respondió {(int)respuesta.StatusCode}: {Texto.Recortar(texto, 300)}");
        return JsonNode.Parse(texto) ?? throw new ErrorProveedor("Jev devolvió una respuesta vacía");
    }
}

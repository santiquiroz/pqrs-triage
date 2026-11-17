using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using PqrsTriage.Api.Dominio;

namespace PqrsTriage.Api.Proveedores.Llm;

public sealed class ProveedorLlm(HttpClient http, LlmOpciones opciones) : IProveedorDecisiones
{
    private const int Intentos = 2;

    public string Id => opciones.Id;

    public ProveedorInfo Info() => new(
        Id,
        opciones.Nombre ?? (opciones.EsOllamaLocal ? "LLM generativo (Ollama local)" : "LLM generativo (remoto)"),
        "llm",
        opciones.Modelo,
        opciones.EsOllamaLocal
            ? "Corre en este equipo con Ollama: ningún dato sale de SuruWorks."
            : "Envía el caso al endpoint configurado (por ejemplo Azure OpenAI en la suscripción de SuruWorks).",
        opciones.CostoUsdPorMillonTokens,
        "LLM con salida JSON estructurada; la confianza es autodeclarada, no calibrada.");

    public async Task<string?> MotivoNoDisponibleAsync(CancellationToken ct)
    {
        if (!opciones.EsOllamaLocal) return string.IsNullOrWhiteSpace(opciones.ClaveEfectiva) ? $"Falta {opciones.VariableClave}" : null;
        var etiquetas = await http.GetFromJsonAsync<JsonNode>($"{opciones.RaizOllama}/api/tags", ct);
        var modelos = etiquetas?["models"]?.AsArray().Select(m => m?["name"]?.GetValue<string>()) ?? [];
        return modelos.Contains(opciones.Modelo) ? null : $"Ollama no tiene el modelo {opciones.Modelo}";
    }

    public async Task<ResultadoDecisiones> DecidirAsync(IReadOnlyDictionary<string, string> estado, IReadOnlyList<Pregunta> preguntas, CancellationToken ct)
    {
        var solicitud = SolicitudLlm.Cuerpo(opciones.Modelo, estado, preguntas, opciones.ReasoningEffort);
        var crudas = new List<JsonNode>();
        for (var intento = 1; intento <= Intentos; intento++)
        {
            var respuesta = await EnviarAsync(solicitud, ct);
            crudas.Add(respuesta);
            var contenido = LectorLlm.Contenido(respuesta);
            if (contenido is not null) return Resultado(preguntas, solicitud, crudas, contenido);
        }
        throw new ErrorProveedor("El LLM no devolvió JSON válido");
    }

    private ResultadoDecisiones Resultado(IReadOnlyList<Pregunta> preguntas, JsonObject solicitud, List<JsonNode> crudas, JsonObject contenido)
    {
        var tokens = crudas.Sum(LectorLlm.TokensEntrada);
        return new ResultadoDecisiones(
            LectorLlm.Respuestas(contenido, preguntas),
            crudas.Count,
            tokens,
            tokens * opciones.CostoUsdPorMillonTokens / 1_000_000,
            opciones.Modelo,
            [solicitud],
            crudas);
    }

    private async Task<JsonNode> EnviarAsync(JsonObject solicitud, CancellationToken ct)
    {
        using var mensaje = new HttpRequestMessage(HttpMethod.Post, $"{opciones.BaseUrl.TrimEnd('/')}/chat/completions")
        {
            Content = JsonContent.Create(solicitud),
        };
        AgregarClave(mensaje);
        using var respuesta = await http.SendAsync(mensaje, ct);
        var texto = await respuesta.Content.ReadAsStringAsync(ct);
        if (!respuesta.IsSuccessStatusCode) throw new ErrorProveedor($"El LLM respondió {(int)respuesta.StatusCode}: {Texto.Recortar(texto, 300)}");
        return JsonNode.Parse(texto) ?? throw new ErrorProveedor("El LLM devolvió una respuesta vacía");
    }

    private void AgregarClave(HttpRequestMessage mensaje)
    {
        var clave = opciones.ClaveEfectiva;
        if (string.IsNullOrWhiteSpace(clave)) return;
        if (opciones.HeaderApiKey.Equals("api-key", StringComparison.OrdinalIgnoreCase)) mensaje.Headers.Add("api-key", clave);
        else mensaje.Headers.Authorization = new AuthenticationHeaderValue("Bearer", clave);
    }
}

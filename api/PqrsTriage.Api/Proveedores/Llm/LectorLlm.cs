using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using PqrsTriage.Api.Dominio;

namespace PqrsTriage.Api.Proveedores.Llm;

public static partial class LectorLlm
{
    public static JsonObject? Contenido(JsonNode respuesta)
    {
        var contenido = respuesta["choices"]?[0]?["message"]?["content"]?.GetValue<string>() ?? "";
        return IntentarParsear(QuitarPensamiento(contenido));
    }

    public static int TokensEntrada(JsonNode respuesta) => respuesta["usage"]?["prompt_tokens"]?.GetValue<int>() ?? 0;

    public static IReadOnlyDictionary<string, Respuesta> Respuestas(JsonObject contenido, IReadOnlyList<Pregunta> preguntas) =>
        preguntas.ToDictionary(p => p.Id, p => Convertir(p, contenido[p.Id]));

    private static Respuesta Convertir(Pregunta pregunta, JsonNode? nodo) => pregunta.Tipo switch
    {
        TipoPregunta.Choice => Eleccion(pregunta, nodo),
        TipoPregunta.Score => Puntaje(pregunta, nodo),
        _ => FabricaRespuestas.Noul(pregunta, Decimal(nodo?["probabilidad"], 0.5), false),
    };

    private static Respuesta Eleccion(Pregunta pregunta, JsonNode? nodo)
    {
        var claves = (pregunta.Opciones ?? []).Select(o => o.Clave).ToList();
        var elegida = nodo?["eleccion"]?.GetValue<string>();
        if (elegida is null || !claves.Contains(elegida)) throw new ErrorProveedor($"El LLM eligió una opción inválida en «{pregunta.Id}»");
        var confianza = Math.Clamp(Decimal(nodo?["confianza"], 0.5), 0, 1);
        return FabricaRespuestas.Eleccion(pregunta, FabricaRespuestas.Repartir(claves, elegida, confianza), confianza, false);
    }

    private static Respuesta Puntaje(Pregunta pregunta, JsonNode? nodo)
    {
        var cantidad = pregunta.Niveles?.Count ?? 1;
        var nivel = Math.Clamp((int)Decimal(nodo?["nivel"], 0), 0, cantidad - 1);
        var confianza = Math.Clamp(Decimal(nodo?["confianza"], 0.5), 0, 1);
        return FabricaRespuestas.Puntaje(pregunta, FabricaRespuestas.RepartirNiveles(cantidad, nivel, confianza), nivel, confianza, false);
    }

    private static double Decimal(JsonNode? nodo, double porDefecto) =>
        nodo?.GetValueKind() == JsonValueKind.Number ? nodo.GetValue<double>() : porDefecto;

    private static string QuitarPensamiento(string contenido) => Pensamiento().Replace(contenido, "").Trim();

    private static JsonObject? IntentarParsear(string texto)
    {
        try
        {
            return JsonNode.Parse(texto) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    [GeneratedRegex(@"<think>.*?</think>", RegexOptions.Singleline)]
    private static partial Regex Pensamiento();
}

using System.Text.Json;
using System.Text.Json.Nodes;
using PqrsTriage.Api.Dominio;

namespace PqrsTriage.Api.Proveedores.Jev;

public static class LectorJev
{
    public static IReadOnlyDictionary<string, Respuesta> Respuestas(JsonNode raiz, IReadOnlyList<Pregunta> preguntas)
    {
        var respuestas = Respuestas(raiz) ?? throw new ErrorProveedor("La respuesta de Jev no trae «answers»");
        return preguntas.ToDictionary(p => p.Id, p => Convertir(p, respuestas[p.Id] ?? throw new ErrorProveedor($"Jev no respondió la pregunta «{p.Id}»")));
    }

    public static int TokensEntrada(JsonNode raiz)
    {
        var uso = raiz["usage"] ?? raiz["data"]?["result"]?["usage"];
        return Entero(uso?["input_tokens"]);
    }

    private static JsonObject? Respuestas(JsonNode raiz) =>
        (raiz["answers"] ?? raiz["data"]?["result"]?["answers"]) as JsonObject;

    private static Respuesta Convertir(Pregunta pregunta, JsonNode respuesta) => pregunta.Tipo switch
    {
        TipoPregunta.Choice => Eleccion(pregunta, respuesta),
        TipoPregunta.Score => Puntaje(pregunta, respuesta),
        _ => FabricaRespuestas.Noul(pregunta, ProbabilidadNoul(respuesta["noul"]), true),
    };

    private static Respuesta Eleccion(Pregunta pregunta, JsonNode respuesta)
    {
        var elegida = respuesta["choice"]?.GetValue<string>() ?? "";
        var probabilidades = Diccionario(respuesta["probabilities"]);
        var porClave = probabilidades.Count > 0 ? probabilidades : new Dictionary<string, double> { [elegida] = 1 };
        return FabricaRespuestas.Eleccion(pregunta, porClave, Decimal(respuesta["confidence"]), true) with { Eleccion = elegida };
    }

    private static Respuesta Puntaje(Pregunta pregunta, JsonNode respuesta)
    {
        var cantidad = pregunta.Niveles?.Count ?? 0;
        var probabilidades = Diccionario(respuesta["probabilities"]);
        var porNivel = Enumerable.Range(0, cantidad).Select(i => probabilidades.GetValueOrDefault(i.ToString())).ToList();
        return FabricaRespuestas.Puntaje(pregunta, porNivel, Decimal(respuesta["score"]), Decimal(respuesta["confidence"]), true);
    }

    private static double ProbabilidadNoul(JsonNode? valor) => valor?.GetValueKind() switch
    {
        JsonValueKind.True => 1,
        JsonValueKind.False => 0,
        JsonValueKind.Number => valor.GetValue<double>(),
        _ => 0.5,
    };

    private static Dictionary<string, double> Diccionario(JsonNode? nodo) =>
        nodo is JsonObject objeto
            ? objeto.Where(kv => kv.Value is not null).ToDictionary(kv => kv.Key, kv => kv.Value!.GetValue<double>())
            : [];

    private static double? Decimal(JsonNode? nodo) => nodo?.GetValueKind() == JsonValueKind.Number ? nodo.GetValue<double>() : null;

    private static int Entero(JsonNode? nodo) => nodo?.GetValueKind() == JsonValueKind.Number ? (int)nodo.GetValue<double>() : 0;
}

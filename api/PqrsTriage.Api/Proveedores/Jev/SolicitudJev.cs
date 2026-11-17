using System.Text.Json;
using System.Text.Json.Nodes;
using PqrsTriage.Api.Dominio;

namespace PqrsTriage.Api.Proveedores.Jev;

public static class SolicitudJev
{
    public static IReadOnlyList<IReadOnlyList<Pregunta>> Lotes(IReadOnlyList<Pregunta> preguntas, int maximo) =>
        maximo <= 0 ? [preguntas] : preguntas.Chunk(maximo).Select(l => (IReadOnlyList<Pregunta>)l).ToList();

    public static JsonObject Cuerpo(string modelo, IReadOnlyDictionary<string, string> estado, IReadOnlyList<Pregunta> preguntas)
    {
        var preguntasJson = new JsonObject();
        foreach (var pregunta in preguntas) preguntasJson[pregunta.Id] = PreguntaJson(pregunta);
        return new JsonObject
        {
            ["model"] = modelo,
            ["state"] = JsonSerializer.SerializeToNode(estado),
            ["questions"] = preguntasJson,
        };
    }

    private static JsonObject PreguntaJson(Pregunta pregunta)
    {
        var json = new JsonObject
        {
            ["type"] = pregunta.Tipo.ToString().ToLowerInvariant(),
            ["instructions"] = pregunta.Instrucciones,
        };
        var criterios = Criterios(pregunta);
        if (criterios is not null) json["criteria"] = criterios;
        return json;
    }

    private static JsonNode? Criterios(Pregunta pregunta) => pregunta.Tipo switch
    {
        TipoPregunta.Choice => CriteriosEleccion(pregunta.Opciones ?? []),
        TipoPregunta.Score => new JsonArray((pregunta.Niveles ?? []).Select(n => (JsonNode)JsonValue.Create(n)!).ToArray()),
        _ => pregunta.Criterios is null ? null : new JsonObject { ["true"] = pregunta.Criterios.Verdadero, ["false"] = pregunta.Criterios.Falso },
    };

    private static JsonObject CriteriosEleccion(IReadOnlyList<Opcion> opciones)
    {
        var criterios = new JsonObject();
        foreach (var opcion in opciones) criterios[opcion.Clave] = opcion.Descripcion;
        return criterios;
    }
}

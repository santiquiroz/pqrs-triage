using System.Text.Json.Nodes;
using PqrsTriage.Api.Dominio;

namespace PqrsTriage.Api.Proveedores.Reglas;

public sealed class ProveedorReglas : IProveedorDecisiones
{
    private const string Modelo = "palabras-clave-v1";

    public string Id => "reglas";

    public ProveedorInfo Info() => new(
        Id,
        "Reglas (sin IA)",
        "reglas",
        Modelo,
        "Corre dentro de la API: ningún dato sale de SuruWorks.",
        0,
        "Línea base: palabras clave y expresiones regulares, sin modelo.");

    public Task<string?> MotivoNoDisponibleAsync(CancellationToken ct) => Task.FromResult<string?>(null);

    public Task<ResultadoDecisiones> DecidirAsync(IReadOnlyDictionary<string, string> estado, IReadOnlyList<Pregunta> preguntas, CancellationToken ct)
    {
        var texto = Texto.Normalizar($"{estado.GetValueOrDefault("asunto")}\n{estado.GetValueOrDefault("mensaje")}");
        var respuestas = preguntas.ToDictionary(p => p.Id, p => Responder(p, texto, preguntas));
        var resultado = new ResultadoDecisiones(respuestas, 0, 0, 0, Modelo, [], [JsonValue.Create("Sin llamadas externas: reglas locales.")!]);
        return Task.FromResult(resultado);
    }

    private static Respuesta Responder(Pregunta pregunta, string texto, IReadOnlyList<Pregunta> preguntas) => pregunta.Id switch
    {
        "tipo" => ReglasPorPregunta.Tipo(pregunta, texto),
        "tipologia" => ReglasTipologia.Responder(pregunta, texto),
        "encaja_catalogo" => ReglasPorPregunta.Encaja(pregunta, texto, preguntas.FirstOrDefault(p => p.Id == "tipologia")),
        "activo" => ReglasPorPregunta.Activo(pregunta, texto),
        "urgencia" => ReglasPorPregunta.Urgencia(pregunta, texto),
        "tutela" => FabricaRespuestas.Noul(pregunta, ReglasPorPregunta.ProbabilidadTutela(texto), false),
        "derecho_peticion" => FabricaRespuestas.Noul(pregunta, ReglasPorPregunta.ProbabilidadDerechoPeticion(texto), false),
        "riesgo_legal" => FabricaRespuestas.Noul(pregunta, ReglasPorPregunta.ProbabilidadRiesgoLegal(texto), false),
        _ => ReglasPorPregunta.Uniforme(pregunta),
    };
}

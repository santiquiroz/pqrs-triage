namespace PqrsTriage.Api.Dominio;

public enum TipoPregunta { Choice, Score, Noul }

public sealed record Opcion(string Clave, string Descripcion);

public sealed record Criterios(string Verdadero, string Falso);

public sealed record Pregunta(
    string Id,
    TipoPregunta Tipo,
    string Instrucciones,
    IReadOnlyList<Opcion>? Opciones = null,
    IReadOnlyList<string>? Niveles = null,
    Criterios? Criterios = null);

public sealed record Respuesta(
    string Id,
    TipoPregunta Tipo,
    string? Eleccion,
    double? Puntaje,
    double? Probabilidad,
    double? Confianza,
    IReadOnlyDictionary<string, double> Probabilidades,
    bool ConfianzaCalibrada);

namespace PqrsTriage.Api.Dominio;

public sealed record EntradaMotor(
    IReadOnlyDictionary<string, Respuesta> Respuestas,
    CasoEntrada Caso,
    Catalogo Catalogo,
    Extraidos Extraidos,
    long CreadoMs);

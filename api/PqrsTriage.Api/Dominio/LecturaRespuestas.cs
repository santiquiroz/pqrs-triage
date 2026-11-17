namespace PqrsTriage.Api.Dominio;

public sealed record LecturaRespuestas(
    string? Tipo,
    string? Motivo,
    double ConfianzaTipologia,
    double Encaja,
    string? Activo,
    double Urgencia,
    double Tutela,
    double DerechoPeticion,
    double RiesgoLegal)
{
    public static LecturaRespuestas Desde(IReadOnlyDictionary<string, Respuesta> r) => new(
        Eleccion(r, "tipo"),
        Eleccion(r, "tipologia"),
        Confianza(r, "tipologia"),
        Probabilidad(r, "encaja_catalogo"),
        Eleccion(r, "activo"),
        r.GetValueOrDefault("urgencia")?.Puntaje ?? 0,
        Probabilidad(r, "tutela"),
        Probabilidad(r, "derecho_peticion"),
        Probabilidad(r, "riesgo_legal"));

    private static string? Eleccion(IReadOnlyDictionary<string, Respuesta> r, string id) => r.GetValueOrDefault(id)?.Eleccion;

    private static double Probabilidad(IReadOnlyDictionary<string, Respuesta> r, string id) => r.GetValueOrDefault(id)?.Probabilidad ?? 0;

    private static double Confianza(IReadOnlyDictionary<string, Respuesta> r, string id)
    {
        var respuesta = r.GetValueOrDefault(id);
        if (respuesta is null) return 0;
        return respuesta.Confianza ?? MaximaProbabilidad(respuesta);
    }

    private static double MaximaProbabilidad(Respuesta respuesta) =>
        respuesta.Probabilidades.Count == 0 ? 0 : respuesta.Probabilidades.Values.Max();
}

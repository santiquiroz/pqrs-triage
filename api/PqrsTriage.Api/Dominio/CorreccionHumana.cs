namespace PqrsTriage.Api.Dominio;

public static class CorreccionHumana
{
    public static IReadOnlyDictionary<string, Respuesta> ConMotivo(IReadOnlyDictionary<string, Respuesta> respuestas, string motivo) =>
        new Dictionary<string, Respuesta>(respuestas)
        {
            ["tipologia"] = RespuestasFijas.Eleccion("tipologia", motivo),
            ["encaja_catalogo"] = RespuestasFijas.SiNo("encaja_catalogo", true),
            ["tutela"] = RespuestasFijas.SiNo("tutela", motivo == ConsultasCatalogo.MotivoTutela),
        };
}

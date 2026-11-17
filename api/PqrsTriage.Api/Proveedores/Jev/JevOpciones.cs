namespace PqrsTriage.Api.Proveedores.Jev;

public sealed class JevOpciones
{
    public const double CostoUsdPorMillon = 0.042;

    public string BaseUrl { get; init; } = "https://api.typesafe.ai";
    public string Ruta { get; init; } = "/v1/systemone";
    public string Modelo { get; init; } = "jev-latest";
    public string? ApiKey { get; init; }
    public int MaxPreguntasPorSolicitud { get; init; }
    public int TimeoutSegundos { get; init; } = 20;

    public string? ClaveEfectiva =>
        Environment.GetEnvironmentVariable("JEV_API_KEY") is { Length: > 0 } clave ? clave : ApiKey;
}

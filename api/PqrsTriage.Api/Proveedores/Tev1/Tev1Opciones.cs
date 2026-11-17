namespace PqrsTriage.Api.Proveedores.Tev1;

public sealed class Tev1Opciones
{
    public string BaseUrl { get; init; } = "http://127.0.0.1:8091/v1";
    public string Modelo { get; init; } = "tev1-4b";
    public int MaxOpciones { get; init; } = 24;
    public int Paralelo { get; init; } = 4;
    public int TimeoutSegundos { get; init; } = 60;
}

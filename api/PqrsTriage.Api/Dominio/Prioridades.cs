namespace PqrsTriage.Api.Dominio;

public static class Prioridades
{
    public const string Tutela = "Tutela";
    public const string Urgente = "Urgente";

    private static readonly string[] Orden = ["Baja", "Media", "Alta", Urgente, Tutela];

    public static int Rango(string prioridad) => Array.IndexOf(Orden, prioridad);

    public static string Mayor(string a, string b) => Rango(a) >= Rango(b) ? a : b;

    public static string DesdeNivelUrgencia(double puntaje) => Orden[Math.Clamp((int)Math.Round(puntaje, MidpointRounding.AwayFromZero), 0, 3)];
}

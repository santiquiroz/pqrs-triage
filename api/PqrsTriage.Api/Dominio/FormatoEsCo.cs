using System.Globalization;

namespace PqrsTriage.Api.Dominio;

public static class FormatoEsCo
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CO");

    public static string Probabilidad(double valor) => $"p = {valor.ToString("0.00", Cultura)}";

    public static string Porcentaje(double valor) => $"{Math.Round(valor * 100).ToString("0", Cultura)} %";

    public static string Numero(double valor) => valor.ToString("0.##", Cultura);

    public static string Fecha(DateTimeOffset fecha) =>
        $"{fecha.ToString("d MMM yyyy", Cultura)}, {fecha.ToString("h:mm tt", Cultura)}";
}

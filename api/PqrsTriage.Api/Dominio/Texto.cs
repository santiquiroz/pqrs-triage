using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PqrsTriage.Api.Dominio;

public static partial class Texto
{
    private static readonly HashSet<string> PalabrasGenericasDeActivo =
        ["gran", "plaza", "centro", "logistico", "bodegas", "torre", "empresarial", "edificio", "bogota", "aeropuerto", "el"];

    public static string Normalizar(string? texto)
    {
        if (string.IsNullOrEmpty(texto)) return "";
        var constructor = new StringBuilder(texto.Length);
        foreach (var caracter in texto.Normalize(NormalizationForm.FormD))
            AgregarSiNoEsTilde(constructor, caracter);
        return constructor.ToString();
    }

    public static IReadOnlyList<string> Palabras(string normalizado) =>
        SeparadorPalabras().Split(normalizado).Where(p => p.Length > 0).ToList();

    public static bool ContienePalabra(string normalizado, string palabra) =>
        Regex.IsMatch(normalizado, $@"\b{Regex.Escape(palabra)}\b");

    public static string PalabraDistintivaDeActivo(string nombreActivo) =>
        Palabras(Normalizar(nombreActivo)).FirstOrDefault(p => !PalabrasGenericasDeActivo.Contains(p)) ?? Normalizar(nombreActivo);

    public static bool MencionaActivo(string normalizado, string nombreActivo) =>
        normalizado.Contains(Normalizar(nombreActivo)) || ContienePalabra(normalizado, PalabraDistintivaDeActivo(nombreActivo));

    public static string Recortar(string texto, int maximo) =>
        texto.Length <= maximo ? texto : texto[..maximo] + "…";

    private static void AgregarSiNoEsTilde(StringBuilder constructor, char caracter)
    {
        if (CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark)
            constructor.Append(char.ToLowerInvariant(caracter));
    }

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex SeparadorPalabras();
}

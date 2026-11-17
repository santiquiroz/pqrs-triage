using System.Text.RegularExpressions;

namespace PqrsTriage.Api.Dominio;

public static partial class ExtractorDatos
{
    public static Extraidos Extraer(CasoEntrada caso) =>
        Extraer($"{caso.Remitente.Nombre}\n{caso.Asunto}\n{caso.Descripcion}");

    public static Extraidos Extraer(string texto) => new(
        Coincidencias(texto, NitConPuntos(), NitSinPuntos()),
        Coincidencias(texto, Factura()),
        Coincidencias(texto, Local()).Select(CapitalizarPrimera).ToList(),
        Coincidencias(texto, Correo()),
        Coincidencias(texto, Telefono()),
        Coincidencias(texto, RadicadoJudicial()),
        Coincidencias(texto, Juzgado()).Select(j => j.Trim()).ToList());

    private static IReadOnlyList<string> Coincidencias(string texto, params Regex[] expresiones) =>
        expresiones
            .SelectMany(e => e.Matches(texto).Select(m => m.Value.Trim()))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string CapitalizarPrimera(string valor) =>
        valor.Length == 0 ? valor : char.ToUpperInvariant(valor[0]) + valor[1..];

    [GeneratedRegex(@"\b\d{1,3}(?:\.\d{3}){2}-\d\b")]
    private static partial Regex NitConPuntos();

    [GeneratedRegex(@"\b\d{9}-\d\b")]
    private static partial Regex NitSinPuntos();

    [GeneratedRegex(@"\b(?:FE|FV|FAC)-?\d{3,}\b", RegexOptions.IgnoreCase)]
    private static partial Regex Factura();

    [GeneratedRegex(@"\b(?:Local|Oficina|Bodega|Módulo|Modulo|Isla|Habitación|Habitacion)\s+[A-Z]?-?\d{1,4}\b", RegexOptions.IgnoreCase)]
    private static partial Regex Local();

    [GeneratedRegex(@"[\w.+-]+@[\w-]+(?:\.[\w-]+)+")]
    private static partial Regex Correo();

    [GeneratedRegex(@"(?:\+57\s?)?\b3\d{2}\s?\d{3}\s?\d{4}\b")]
    private static partial Regex Telefono();

    [GeneratedRegex(@"\b\d{5}-?\d{2}-?\d{2}-?\d{3}-?\d{4}-?\d{5}-?\d{2}\b")]
    private static partial Regex RadicadoJudicial();

    [GeneratedRegex(@"Juzgado\s+[^,.;\n]{3,90}", RegexOptions.IgnoreCase)]
    private static partial Regex Juzgado();
}

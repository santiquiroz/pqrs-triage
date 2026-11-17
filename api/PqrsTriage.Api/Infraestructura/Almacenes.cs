using System.Text.RegularExpressions;
using PqrsTriage.Api.Dominio;

namespace PqrsTriage.Api.Infraestructura;

public sealed class AlmacenCatalogo(Catalogo inicial)
{
    private readonly Lock candado = new();
    private Catalogo actual = inicial;

    public Catalogo Actual
    {
        get { lock (candado) return actual; }
    }

    public Catalogo Actualizar(Func<Catalogo, Catalogo> cambio)
    {
        lock (candado) return actual = cambio(actual);
    }
}

public sealed record CorreoEnviado(
    string Id,
    string Radicado,
    string Tipo,
    IReadOnlyList<string> Para,
    IReadOnlyList<string> Cc,
    string Asunto,
    string CuerpoHtml,
    string EnviadoEn);

public sealed class BandejaSalida
{
    private readonly Lock candado = new();
    private readonly List<CorreoEnviado> correos = [];

    public IReadOnlyList<CorreoEnviado> Guardar(string radicado, IReadOnlyList<CorreoPropuesto> propuestos, DateTimeOffset ahora)
    {
        var enviados = propuestos.Select((c, i) => Enviado(radicado, c, i, ahora)).ToList();
        lock (candado) correos.AddRange(enviados);
        return enviados;
    }

    public IReadOnlyList<CorreoEnviado> Todos()
    {
        lock (candado) return correos.AsEnumerable().Reverse().ToList();
    }

    private static CorreoEnviado Enviado(string radicado, CorreoPropuesto correo, int indice, DateTimeOffset ahora)
    {
        var final = GeneradorCorreos.ConRadicado(correo, radicado);
        return new CorreoEnviado($"{radicado}-{indice + 1}", radicado, final.Tipo, final.Para, final.Cc, final.Asunto, final.CuerpoHtml, ahora.ToString("O"));
    }
}

public sealed partial class ContadorRadicados
{
    private long ultimo;

    public ContadorRadicados(IEnumerable<string?> idsExistentes) =>
        ultimo = idsExistentes.Select(Numero).DefaultIfEmpty(0).Max();

    public string Siguiente() => $"PQRS-2026-{Interlocked.Increment(ref ultimo):D5}";

    private static long Numero(string? id)
    {
        var coincidencia = id is null ? Match.Empty : Formato().Match(id);
        return coincidencia.Success ? long.Parse(coincidencia.Groups[1].Value) : 0;
    }

    [GeneratedRegex(@"^PQRS-2026-(\d+)$")]
    private static partial Regex Formato();
}

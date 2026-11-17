using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace PqrsTriage.Api.Dominio;

public sealed record Plantilla(string Asunto, string CuerpoHtml);

public sealed partial class GeneradorCorreos(IReadOnlyDictionary<string, Plantilla> plantillas)
{
    public const string RadicadoProvisional = "PQRS-NUEVO";
    private const int LargoResumen = 400;
    private static readonly HashSet<string> ValoresYaEnHtml = ["razones", "extraidos", "notas"];

    public IReadOnlyList<CorreoPropuesto> Generar(IReadOnlyList<Envio> envios, Decision decision, CasoEntrada caso, Extraidos extraidos)
    {
        var valores = Valores(decision, caso, extraidos);
        return envios.Select(e => Renderizar(e, valores)).ToList();
    }

    public static CorreoPropuesto ConRadicado(CorreoPropuesto correo, string radicado) => correo with
    {
        Asunto = correo.Asunto.Replace(RadicadoProvisional, radicado),
        CuerpoHtml = correo.CuerpoHtml.Replace(RadicadoProvisional, radicado),
    };

    private CorreoPropuesto Renderizar(Envio envio, IReadOnlyDictionary<string, string> valores)
    {
        var plantilla = plantillas.GetValueOrDefault(envio.Tipo) ?? new Plantilla($"[{{{{radicado}}}}] {envio.Tipo}", "<p>{{resumen}}</p>");
        return new CorreoPropuesto(envio.Tipo, envio.Para, envio.Cc, Reemplazar(plantilla.Asunto, valores, false), Reemplazar(plantilla.CuerpoHtml, valores, true));
    }

    private static string Reemplazar(string plantilla, IReadOnlyDictionary<string, string> valores, bool esHtml) =>
        Marcador().Replace(plantilla, m => ValorDeMarcador(m.Groups[1].Value, valores, esHtml));

    private static string ValorDeMarcador(string clave, IReadOnlyDictionary<string, string> valores, bool esHtml)
    {
        var valor = valores.GetValueOrDefault(clave, "");
        if (ValoresYaEnHtml.Contains(clave)) return esHtml ? valor : "";
        return esHtml ? WebUtility.HtmlEncode(valor) : valor;
    }

    private static IReadOnlyDictionary<string, string> Valores(Decision d, CasoEntrada caso, Extraidos extraidos) => new Dictionary<string, string>
    {
        ["radicado"] = RadicadoProvisional,
        ["tipo"] = d.Tipo ?? "Tutela",
        ["motivo"] = d.Motivo ?? "Por clasificar",
        ["area"] = d.Area?.Nombre ?? "Mesa de PQRS",
        ["grupo"] = d.Grupo ?? "",
        ["responsable"] = d.Responsable?.Nombre ?? "Mesa de PQRS",
        ["prioridad"] = d.Prioridad,
        ["ans"] = $"{d.AnsDias} días hábiles",
        ["vence"] = FormatoEsCo.Fecha(DateTimeOffset.Parse(d.VenceEn)),
        ["destino"] = d.DestinoTexto,
        ["remitente"] = caso.Remitente.Nombre,
        ["empresa"] = caso.Remitente.Empresa ?? "",
        ["correoRemitente"] = caso.Remitente.Correo,
        ["activo"] = d.Activo ?? "No identificado",
        ["asuntoCaso"] = caso.Asunto,
        ["resumen"] = Texto.Recortar(caso.Descripcion, LargoResumen),
        ["etiquetas"] = string.Join(" · ", d.Etiquetas),
        ["razones"] = ListaHtml(d.Razones.Where(r => r.Disparada).Select(r => $"{r.Regla}: {r.Detalle}")),
        ["extraidos"] = ListaHtml(DescribirExtraidos(extraidos)),
        ["notas"] = BloqueNotas(d.Notas),
    };

    private static IEnumerable<string> DescribirExtraidos(Extraidos e)
    {
        var grupos = new (string Nombre, IReadOnlyList<string> Valores)[]
        {
            ("NIT", e.Nits), ("Facturas", e.Facturas), ("Locales", e.Locales), ("Radicados judiciales", e.RadicadosJudiciales),
            ("Juzgados", e.Juzgados), ("Teléfonos", e.Telefonos),
        };
        return grupos.Where(g => g.Valores.Count > 0).Select(g => $"{g.Nombre}: {string.Join(", ", g.Valores)}");
    }

    private static string BloqueNotas(IReadOnlyList<string> notas) =>
        notas.Count == 0
            ? ""
            : $"<div style=\"margin:14px 0 0;padding:10px 12px;border-left:3px solid #005787;background:#EEF4F8\"><strong>Nota de la IA</strong>{ListaHtml(notas)}</div>";

    private static string ListaHtml(IEnumerable<string> elementos)
    {
        var lista = elementos.ToList();
        if (lista.Count == 0) return "<p style=\"color:#4A5A70\">Sin datos.</p>";
        var html = new StringBuilder("<ul style=\"margin:0;padding-left:18px\">");
        foreach (var elemento in lista) html.Append("<li>").Append(WebUtility.HtmlEncode(elemento)).Append("</li>");
        return html.Append("</ul>").ToString();
    }

    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    private static partial Regex Marcador();
}

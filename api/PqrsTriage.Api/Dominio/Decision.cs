namespace PqrsTriage.Api.Dominio;

public static class Destinos
{
    public const string Tutela = "tutela";
    public const string Automatica = "automatica";
    public const string Gestor = "gestor";
}

public static class TiposCorreo
{
    public const string NotificacionArea = "notificacion-area";
    public const string AcuseCliente = "acuse-cliente";
    public const string AlertaJuridica = "alerta-juridica";
    public const string BandejaGestor = "bandeja-gestor";
}

public sealed record AreaRef(string Id, string Nombre, string Correo);

public sealed record ResponsableRef(string Id, string Nombre, string? Cargo);

public sealed record Razon(string Regla, string Detalle, bool Disparada);

public sealed record Envio(string Tipo, IReadOnlyList<string> Para, IReadOnlyList<string> Cc);

public sealed record CorreoPropuesto(string Tipo, IReadOnlyList<string> Para, IReadOnlyList<string> Cc, string Asunto, string CuerpoHtml);

public sealed record Extraidos(
    IReadOnlyList<string> Nits,
    IReadOnlyList<string> Facturas,
    IReadOnlyList<string> Locales,
    IReadOnlyList<string> Correos,
    IReadOnlyList<string> Telefonos,
    IReadOnlyList<string> RadicadosJudiciales,
    IReadOnlyList<string> Juzgados);

public sealed record Decision(
    string Destino,
    string DestinoTexto,
    string? Tipo,
    AreaRef? Area,
    string? Grupo,
    string? Motivo,
    ResponsableRef? Responsable,
    string Prioridad,
    int AnsDias,
    string VenceEn,
    string TipoDias,
    string? Activo,
    IReadOnlyList<string> Etiquetas,
    IReadOnlyList<Razon> Razones,
    bool RequiereRevisionHumana,
    IReadOnlyList<string> Notas,
    IReadOnlyList<CorreoPropuesto> Correos);

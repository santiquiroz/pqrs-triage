namespace PqrsTriage.Api.Dominio;

public sealed record TipoPqrs(string Id, string Desc);

public sealed record Motivo(string Nombre, string Prioridad, string NombreCliente, bool Publico);

public sealed record Grupo(string Nombre, string? NombreCliente, string? Responsable, string? Suplente, IReadOnlyList<Motivo> Motivos);

public sealed record Area(string Id, string Nombre, string Correo, bool Validado, IReadOnlyList<Grupo> Grupos);

public sealed record Activo(string Nombre, string Producto);

public sealed record Usuario(string Id, string Nombre, string Rol, string? Area, string? Cargo);

public sealed record Umbrales(
    double ConfianzaTipologia,
    double EncajaCatalogo,
    double Tutela,
    double DerechoPeticion,
    double RiesgoLegal,
    double UrgenciaCritica);

public sealed record Catalogo(
    int Version,
    string TipoDias,
    int AlertaPct,
    int CierreAutoDias,
    IReadOnlyDictionary<string, int> Prioridades,
    IReadOnlyList<TipoPqrs> Tipos,
    IReadOnlyList<Area> Areas,
    IReadOnlyList<Activo> Activos,
    IReadOnlyList<Usuario> Usuarios,
    IReadOnlyList<string> Festivos,
    string CorreoMesa,
    int AnsTutelaDias,
    int AnsDerechoPeticionDias,
    Umbrales Umbrales);

public sealed record MotivoUbicado(Area Area, Grupo Grupo, Motivo Motivo);

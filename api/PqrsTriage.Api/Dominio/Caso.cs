namespace PqrsTriage.Api.Dominio;

public sealed record Remitente(string Nombre, string? Empresa, string Correo, string? Tipo, string? Nit);

public sealed record CasoEntrada(
    string? Id,
    string Canal,
    string? Buzon,
    Remitente Remitente,
    string Asunto,
    string Descripcion,
    string? ActivoDeclarado = null,
    string? LocalDeclarado = null,
    string? TipoDeclarado = null,
    long? Creado = null);

public sealed record Esperado(
    string? Tipo,
    string? Area,
    string? Grupo,
    string? Motivo,
    IReadOnlyList<string>? MotivosAceptables,
    bool EsTutela,
    bool? DerechoPeticion,
    bool? RiesgoLegal,
    string? Destino,
    string? Activo,
    string? Prioridad);

public sealed record CasoEvaluacion(
    string Id,
    string Canal,
    string? Buzon,
    Remitente Remitente,
    string Asunto,
    string Descripcion,
    string? ActivoDeclarado,
    string? LocalDeclarado,
    long? Creado,
    Esperado? Esperado)
{
    public CasoEntrada AEntrada() =>
        new(Id, Canal, Buzon, Remitente, Asunto, Descripcion, ActivoDeclarado, LocalDeclarado, Creado: Creado);
}

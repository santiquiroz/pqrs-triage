namespace PqrsTriage.Api.Dominio;

public static class ConsultasCatalogo
{
    public const string AreaJuridica = "juridica";
    public const string MotivoTutela = "Acción de tutela";

    public static IEnumerable<MotivoUbicado> Motivos(this Catalogo catalogo) =>
        from area in catalogo.Areas
        from grupo in area.Grupos
        from motivo in grupo.Motivos
        select new MotivoUbicado(area, grupo, motivo);

    public static MotivoUbicado? BuscarMotivo(this Catalogo catalogo, string? nombre) =>
        nombre is null ? null : catalogo.Motivos().FirstOrDefault(m => m.Motivo.Nombre == nombre);

    public static Area? BuscarArea(this Catalogo catalogo, string id) =>
        catalogo.Areas.FirstOrDefault(a => a.Id == id);

    public static Usuario? BuscarUsuario(this Catalogo catalogo, string? id) =>
        id is null ? null : catalogo.Usuarios.FirstOrDefault(u => u.Id == id);

    public static Catalogo ConCorreoDeArea(this Catalogo catalogo, string id, string correo) =>
        catalogo with { Areas = catalogo.Areas.Select(a => a.Id == id ? a with { Correo = correo, Validado = true } : a).ToList() };
}

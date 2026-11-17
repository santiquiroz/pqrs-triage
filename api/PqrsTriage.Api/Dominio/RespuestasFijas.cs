namespace PqrsTriage.Api.Dominio;

public static class RespuestasFijas
{
    private const string Si = "si";
    private const string No = "no";

    public static Respuesta Eleccion(string id, string clave) =>
        new(id, TipoPregunta.Choice, clave, null, null, 1, new Dictionary<string, double> { [clave] = 1 }, true);

    public static Respuesta SiNo(string id, bool valor)
    {
        var p = valor ? 1 : 0;
        return new(id, TipoPregunta.Noul, valor ? Si : No, null, p, null, new Dictionary<string, double> { [Si] = p, [No] = 1 - p }, true);
    }
}

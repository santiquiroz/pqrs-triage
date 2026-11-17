namespace PqrsTriage.Api.Dominio;

public sealed record Modificadores(string Prioridad, int AnsDias, IReadOnlyList<string> Etiquetas, IReadOnlyList<Razon> Razones)
{
    public const string EtiquetaUrgenciaCritica = "Urgencia crítica";
    public const string EtiquetaDerechoPeticion = "Derecho de petición (Ley 1755 de 2015)";
    public const string EtiquetaRiesgoLegal = "Riesgo legal";

    public static Modificadores Calcular(LecturaRespuestas lectura, Catalogo catalogo, string prioridadCatalogo)
    {
        var umbrales = catalogo.Umbrales;
        var prioridadUrgencia = Prioridades.DesdeNivelUrgencia(lectura.Urgencia);
        var prioridad = Prioridades.Mayor(prioridadCatalogo, prioridadUrgencia);
        var esCritica = lectura.Urgencia >= umbrales.UrgenciaCritica;
        var esDerechoPeticion = lectura.DerechoPeticion >= umbrales.DerechoPeticion;
        var esRiesgoLegal = lectura.RiesgoLegal >= umbrales.RiesgoLegal;
        var ans = CalcularAnsDias(catalogo, prioridad, esDerechoPeticion);
        return new Modificadores(
            prioridad,
            ans,
            ListarEtiquetas(esCritica, esDerechoPeticion, esRiesgoLegal),
            [
                RazonUrgencia(lectura, prioridadCatalogo, prioridadUrgencia, prioridad),
                new(EtiquetaUrgenciaCritica, $"Puntaje de urgencia {FormatoEsCo.Numero(lectura.Urgencia)} de 3 (umbral {FormatoEsCo.Numero(umbrales.UrgenciaCritica)}): copia a la mesa", esCritica),
                new(EtiquetaDerechoPeticion, $"{FormatoEsCo.Probabilidad(lectura.DerechoPeticion)}: ANS máximo de {catalogo.AnsDerechoPeticionDias} días hábiles", esDerechoPeticion),
                new(EtiquetaRiesgoLegal, $"{FormatoEsCo.Probabilidad(lectura.RiesgoLegal)} (umbral {FormatoEsCo.Numero(umbrales.RiesgoLegal)}): copia a Jurídica", esRiesgoLegal),
            ]);
    }

    private static int CalcularAnsDias(Catalogo catalogo, string prioridad, bool esDerechoPeticion)
    {
        var ans = catalogo.Prioridades.GetValueOrDefault(prioridad, catalogo.AnsDerechoPeticionDias);
        return esDerechoPeticion ? Math.Min(ans, catalogo.AnsDerechoPeticionDias) : ans;
    }

    private static Razon RazonUrgencia(LecturaRespuestas lectura, string catalogo, string urgencia, string final)
    {
        var subio = final != catalogo;
        var detalle = $"Catálogo: {catalogo}. Urgencia juzgada: {urgencia} (puntaje {FormatoEsCo.Numero(lectura.Urgencia)}). Prioridad final: {final}. La IA puede subir la prioridad, nunca bajarla.";
        return new("La urgencia sube la prioridad", detalle, subio);
    }

    private static IReadOnlyList<string> ListarEtiquetas(bool esCritica, bool esDerechoPeticion, bool esRiesgoLegal)
    {
        var etiquetas = new List<string>();
        if (esCritica) etiquetas.Add(EtiquetaUrgenciaCritica);
        if (esDerechoPeticion) etiquetas.Add(EtiquetaDerechoPeticion);
        if (esRiesgoLegal) etiquetas.Add(EtiquetaRiesgoLegal);
        return etiquetas;
    }
}

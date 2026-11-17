namespace PqrsTriage.Api.Dominio;

public static class DeclaracionCliente
{
    public static void Validar(CasoEntrada caso, Catalogo catalogo)
    {
        if (EstaDeclarado(caso.TipoDeclarado) && catalogo.Tipos.All(t => t.Id != caso.TipoDeclarado))
            throw new ArgumentException($"No existe el tipo «{caso.TipoDeclarado}».");
    }

    public static IReadOnlyDictionary<string, Respuesta> Aplicar(IReadOnlyDictionary<string, Respuesta> modelo, CasoEntrada caso)
    {
        if (!EstaDeclarado(caso.TipoDeclarado)) return modelo;
        return new Dictionary<string, Respuesta>(modelo) { ["tipo"] = RespuestasFijas.Eleccion("tipo", caso.TipoDeclarado!) };
    }

    public static IReadOnlyList<string> Notas(IReadOnlyDictionary<string, Respuesta> modelo, CasoEntrada caso, double umbral)
    {
        var opinion = modelo.GetValueOrDefault("tipo");
        if (!EsDesacuerdo(opinion, caso.TipoDeclarado)) return [];
        var confianza = Confianza(opinion!);
        if (confianza < umbral) return [];
        return [$"Tipo: el cliente eligió «{caso.TipoDeclarado}», pero la IA cree que es «{opinion!.Eleccion}» ({FormatoEsCo.Porcentaje(confianza)})."];
    }

    private static bool EsDesacuerdo(Respuesta? opinion, string? declarado) =>
        EstaDeclarado(declarado) && opinion?.Eleccion is not null && opinion.Eleccion != declarado;

    private static double Confianza(Respuesta opinion) =>
        opinion.Confianza ?? opinion.Probabilidades.GetValueOrDefault(opinion.Eleccion!, 0);

    private static bool EstaDeclarado(string? valor) => !string.IsNullOrWhiteSpace(valor);
}

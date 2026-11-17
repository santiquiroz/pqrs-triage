using PqrsTriage.Api.Dominio;

namespace PqrsTriage.Api.Proveedores;

public static class FabricaRespuestas
{
    public const string Si = "si";
    public const string No = "no";

    public static Respuesta Eleccion(Pregunta pregunta, IReadOnlyDictionary<string, double> probabilidades, double? confianza, bool calibrada) =>
        new(pregunta.Id, TipoPregunta.Choice, ClaveMasProbable(probabilidades), null, null, confianza, probabilidades, calibrada);

    public static Respuesta Puntaje(Pregunta pregunta, IReadOnlyList<double> porNivel, double? puntaje, double? confianza, bool calibrada)
    {
        var niveles = pregunta.Niveles ?? [];
        var indice = IndiceMaximo(porNivel);
        var probabilidades = porNivel.Select((p, i) => (i, p)).ToDictionary(x => x.i.ToString(), x => x.p);
        var esperado = puntaje ?? porNivel.Select((p, i) => p * i).Sum();
        return new(pregunta.Id, TipoPregunta.Score, niveles.ElementAtOrDefault(indice), esperado, null, confianza, probabilidades, calibrada);
    }

    public static Respuesta Noul(Pregunta pregunta, double probabilidad, bool calibrada)
    {
        var p = Math.Clamp(probabilidad, 0, 1);
        var probabilidades = new Dictionary<string, double> { [Si] = p, [No] = 1 - p };
        return new(pregunta.Id, TipoPregunta.Noul, p >= 0.5 ? Si : No, null, p, null, probabilidades, calibrada);
    }

    public static IReadOnlyDictionary<string, double> Repartir(IReadOnlyList<string> claves, string elegida, double probabilidadElegida)
    {
        var resto = claves.Count > 1 ? (1 - probabilidadElegida) / (claves.Count - 1) : 0;
        return claves.ToDictionary(c => c, c => c == elegida ? probabilidadElegida : resto);
    }

    public static IReadOnlyList<double> RepartirNiveles(int cantidad, int elegido, double probabilidadElegida)
    {
        var resto = cantidad > 1 ? (1 - probabilidadElegida) / (cantidad - 1) : 0;
        return Enumerable.Range(0, cantidad).Select(i => i == elegido ? probabilidadElegida : resto).ToList();
    }

    public static IReadOnlyList<double> Normalizar(IReadOnlyList<double> valores)
    {
        var suma = valores.Sum();
        return suma <= 0 ? valores.Select(_ => 1.0 / valores.Count).ToList() : valores.Select(v => v / suma).ToList();
    }

    public static IReadOnlyList<double> Softmax(IReadOnlyList<double> puntajes, double factor)
    {
        var maximo = puntajes.Max();
        return Normalizar(puntajes.Select(p => Math.Exp((p - maximo) * factor)).ToList());
    }

    public static int IndiceMaximo(IReadOnlyList<double> valores)
    {
        var indice = 0;
        for (var i = 1; i < valores.Count; i++)
            if (valores[i] > valores[indice]) indice = i;
        return indice;
    }

    private static string? ClaveMasProbable(IReadOnlyDictionary<string, double> probabilidades) =>
        probabilidades.Count == 0 ? null : probabilidades.MaxBy(kv => kv.Value).Key;
}

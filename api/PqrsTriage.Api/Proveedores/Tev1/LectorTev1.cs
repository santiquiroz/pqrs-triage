using System.Text.Json.Nodes;

namespace PqrsTriage.Api.Proveedores.Tev1;

public sealed record ProbabilidadesTev1(IReadOnlyList<double> Valores, bool DesdeLogprobs)
{
    public int Ganadora => FabricaRespuestas.IndiceMaximo(Valores);
}

public static class LectorTev1
{
    public static string Letra(int indice) => ((char)('A' + indice)).ToString();

    public static ProbabilidadesTev1 Leer(JsonNode respuesta, int cantidadOpciones)
    {
        var eleccion = respuesta["choices"]?[0];
        var contenido = eleccion?["message"]?["content"]?.GetValue<string>() ?? "";
        var indice = IndiceDeLetra(contenido, cantidadOpciones);
        var desdeLogprobs = DesdeLogprobs(eleccion?["logprobs"]?["content"]?[0]?["top_logprobs"]?.AsArray(), cantidadOpciones);
        return desdeLogprobs ?? new ProbabilidadesTev1(UnoEn(indice, cantidadOpciones), false);
    }

    public static int TokensEntrada(JsonNode respuesta) => respuesta["usage"]?["prompt_tokens"]?.GetValue<int>() ?? 0;

    private static int IndiceDeLetra(string contenido, int cantidad)
    {
        var limpio = contenido.Trim().TrimStart('"', '(', '[', '*');
        var indice = limpio.Length == 0 ? -1 : char.ToUpperInvariant(limpio[0]) - 'A';
        if (indice < 0 || indice >= cantidad) throw new ErrorProveedor($"Tev1 devolvió una opción inválida: «{Recortar(contenido)}»");
        return indice;
    }

    private static ProbabilidadesTev1? DesdeLogprobs(JsonArray? top, int cantidad)
    {
        if (top is null || top.Count == 0) return null;
        var valores = new double[cantidad];
        foreach (var candidato in top) SumarCandidato(valores, candidato);
        return valores.Sum() <= 0 ? null : new ProbabilidadesTev1(FabricaRespuestas.Normalizar(valores), true);
    }

    private static void SumarCandidato(double[] valores, JsonNode? candidato)
    {
        var token = candidato?["token"]?.GetValue<string>()?.Trim() ?? "";
        if (token.Length != 1) return;
        var indice = char.ToUpperInvariant(token[0]) - 'A';
        if (indice < 0 || indice >= valores.Length) return;
        valores[indice] += Math.Exp(candidato!["logprob"]!.GetValue<double>());
    }

    private static IReadOnlyList<double> UnoEn(int indice, int cantidad) =>
        Enumerable.Range(0, cantidad).Select(i => i == indice ? 1.0 : 0.0).ToList();

    private static string Recortar(string texto) => texto.Length <= 40 ? texto : texto[..40];
}

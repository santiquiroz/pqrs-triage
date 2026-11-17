using PqrsTriage.Api.Dominio;

namespace PqrsTriage.Api.Proveedores.Reglas;

public static class ReglasTipologia
{
    private const int LargoMinimo = 4;
    private const int LargoPrefijo = 5;
    private const double FactorSoftmax = 1.5;
    private const string SeparadorCliente = "Para el cliente:";

    private static readonly HashSet<string> PalabrasVacias =
    [
        "para", "cliente", "suruworks", "como", "sobre", "desde", "entre", "esta", "este", "estos", "estas", "tiene", "porque",
        "cuando", "donde", "todo", "todos", "nuestro", "nuestra", "favor", "gracias", "saludo", "cordial", "buenos", "buenas",
        "dias", "tardes", "solicito", "solicitamos", "quiero", "queremos", "necesito", "necesitamos", "otro", "otros",
    ];

    public static Respuesta Responder(Pregunta pregunta, string texto)
    {
        var opciones = pregunta.Opciones ?? [];
        var probabilidades = FabricaRespuestas.Softmax(Puntajes(pregunta, texto), FactorSoftmax);
        var porClave = opciones.Select((o, i) => (o.Clave, P: probabilidades[i])).ToDictionary(x => x.Clave, x => x.P);
        return FabricaRespuestas.Eleccion(pregunta, porClave, probabilidades.Max(), false);
    }

    public static IReadOnlyList<double> Puntajes(Pregunta pregunta, string texto)
    {
        var prefijosTexto = Prefijos(texto);
        return (pregunta.Opciones ?? []).Select(o => Puntaje(o, prefijosTexto)).ToList();
    }

    private static double Puntaje(Opcion opcion, HashSet<string> prefijosTexto)
    {
        var partes = opcion.Descripcion.Split(SeparadorCliente);
        var contexto = partes[0];
        var paraCliente = partes.Length > 1 ? partes[1] : "";
        return 2 * Coincidencias(opcion.Clave, prefijosTexto)
            + Coincidencias(paraCliente, prefijosTexto)
            + 0.5 * Coincidencias(contexto, prefijosTexto);
    }

    private static int Coincidencias(string frase, HashSet<string> prefijosTexto) =>
        Prefijos(Texto.Normalizar(frase)).Count(prefijosTexto.Contains);

    private static HashSet<string> Prefijos(string normalizado) =>
        Texto.Palabras(normalizado)
            .Where(p => p.Length >= LargoMinimo && !PalabrasVacias.Contains(p))
            .Select(p => p[..Math.Min(LargoPrefijo, p.Length)])
            .ToHashSet();
}

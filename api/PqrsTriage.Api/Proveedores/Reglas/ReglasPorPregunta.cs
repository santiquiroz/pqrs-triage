using System.Text.RegularExpressions;
using PqrsTriage.Api.Dominio;

namespace PqrsTriage.Api.Proveedores.Reglas;

public static partial class ReglasPorPregunta
{
    private const double ProbabilidadElegida = 0.7;
    private const double ProbabilidadActivo = 0.9;
    private const double ProbabilidadNivel = 0.8;

    public static Respuesta Tipo(Pregunta pregunta, string texto)
    {
        var claves = Claves(pregunta);
        var buscado = TipoPorPalabras(texto);
        var elegida = claves.FirstOrDefault(c => Texto.Normalizar(c) == buscado) ?? claves.FirstOrDefault() ?? buscado;
        return FabricaRespuestas.Eleccion(pregunta, FabricaRespuestas.Repartir(claves, elegida, ProbabilidadElegida), ProbabilidadElegida, false);
    }

    public static Respuesta Encaja(Pregunta pregunta, string texto, Pregunta? tipologia)
    {
        if (Spam().IsMatch(texto)) return FabricaRespuestas.Noul(pregunta, 0.05, false);
        var hayCoincidencia = tipologia is not null && ReglasTipologia.Puntajes(tipologia, texto).Max() > 0;
        return FabricaRespuestas.Noul(pregunta, hayCoincidencia ? 0.8 : 0.4, false);
    }

    public static Respuesta Activo(Pregunta pregunta, string texto)
    {
        var claves = Claves(pregunta);
        var mencionado = claves.FirstOrDefault(c => c != ConstructorPreguntas.NoIdentificado && Texto.MencionaActivo(texto, c));
        var elegida = mencionado ?? ConstructorPreguntas.NoIdentificado;
        return FabricaRespuestas.Eleccion(pregunta, FabricaRespuestas.Repartir(claves, elegida, ProbabilidadActivo), ProbabilidadActivo, false);
    }

    public static Respuesta Urgencia(Pregunta pregunta, string texto)
    {
        var cantidad = pregunta.Niveles?.Count ?? 4;
        var nivel = Math.Min(NivelUrgencia(texto), cantidad - 1);
        var porNivel = FabricaRespuestas.RepartirNiveles(cantidad, nivel, ProbabilidadNivel);
        return FabricaRespuestas.Puntaje(pregunta, porNivel, null, ProbabilidadNivel, false);
    }

    public static double ProbabilidadTutela(string texto)
    {
        if (!Tutela().IsMatch(texto)) return 0.02;
        return ContextoJudicial().IsMatch(texto) ? 0.95 : 0.4;
    }

    public static double ProbabilidadDerechoPeticion(string texto) => DerechoPeticion().IsMatch(texto) ? 0.95 : 0.02;

    public static double ProbabilidadRiesgoLegal(string texto) => RiesgoLegal().IsMatch(texto) ? 0.9 : 0.05;

    public static Respuesta Uniforme(Pregunta pregunta) => pregunta.Tipo switch
    {
        TipoPregunta.Noul => FabricaRespuestas.Noul(pregunta, 0.5, false),
        TipoPregunta.Score => FabricaRespuestas.Puntaje(pregunta, FabricaRespuestas.Normalizar(pregunta.Niveles!.Select(_ => 1.0).ToList()), null, null, false),
        _ => FabricaRespuestas.Eleccion(pregunta, Claves(pregunta).ToDictionary(c => c, _ => 1.0 / Math.Max(1, Claves(pregunta).Count)), null, false),
    };

    private static string TipoPorPalabras(string texto)
    {
        if (Felicitacion().IsMatch(texto)) return "felicitacion";
        if (Sugerencia().IsMatch(texto)) return "sugerencia";
        if (Reclamo().IsMatch(texto)) return "reclamo";
        if (Queja().IsMatch(texto)) return "queja";
        return "peticion";
    }

    private static int NivelUrgencia(string texto)
    {
        if (Critica().IsMatch(texto)) return 3;
        if (Hoy().IsMatch(texto)) return 2;
        if (EstaSemana().IsMatch(texto)) return 1;
        return 0;
    }

    private static IReadOnlyList<string> Claves(Pregunta pregunta) => pregunta.Opciones?.Select(o => o.Clave).ToList() ?? [];

    [GeneratedRegex(@"felicit|agradec|excelente")]
    private static partial Regex Felicitacion();

    [GeneratedRegex(@"suger|propon|seria bueno|seria ideal")]
    private static partial Regex Sugerencia();

    [GeneratedRegex(@"cobr|factura.*(mal|error|incorrect|doble)|reclam|no corresponde|nota credito")]
    private static partial Regex Reclamo();

    [GeneratedRegex(@"queja|pesim|molest|inconform|mala atencion|no funciona")]
    private static partial Regex Queja();

    [GeneratedRegex(@"descuento|suscrib|fuera de la oficina|respuesta automatica|unsubscribe|out of office")]
    private static partial Regex Spam();

    [GeneratedRegex(@"\btutela")]
    private static partial Regex Tutela();

    [GeneratedRegex(@"juzgado|despacho|accionante|accionad|auto admisorio|desacato|fallo|decreto 2591")]
    private static partial Regex ContextoJudicial();

    [GeneratedRegex(@"derecho de peticion|ley 1755|articulo 23")]
    private static partial Regex DerechoPeticion();

    [GeneratedRegex(@"demand|abogad|superintendencia|denuncia|accion legal|procuradur|tutela|requerimiento")]
    private static partial Regex RiesgoLegal();

    [GeneratedRegex(@"\b(inundac|incendi|robo|hurt|emergenc)|riesgo electrico")]
    private static partial Regex Critica();

    [GeneratedRegex(@"urgente|\bhoy\b|inmediat|ya mismo")]
    private static partial Regex Hoy();

    [GeneratedRegex(@"esta semana|pronto")]
    private static partial Regex EstaSemana();
}

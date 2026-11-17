using System.Text.Json.Nodes;
using PqrsTriage.Api.Dominio;

namespace PqrsTriage.Api.Proveedores.Tev1;

public sealed record LlamadaTev1(JsonNode Solicitud, JsonNode Respuesta, int Tokens);

public sealed record SalidaPregunta(Respuesta Respuesta, IReadOnlyList<LlamadaTev1> Llamadas);

public sealed record EleccionTev1(IReadOnlyList<Opcion> Opciones, IReadOnlyList<double> Probabilidades, double? Confianza, IReadOnlyList<LlamadaTev1> Llamadas)
{
    public int IndiceGanador => FabricaRespuestas.IndiceMaximo(Probabilidades);

    public Opcion OpcionGanadora => Opciones[IndiceGanador];

    public double ProbabilidadDe(string clave) =>
        Opciones.Select((o, i) => (o.Clave, i)).Where(x => x.Clave == clave).Select(x => Probabilidades[x.i]).FirstOrDefault();

    public IReadOnlyDictionary<string, double> PorClave() =>
        Opciones.Select((o, i) => (o.Clave, P: Probabilidades[i])).ToDictionary(x => x.Clave, x => x.P);
}

public static class OpcionesTev1
{
    public const string ClaveSi = "si";
    public const string ClaveNo = "no";

    public static IReadOnlyList<Opcion> Desde(Pregunta pregunta) => pregunta.Tipo switch
    {
        TipoPregunta.Noul =>
        [
            new Opcion(ClaveSi, $"Sí. {pregunta.Criterios?.Verdadero ?? pregunta.Instrucciones}"),
            new Opcion(ClaveNo, $"No. {pregunta.Criterios?.Falso ?? "No aplica."}"),
        ],
        TipoPregunta.Score => (pregunta.Niveles ?? []).Select((n, i) => new Opcion(i.ToString(), n)).ToList(),
        _ => pregunta.Opciones ?? [],
    };
}

public static class Torneo
{
    public static IReadOnlyList<IReadOnlyList<Opcion>> Partir(IReadOnlyList<Opcion> opciones, int maximoPorGrupo)
    {
        var cantidadGrupos = (int)Math.Ceiling(opciones.Count / (double)maximoPorGrupo);
        var tamano = (int)Math.Ceiling(opciones.Count / (double)cantidadGrupos);
        return opciones.Chunk(tamano).Select(g => (IReadOnlyList<Opcion>)g).ToList();
    }

    public static EleccionTev1 Combinar(IReadOnlyList<Opcion> todas, IReadOnlyList<EleccionTev1> rondas, EleccionTev1 final)
    {
        var ganadora = final.OpcionGanadora.Clave;
        var probabilidades = todas.Select(o => final.ProbabilidadDe(o.Clave)).ToList();
        var rondaGanadora = rondas.First(r => r.OpcionGanadora.Clave == ganadora);
        var confianza = final.Confianza is null || rondaGanadora.Confianza is null
            ? (double?)null
            : final.Confianza.Value * rondaGanadora.ProbabilidadDe(ganadora);
        return new EleccionTev1(todas, probabilidades, confianza, [.. rondas.SelectMany(r => r.Llamadas), .. final.Llamadas]);
    }
}

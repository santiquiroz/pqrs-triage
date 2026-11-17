using PqrsTriage.Api.Dominio;
using PqrsTriage.Api.Infraestructura;

namespace PqrsTriage.Api.Evaluacion;

public static class Calificador
{
    public static Obtenido Obtener(ResultadoTriage resultado)
    {
        var d = resultado.Decision;
        var tipologia = resultado.Respuestas.GetValueOrDefault("tipologia");
        return new Obtenido(d.Tipo, d.Area?.Id, d.Motivo, d.Prioridad, d.Destino == Destinos.Tutela, d.Activo, d.Destino, tipologia?.Confianza ?? tipologia?.Probabilidades.Values.DefaultIfEmpty(0).Max());
    }

    public static Aciertos? Calificar(CasoEvaluacion caso, Obtenido obtenido)
    {
        var e = caso.Esperado;
        if (e is null) return null;
        return new Aciertos(
            Comparar(e.Tipo, obtenido.Tipo),
            Comparar(e.Area, obtenido.Area),
            AciertoMotivo(e, obtenido),
            obtenido.EsTutela == e.EsTutela,
            AciertoActivo(caso, e, obtenido),
            Comparar(e.Destino, obtenido.Destino));
    }

    private static bool? Comparar(string? esperado, string? obtenido) => esperado is null ? null : esperado == obtenido;

    private static bool? AciertoMotivo(Esperado e, Obtenido o)
    {
        if (e.Motivo is null) return null;
        return o.Motivo == e.Motivo || (e.MotivosAceptables?.Contains(o.Motivo ?? "") ?? false);
    }

    private static bool? AciertoActivo(CasoEvaluacion caso, Esperado e, Obtenido o)
    {
        if (e.Activo is null || !ActivoEsDeducible(caso, e.Activo)) return null;
        return e.Activo == o.Activo;
    }

    private static bool ActivoEsDeducible(CasoEvaluacion caso, string activo) =>
        !string.IsNullOrWhiteSpace(caso.ActivoDeclarado) || Texto.MencionaActivo(Texto.Normalizar($"{caso.Asunto} {caso.Descripcion}"), activo);
}

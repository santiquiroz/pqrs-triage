using PqrsTriage.Api.Dominio;

namespace PqrsTriage.Api.Evaluacion;

public static class CalculadoraResumen
{
    public static ResumenEvaluacion Resumir(string proveedor, IReadOnlyList<FilaEvaluacion> filas)
    {
        var validas = filas.Where(f => f.Error is null && f.Obtenido is not null).ToList();
        var calificadas = validas.Where(f => f.Aciertos is not null).ToList();
        var latencias = validas.Select(f => f.LatenciaMs).Order().ToList();
        return new ResumenEvaluacion(
            proveedor,
            filas.Count,
            filas.Count(f => f.Error is not null),
            Fraccion(calificadas.Select(f => f.Aciertos!.Tipo)),
            Fraccion(calificadas.Select(f => f.Aciertos!.Area)),
            Fraccion(calificadas.Select(f => f.Aciertos!.Motivo)),
            Fraccion(calificadas.Select(f => f.Aciertos!.Activo)),
            Fraccion(calificadas.Where(f => f.Obtenido!.Destino == Destinos.Automatica).Select(f => f.Aciertos!.Motivo)),
            Tutela(calificadas),
            Porcentaje(validas, Destinos.Automatica),
            Porcentaje(validas, Destinos.Gestor),
            Porcentaje(validas, Destinos.Tutela),
            Percentil(latencias, 0.5),
            Percentil(latencias, 0.95),
            validas.Count == 0 ? 0 : validas.Average(f => f.CostoUsd) * 1000);
    }

    public static double? Fraccion(IEnumerable<bool?> aciertos)
    {
        var contados = aciertos.Where(a => a.HasValue).Select(a => a!.Value).ToList();
        return contados.Count == 0 ? null : contados.Count(a => a) / (double)contados.Count;
    }

    public static long Percentil(IReadOnlyList<long> ordenados, double percentil)
    {
        if (ordenados.Count == 0) return 0;
        var indice = (int)Math.Ceiling(percentil * ordenados.Count) - 1;
        return ordenados[Math.Clamp(indice, 0, ordenados.Count - 1)];
    }

    private static ResumenTutela Tutela(IReadOnlyList<FilaEvaluacion> filas)
    {
        var vp = filas.Count(f => f.Esperado!.EsTutela && f.Obtenido!.EsTutela);
        var fn = filas.Count(f => f.Esperado!.EsTutela && !f.Obtenido!.EsTutela);
        var fp = filas.Count(f => !f.Esperado!.EsTutela && f.Obtenido!.EsTutela);
        return new ResumenTutela(vp, fn, fp, Division(vp, vp + fn), Division(vp, vp + fp));
    }

    private static double Porcentaje(IReadOnlyList<FilaEvaluacion> filas, string destino) =>
        filas.Count == 0 ? 0 : filas.Count(f => f.Obtenido!.Destino == destino) / (double)filas.Count;

    private static double? Division(int numerador, int denominador) => denominador == 0 ? null : numerador / (double)denominador;
}

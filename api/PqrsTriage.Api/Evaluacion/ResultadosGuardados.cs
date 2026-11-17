using System.Text.Json;
using PqrsTriage.Api.Infraestructura;

namespace PqrsTriage.Api.Evaluacion;

public sealed record ResultadoGuardado(string Archivo, DateTimeOffset GeneradoEn, IReadOnlyList<ResumenEvaluacion> Resumenes, IReadOnlyList<FilaPorProveedor> Filas);

public static class ResultadosGuardados
{
    private const string Carpeta = "resultados";

    public static IReadOnlyList<ResultadoGuardado> Listar(RepositorioDatos datos)
    {
        var carpeta = Path.Combine(datos.Ruta, Carpeta);
        if (!Directory.Exists(carpeta)) return [];
        return Directory.EnumerateFiles(carpeta, "*.json")
            .Select(Leer)
            .OfType<ResultadoGuardado>()
            .OrderByDescending(r => r.GeneradoEn)
            .ToList();
    }

    private static ResultadoGuardado? Leer(string ruta)
    {
        try
        {
            var reporte = JsonSerializer.Deserialize<ReporteEvaluacion>(File.ReadAllText(ruta), Json.Relajado);
            if (reporte is null) return null;
            return new ResultadoGuardado(Path.GetFileName(ruta), File.GetLastWriteTimeUtc(ruta), reporte.Resumenes, reporte.Filas);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

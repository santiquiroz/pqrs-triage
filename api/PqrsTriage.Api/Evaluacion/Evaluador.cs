using PqrsTriage.Api.Infraestructura;
using PqrsTriage.Api.Proveedores;

namespace PqrsTriage.Api.Evaluacion;

public sealed class Evaluador(RepositorioDatos datos, ServicioTriage triage)
{
    public async Task<FilaEvaluacion> EvaluarAsync(IProveedorDecisiones proveedor, string conjunto, string casoId, CancellationToken ct)
    {
        var encontrado = datos.Buscar(conjunto, casoId) ?? throw new ArgumentException($"No existe el caso {casoId} en el conjunto {conjunto}");
        try
        {
            var resultado = await triage.ClasificarConAsync(proveedor, encontrado.Caso.AEntrada(), ct);
            var obtenido = Calificador.Obtener(resultado);
            return new FilaEvaluacion(casoId, encontrado.Conjunto, encontrado.Caso.Esperado, obtenido, Calificador.Calificar(encontrado.Caso, obtenido),
                resultado.Metricas.LatenciaMs, resultado.Metricas.CostoUsd, null);
        }
        catch (Exception ex) when (ex is ErrorProveedor or HttpRequestException or TaskCanceledException)
        {
            return new FilaEvaluacion(casoId, encontrado.Conjunto, encontrado.Caso.Esperado, null, null, 0, 0, ex.Message);
        }
    }
}

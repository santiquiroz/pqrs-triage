using PqrsTriage.Api.Dominio;
using PqrsTriage.Api.Evaluacion;

namespace PqrsTriage.Tests;

public class CalculadoraResumenTests
{
    [Fact]
    public void Calcula_exactitudes_tutela_y_latencias_ignorando_nulos_y_errores()
    {
        FilaEvaluacion[] filas =
        [
            Fila("1", esTutela: false, destino: Destinos.Automatica, motivo: true, tipo: true, latencia: 100, costo: 0.001),
            Fila("2", esTutela: true, destino: Destinos.Tutela, motivo: true, tipo: null, latencia: 200, costo: 0.001),
            Fila("3", esTutela: false, destino: Destinos.Gestor, motivo: false, tipo: false, latencia: 300, costo: 0.001),
            new("4", "dificiles", Esperado(true), null, null, 0, 0, "Tev1 no responde"),
        ];

        var resumen = CalculadoraResumen.Resumir("prueba", filas);

        Assert.Equal(4, resumen.Total);
        Assert.Equal(1, resumen.Errores);
        Assert.Equal(0.5, resumen.ExactitudTipo);
        Assert.Equal(2 / 3.0, resumen.ExactitudTipologia!.Value, 6);
        Assert.Equal(1, resumen.ExactitudEnAutomaticos);
        Assert.Equal(new ResumenTutela(1, 0, 0, 1, 1), resumen.Tutela);
        Assert.Equal(1 / 3.0, resumen.PorcentajeAutomatico, 6);
        Assert.Equal(200, resumen.LatenciaP50Ms);
        Assert.Equal(300, resumen.LatenciaP95Ms);
        Assert.Equal(1, resumen.CostoUsdPorMil, 6);
        Assert.Null(resumen.ExactitudActivo);
    }

    private static Esperado Esperado(bool esTutela) => new("Petición", "rentas", null, "X", null, esTutela, null, null, null, null, null);

    private static FilaEvaluacion Fila(string id, bool esTutela, string destino, bool motivo, bool? tipo, long latencia, double costo) => new(
        id,
        "mockup",
        Esperado(esTutela),
        new Obtenido("Petición", "rentas", "X", "Media", destino == Destinos.Tutela, null, destino, 0.9),
        new Aciertos(tipo, true, motivo, true, null, null),
        latencia,
        costo,
        null);
}

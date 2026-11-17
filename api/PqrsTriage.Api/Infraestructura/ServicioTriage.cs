using System.Diagnostics;
using System.Text.Json.Nodes;
using PqrsTriage.Api.Dominio;
using PqrsTriage.Api.Proveedores;

namespace PqrsTriage.Api.Infraestructura;

public sealed record Metricas(long LatenciaMs, int Llamadas, int TokensEntrada, double CostoUsd);

public sealed record Crudo(IReadOnlyList<JsonNode> Solicitudes, IReadOnlyList<JsonNode> Respuestas);

public sealed record ResultadoTriage(
    ProveedorInfo Proveedor,
    IReadOnlyDictionary<string, string> Estado,
    IReadOnlyList<Pregunta> Preguntas,
    IReadOnlyDictionary<string, Respuesta> Respuestas,
    Extraidos Extraidos,
    Decision Decision,
    Metricas Metricas,
    Crudo Crudo);

public sealed class ServicioTriage(AlmacenCatalogo almacen, RegistroProveedores registro, MotorEnrutamiento motor, TimeProvider reloj)
{
    public async Task<ResultadoTriage> ClasificarAsync(string proveedorId, CasoEntrada caso, CancellationToken ct)
    {
        var proveedor = await registro.ObtenerDisponibleAsync(proveedorId, ct);
        return await ClasificarConAsync(proveedor, caso, ct);
    }

    public async Task<ResultadoTriage> ClasificarConAsync(IProveedorDecisiones proveedor, CasoEntrada caso, CancellationToken ct)
    {
        var catalogo = almacen.Actual;
        DeclaracionCliente.Validar(caso, catalogo);
        var preguntas = ConstructorPreguntas.Construir(catalogo);
        var estado = ConstructorPreguntas.ConstruirEstado(caso);
        var cronometro = Stopwatch.StartNew();
        var resultado = await proveedor.DecidirAsync(estado, preguntas, ct);
        cronometro.Stop();
        var extraidos = ExtractorDatos.Extraer(caso);
        var creado = caso.Creado ?? reloj.GetUtcNow().ToUnixTimeMilliseconds();
        var decision = motor.Decidir(new EntradaMotor(resultado.Respuestas, caso, catalogo, extraidos, creado));
        return new ResultadoTriage(
            proveedor.Info() with { Modelo = resultado.Modelo },
            estado,
            preguntas,
            resultado.Respuestas,
            extraidos,
            decision,
            new Metricas(cronometro.ElapsedMilliseconds, resultado.Llamadas, resultado.TokensEntrada, resultado.CostoUsd),
            new Crudo(resultado.Solicitudes, resultado.RespuestasCrudas));
    }

    public Decision Decidir(CasoEntrada caso, IReadOnlyDictionary<string, Respuesta> respuestas)
    {
        var catalogo = almacen.Actual;
        DeclaracionCliente.Validar(caso, catalogo);
        var creado = caso.Creado ?? reloj.GetUtcNow().ToUnixTimeMilliseconds();
        return motor.Decidir(new EntradaMotor(respuestas, caso, catalogo, ExtractorDatos.Extraer(caso), creado));
    }

    public Decision Corregir(CasoEntrada caso, IReadOnlyDictionary<string, Respuesta> respuestas, string motivo)
    {
        if (almacen.Actual.BuscarMotivo(motivo) is null) throw new KeyNotFoundException($"No existe la tipología «{motivo}».");
        return Decidir(caso, CorreccionHumana.ConMotivo(respuestas, motivo));
    }
}

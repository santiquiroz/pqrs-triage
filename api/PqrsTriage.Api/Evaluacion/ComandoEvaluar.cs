using System.Globalization;
using System.Text.Json;
using PqrsTriage.Api.Infraestructura;
using PqrsTriage.Api.Proveedores;

namespace PqrsTriage.Api.Evaluacion;

public sealed record ArgumentosEvaluar(string Proveedor, string Conjunto, string? Salida, int? Limite, int Paralelo)
{
    public static ArgumentosEvaluar Leer(IReadOnlyList<string> args)
    {
        string? Valor(string nombre) => args.SkipWhile(a => a != nombre).Skip(1).FirstOrDefault();
        return new ArgumentosEvaluar(
            Valor("--proveedor") ?? "reglas",
            Valor("--conjunto") ?? RepositorioDatos.ConjuntoTodos,
            Valor("--salida"),
            int.TryParse(Valor("--limite"), out var limite) ? limite : null,
            int.TryParse(Valor("--paralelo"), out var paralelo) ? paralelo : 2);
    }
}

public sealed record ReporteEvaluacion(IReadOnlyList<ResumenEvaluacion> Resumenes, IReadOnlyList<FilaPorProveedor> Filas);

public sealed record FilaPorProveedor(string Proveedor, FilaEvaluacion Fila);

public static class ComandoEvaluar
{
    public static async Task<int> EjecutarAsync(IServiceProvider servicios, IReadOnlyList<string> args)
    {
        var argumentos = ArgumentosEvaluar.Leer(args);
        var datos = servicios.GetRequiredService<RepositorioDatos>();
        var registro = servicios.GetRequiredService<RegistroProveedores>();
        var evaluador = servicios.GetRequiredService<Evaluador>();
        var proveedores = await ProveedoresAsync(registro, argumentos.Proveedor);
        var casos = datos.Conjunto(argumentos.Conjunto).Where(c => c.Esperado is not null).Take(argumentos.Limite ?? int.MaxValue).ToList();
        var reporte = await EvaluarTodosAsync(proveedores, casos.Select(c => c.Id).ToList(), argumentos, evaluador);
        Imprimir(reporte.Resumenes);
        await GuardarAsync(reporte, argumentos.Salida, Directory.GetParent(datos.Ruta)!.FullName);
        return 0;
    }

    private static async Task<IReadOnlyList<IProveedorDecisiones>> ProveedoresAsync(RegistroProveedores registro, string id) =>
        id == "todos" ? await registro.DisponiblesAsync(CancellationToken.None) : [await registro.ObtenerDisponibleAsync(id, CancellationToken.None)];

    private static async Task<ReporteEvaluacion> EvaluarTodosAsync(
        IReadOnlyList<IProveedorDecisiones> proveedores, IReadOnlyList<string> casos, ArgumentosEvaluar argumentos, Evaluador evaluador)
    {
        var resumenes = new List<ResumenEvaluacion>();
        var filas = new List<FilaPorProveedor>();
        foreach (var proveedor in proveedores)
        {
            var delProveedor = await EvaluarProveedorAsync(proveedor, casos, argumentos, evaluador);
            resumenes.Add(CalculadoraResumen.Resumir(proveedor.Id, delProveedor));
            filas.AddRange(delProveedor.Select(f => new FilaPorProveedor(proveedor.Id, f)));
        }
        return new ReporteEvaluacion(resumenes, filas);
    }

    private static async Task<IReadOnlyList<FilaEvaluacion>> EvaluarProveedorAsync(
        IProveedorDecisiones proveedor, IReadOnlyList<string> casos, ArgumentosEvaluar argumentos, Evaluador evaluador)
    {
        using var limite = new SemaphoreSlim(argumentos.Paralelo);
        var hechos = 0;
        var tareas = casos.Select(async id =>
        {
            await limite.WaitAsync();
            try
            {
                var fila = await evaluador.EvaluarAsync(proveedor, argumentos.Conjunto, id, CancellationToken.None);
                Console.Error.WriteLine($"[{proveedor.Id}] {Interlocked.Increment(ref hechos)}/{casos.Count} {id}{(fila.Error is null ? "" : $" ERROR {fila.Error}")}");
                return fila;
            }
            finally
            {
                limite.Release();
            }
        });
        return await Task.WhenAll(tareas);
    }

    private static void Imprimir(IReadOnlyList<ResumenEvaluacion> resumenes)
    {
        var filas = new (string Nombre, Func<ResumenEvaluacion, string> Valor)[]
        {
            ("Casos (errores)", r => $"{r.Total} ({r.Errores})"),
            ("Exactitud tipo", r => Pct(r.ExactitudTipo)),
            ("Exactitud área", r => Pct(r.ExactitudArea)),
            ("Exactitud tipología", r => Pct(r.ExactitudTipologia)),
            ("Exactitud activo", r => Pct(r.ExactitudActivo)),
            ("Exactitud en automáticos", r => Pct(r.ExactitudEnAutomaticos)),
            ("Tutela recall", r => $"{Pct(r.Tutela.Recall)} (vp {r.Tutela.Vp}, fn {r.Tutela.Fn})"),
            ("Tutela precisión", r => $"{Pct(r.Tutela.Precision)} (fp {r.Tutela.Fp})"),
            ("% automático / gestor / tutela", r => $"{Pct(r.PorcentajeAutomatico)} / {Pct(r.PorcentajeGestor)} / {Pct(r.PorcentajeTutela)}"),
            ("Latencia p50 / p95", r => $"{r.LatenciaP50Ms} / {r.LatenciaP95Ms} ms"),
            ("Costo por 1.000 PQRS", r => $"US$ {r.CostoUsdPorMil.ToString("0.0000", CultureInfo.InvariantCulture)}"),
        };
        Console.WriteLine($"{"",-32}{string.Concat(resumenes.Select(r => $"{r.Proveedor,-30}"))}");
        foreach (var (nombre, valor) in filas)
            Console.WriteLine($"{nombre,-32}{string.Concat(resumenes.Select(r => $"{valor(r),-30}"))}");
    }

    private static async Task GuardarAsync(ReporteEvaluacion reporte, string? salida, string raizDemo)
    {
        if (string.IsNullOrWhiteSpace(salida)) return;
        var ruta = Path.GetFullPath(salida, raizDemo);
        Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
        await File.WriteAllTextAsync(ruta, JsonSerializer.Serialize(reporte, Json.Indentado));
        Console.WriteLine($"Resultados guardados en {ruta}");
    }

    private static string Pct(double? valor) => valor is null ? "n/a" : $"{valor.Value * 100:0} %";
}

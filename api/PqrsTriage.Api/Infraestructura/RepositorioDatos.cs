using System.Text.Json;
using PqrsTriage.Api.Dominio;

namespace PqrsTriage.Api.Infraestructura;

public sealed record ConjuntoInfo(string Id, string Nombre, int Total, int Etiquetados);

public sealed record CasoEncontrado(CasoEvaluacion Caso, string Conjunto);

public sealed class RepositorioDatos
{
    public const string ConjuntoMockup = "mockup";
    public const string ConjuntoDificiles = "dificiles";
    public const string ConjuntoTodos = "todos";

    public RepositorioDatos(string ruta)
    {
        Ruta = ruta;
        CatalogoInicial = Leer<Catalogo>("catalogo.json");
        Mockup = Leer<List<CasoEvaluacion>>("casos-mockup.json");
        Dificiles = Leer<List<CasoEvaluacion>>("casos-dificiles.json");
        IdsBandeja = Leer<List<string>>("bandeja.json");
        Plantillas = Leer<Dictionary<string, Plantilla>>("plantillas-correo.json");
    }

    public string Ruta { get; }
    public Catalogo CatalogoInicial { get; }
    public IReadOnlyList<CasoEvaluacion> Mockup { get; }
    public IReadOnlyList<CasoEvaluacion> Dificiles { get; }
    public IReadOnlyList<string> IdsBandeja { get; }
    public IReadOnlyDictionary<string, Plantilla> Plantillas { get; }

    public static string BuscarRuta(string? configurada)
    {
        if (!string.IsNullOrWhiteSpace(configurada)) return ValidarRuta(Path.GetFullPath(configurada));
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidata = Path.Combine(dir.FullName, "data");
            if (File.Exists(Path.Combine(candidata, "catalogo.json"))) return candidata;
        }
        throw new InvalidOperationException("No encontré la carpeta data/ con catalogo.json. Configura Datos:Ruta.");
    }

    public IReadOnlyList<CasoEvaluacion> Conjunto(string id) => id switch
    {
        ConjuntoMockup => Mockup,
        ConjuntoDificiles => Dificiles,
        ConjuntoTodos => [.. Mockup, .. Dificiles],
        _ => throw new ArgumentException($"Conjunto desconocido: {id}. Usa mockup, dificiles o todos."),
    };

    public IReadOnlyList<ConjuntoInfo> Conjuntos() =>
    [
        Info(ConjuntoMockup, "Casos del mockup de diseño", Mockup),
        Info(ConjuntoDificiles, "Casos difíciles", Dificiles),
        Info(ConjuntoTodos, "Todos", Conjunto(ConjuntoTodos)),
    ];

    public CasoEncontrado? Buscar(string conjunto, string casoId)
    {
        var enMockup = Mockup.FirstOrDefault(c => c.Id == casoId);
        var caso = enMockup ?? Dificiles.FirstOrDefault(c => c.Id == casoId);
        if (caso is null || !Conjunto(conjunto).Contains(caso)) return null;
        return new CasoEncontrado(caso, enMockup is null ? ConjuntoDificiles : ConjuntoMockup);
    }

    public IReadOnlyList<CasoEntrada> Bandeja() =>
        IdsBandeja.Select(id => Buscar(ConjuntoTodos, id)?.Caso.AEntrada()).OfType<CasoEntrada>().ToList();

    private static ConjuntoInfo Info(string id, string nombre, IReadOnlyList<CasoEvaluacion> casos) =>
        new(id, nombre, casos.Count, casos.Count(c => c.Esperado is not null));

    private T Leer<T>(string archivo)
    {
        using var flujo = File.OpenRead(Path.Combine(Ruta, archivo));
        return JsonSerializer.Deserialize<T>(flujo, Json.Relajado) ?? throw new InvalidOperationException($"{archivo} está vacío");
    }

    private static string ValidarRuta(string ruta) =>
        File.Exists(Path.Combine(ruta, "catalogo.json")) ? ruta : throw new InvalidOperationException($"No existe {ruta}/catalogo.json");
}

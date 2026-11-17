using PqrsTriage.Api.Dominio;
using PqrsTriage.Api.Evaluacion;
using PqrsTriage.Api.Infraestructura;

namespace PqrsTriage.Api.Endpoints;

public static class MapaEndpoints
{
    public static WebApplication MapearEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api");
        MapearCatalogo(api);
        MapearTriage(api);
        MapearEvaluacion(api);
        return app;
    }

    private static void MapearCatalogo(RouteGroupBuilder api)
    {
        api.MapGet("/proveedores", (RegistroProveedores registro, CancellationToken ct) => registro.ListarAsync(ct));
        api.MapGet("/catalogo", (AlmacenCatalogo almacen) => almacen.Actual);
        api.MapPut("/catalogo/areas/{id}", (string id, SolicitudCorreoArea solicitud, AlmacenCatalogo almacen) => ActualizarCorreoArea(id, solicitud, almacen));
        api.MapPut("/catalogo/umbrales", (Umbrales? umbrales, AlmacenCatalogo almacen) =>
        {
            var validos = Validaciones.Umbrales(umbrales);
            return almacen.Actualizar(c => c with { Umbrales = validos }).Umbrales;
        });
        api.MapGet("/bandeja", (RepositorioDatos datos) => datos.Bandeja());
        api.MapGet("/correos", (BandejaSalida bandeja) => bandeja.Todos());
    }

    private static void MapearTriage(RouteGroupBuilder api)
    {
        api.MapPost("/triage", (SolicitudTriage s, ServicioTriage triage, CancellationToken ct) =>
            triage.ClasificarAsync(Validaciones.Texto(s.Proveedor, "el proveedor"), Validaciones.Caso(s.Caso), ct));
        api.MapPost("/triage/decidir", (SolicitudDecidir s, ServicioTriage triage) =>
            triage.Decidir(Validaciones.Caso(s.Caso), s.Respuestas ?? throw new EntradaInvalida("Faltan las respuestas del modelo.")));
        api.MapPost("/triage/corregir", (SolicitudCorregir s, ServicioTriage triage) =>
            triage.Corregir(
                Validaciones.Caso(s.Caso),
                s.Respuestas ?? throw new EntradaInvalida("Faltan las respuestas del modelo."),
                Validaciones.Texto(s.Motivo, "la tipología")));
        api.MapPost("/triage/enrutar", (SolicitudEnrutar s, ContadorRadicados contador, BandejaSalida bandeja, TimeProvider reloj) =>
        {
            Validaciones.Caso(s.Caso);
            var decision = s.Decision ?? throw new EntradaInvalida("Falta la decisión.");
            var radicado = contador.Siguiente();
            return new { radicado, correos = bandeja.Guardar(radicado, decision.Correos, reloj.GetLocalNow()) };
        });
    }

    private static void MapearEvaluacion(RouteGroupBuilder api)
    {
        api.MapGet("/evaluacion/conjuntos", (RepositorioDatos datos) => datos.Conjuntos());
        api.MapGet("/evaluacion/guardados", (RepositorioDatos datos) => ResultadosGuardados.Listar(datos));
        api.MapGet("/evaluacion/casos", (string? conjunto, RepositorioDatos datos) => datos.Conjunto(conjunto ?? RepositorioDatos.ConjuntoTodos));
        api.MapPost("/evaluacion/caso", async (SolicitudEvaluarCaso s, RegistroProveedores registro, Evaluador evaluador, CancellationToken ct) =>
        {
            var proveedor = await registro.ObtenerDisponibleAsync(Validaciones.Texto(s.Proveedor, "el proveedor"), ct);
            return await evaluador.EvaluarAsync(proveedor, s.Conjunto ?? RepositorioDatos.ConjuntoTodos, Validaciones.Texto(s.CasoId, "el caso"), ct);
        });
        api.MapPost("/evaluacion/resumen", (SolicitudResumen s) =>
            CalculadoraResumen.Resumir(Validaciones.Texto(s.Proveedor, "el proveedor"), s.Filas ?? []));
    }

    private static Area ActualizarCorreoArea(string id, SolicitudCorreoArea solicitud, AlmacenCatalogo almacen)
    {
        var correo = Validaciones.Correo(solicitud.Correo);
        if (almacen.Actual.BuscarArea(id) is null) throw new KeyNotFoundException($"No existe el área {id}.");
        return almacen.Actualizar(c => c.ConCorreoDeArea(id, correo)).BuscarArea(id)!;
    }
}

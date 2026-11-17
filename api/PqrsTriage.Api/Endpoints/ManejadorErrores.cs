using PqrsTriage.Api.Infraestructura;
using PqrsTriage.Api.Proveedores;

namespace PqrsTriage.Api.Endpoints;

public sealed class ManejadorErrores(RequestDelegate siguiente, ILogger<ManejadorErrores> logger)
{
    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await siguiente(contexto);
        }
        catch (OperationCanceledException) when (contexto.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("El cliente canceló {Ruta}", contexto.Request.Path);
        }
        catch (Exception ex) when (!contexto.Response.HasStarted)
        {
            var estado = Estado(ex);
            if (estado == StatusCodes.Status500InternalServerError) logger.LogError(ex, "Error no controlado");
            contexto.Response.StatusCode = estado;
            await contexto.Response.WriteAsJsonAsync(new ErrorApi(Mensaje(ex, estado)), Json.Relajado);
        }
    }

    private static int Estado(Exception ex) => ex switch
    {
        EntradaInvalida or ProveedorDesconocido or ArgumentException => StatusCodes.Status400BadRequest,
        KeyNotFoundException => StatusCodes.Status404NotFound,
        ProveedorNoDisponible => StatusCodes.Status503ServiceUnavailable,
        ErrorProveedor or HttpRequestException or TaskCanceledException => StatusCodes.Status502BadGateway,
        _ => StatusCodes.Status500InternalServerError,
    };

    private static string Mensaje(Exception ex, int estado) => estado switch
    {
        StatusCodes.Status500InternalServerError => "Error interno de la demo. Revisa la consola de la API.",
        StatusCodes.Status502BadGateway when ex is TaskCanceledException => "El proveedor no respondió a tiempo.",
        _ => ex.Message,
    };
}

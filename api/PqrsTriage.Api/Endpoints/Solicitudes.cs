using System.Net.Mail;
using PqrsTriage.Api.Dominio;

namespace PqrsTriage.Api.Endpoints;

public sealed record SolicitudTriage(string? Proveedor, CasoEntrada? Caso);

public sealed record SolicitudEnrutar(CasoEntrada? Caso, Decision? Decision);

public sealed record SolicitudDecidir(CasoEntrada? Caso, IReadOnlyDictionary<string, Respuesta>? Respuestas);

public sealed record SolicitudCorregir(CasoEntrada? Caso, IReadOnlyDictionary<string, Respuesta>? Respuestas, string? Motivo);

public sealed record SolicitudCorreoArea(string? Correo);

public sealed record SolicitudEvaluarCaso(string? Proveedor, string? Conjunto, string? CasoId);

public sealed record SolicitudResumen(string? Proveedor, IReadOnlyList<Evaluacion.FilaEvaluacion>? Filas);

public sealed record ErrorApi(string Error);

public sealed class EntradaInvalida(string mensaje) : Exception(mensaje);

public static class Validaciones
{
    public static CasoEntrada Caso(CasoEntrada? caso)
    {
        if (caso is null) throw new EntradaInvalida("Falta el caso.");
        if (caso.Remitente is null || !EsCorreo(caso.Remitente.Correo)) throw new EntradaInvalida("El correo del remitente no es válido.");
        if (string.IsNullOrWhiteSpace(caso.Descripcion) && string.IsNullOrWhiteSpace(caso.Asunto)) throw new EntradaInvalida("Escribe el asunto o el mensaje del caso.");
        return caso with { Canal = string.IsNullOrWhiteSpace(caso.Canal) ? "Portal" : caso.Canal, Asunto = caso.Asunto ?? "", Descripcion = caso.Descripcion ?? "" };
    }

    public static string Texto(string? valor, string campo) =>
        string.IsNullOrWhiteSpace(valor) ? throw new EntradaInvalida($"Falta {campo}.") : valor;

    public static string Correo(string? correo) =>
        EsCorreo(correo) ? correo!.Trim() : throw new EntradaInvalida("El correo no tiene un formato válido.");

    public static Umbrales Umbrales(Umbrales? u)
    {
        if (u is null) throw new EntradaInvalida("Faltan los umbrales.");
        double[] fracciones = [u.ConfianzaTipologia, u.EncajaCatalogo, u.Tutela, u.DerechoPeticion, u.RiesgoLegal];
        if (fracciones.Any(f => f is < 0 or > 1)) throw new EntradaInvalida("Los umbrales de probabilidad van de 0 a 1.");
        if (u.UrgenciaCritica is < 0 or > 3) throw new EntradaInvalida("El umbral de urgencia crítica va de 0 a 3.");
        return u;
    }

    private static bool EsCorreo(string? correo) =>
        !string.IsNullOrWhiteSpace(correo) && MailAddress.TryCreate(correo.Trim(), out var direccion) && direccion.Host.Contains('.');
}

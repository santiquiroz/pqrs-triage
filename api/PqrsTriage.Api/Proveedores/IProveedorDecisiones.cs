using System.Text.Json.Nodes;
using PqrsTriage.Api.Dominio;

namespace PqrsTriage.Api.Proveedores;

public sealed record ProveedorInfo(
    string Id,
    string Nombre,
    string Tipo,
    string Modelo,
    string Privacidad,
    double CostoUsdPorMillonTokens,
    string Descripcion);

public sealed record ProveedorDto(
    string Id,
    string Nombre,
    string Tipo,
    string Modelo,
    bool Disponible,
    string? MotivoNoDisponible,
    string Privacidad,
    double CostoUsdPorMillonTokens,
    string Descripcion);

public sealed record ResultadoDecisiones(
    IReadOnlyDictionary<string, Respuesta> Respuestas,
    int Llamadas,
    int TokensEntrada,
    double CostoUsd,
    string Modelo,
    IReadOnlyList<JsonNode> Solicitudes,
    IReadOnlyList<JsonNode> RespuestasCrudas);

public interface IProveedorDecisiones
{
    string Id { get; }

    ProveedorInfo Info();

    Task<string?> MotivoNoDisponibleAsync(CancellationToken ct);

    Task<ResultadoDecisiones> DecidirAsync(IReadOnlyDictionary<string, string> estado, IReadOnlyList<Pregunta> preguntas, CancellationToken ct);
}

public sealed class ErrorProveedor(string mensaje) : Exception(mensaje);

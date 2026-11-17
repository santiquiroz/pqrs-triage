using System.Collections.Concurrent;
using PqrsTriage.Api.Proveedores;

namespace PqrsTriage.Api.Infraestructura;

public sealed class ProveedorDesconocido(string id) : Exception($"Proveedor desconocido: {id}");

public sealed class ProveedorNoDisponible(string motivo) : Exception(motivo);

public sealed class RegistroProveedores(IServiceProvider servicios, TimeProvider reloj)
{
    private static readonly TimeSpan VigenciaCache = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan TiempoMaximoRevision = TimeSpan.FromSeconds(4);
    private readonly ConcurrentDictionary<string, (DateTimeOffset Revisado, string? Motivo)> cache = new();

    public async Task<IReadOnlyList<ProveedorDto>> ListarAsync(CancellationToken ct)
    {
        var proveedores = Todos();
        var motivos = await Task.WhenAll(proveedores.Select(p => MotivoAsync(p, ct)));
        return proveedores.Select((p, i) => Dto(p.Info(), motivos[i])).ToList();
    }

    public async Task<IProveedorDecisiones> ObtenerDisponibleAsync(string id, CancellationToken ct)
    {
        var proveedor = Todos().FirstOrDefault(p => p.Id == id) ?? throw new ProveedorDesconocido(id);
        var motivo = await MotivoAsync(proveedor, ct);
        return motivo is null ? proveedor : throw new ProveedorNoDisponible(motivo);
    }

    public async Task<IReadOnlyList<IProveedorDecisiones>> DisponiblesAsync(CancellationToken ct)
    {
        var proveedores = Todos();
        var motivos = await Task.WhenAll(proveedores.Select(p => MotivoAsync(p, ct)));
        return proveedores.Where((_, i) => motivos[i] is null).ToList();
    }

    private IReadOnlyList<IProveedorDecisiones> Todos() => servicios.GetServices<IProveedorDecisiones>().ToList();

    private async Task<string?> MotivoAsync(IProveedorDecisiones proveedor, CancellationToken ct)
    {
        var ahora = reloj.GetUtcNow();
        if (cache.TryGetValue(proveedor.Id, out var guardado) && ahora - guardado.Revisado < VigenciaCache) return guardado.Motivo;
        var motivo = await RevisarAsync(proveedor, ct);
        cache[proveedor.Id] = (ahora, motivo);
        return motivo;
    }

    private static async Task<string?> RevisarAsync(IProveedorDecisiones proveedor, CancellationToken ct)
    {
        using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct);
        limite.CancelAfter(TiempoMaximoRevision);
        try
        {
            return await proveedor.MotivoNoDisponibleAsync(limite.Token);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            return $"{proveedor.Info().Nombre} no responde ({ex.GetType().Name})";
        }
    }

    private static ProveedorDto Dto(ProveedorInfo i, string? motivo) =>
        new(i.Id, i.Nombre, i.Tipo, i.Modelo, motivo is null, motivo, i.Privacidad, i.CostoUsdPorMillonTokens, i.Descripcion);
}

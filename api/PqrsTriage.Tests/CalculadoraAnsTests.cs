using PqrsTriage.Api.Dominio;

namespace PqrsTriage.Tests;

public class CalculadoraAnsTests
{
    private static readonly TimeSpan Colombia = TimeSpan.FromHours(-5);

    [Theory]
    [InlineData(2026, 11, 13, 10, 1, 2026, 11, 17, 10)]
    [InlineData(2026, 11, 14, 10, 1, 2026, 11, 18, 0)]
    [InlineData(2026, 11, 11, 9, 5, 2026, 11, 19, 9)]
    public void Vence_salta_fines_de_semana_y_festivos(int a, int m, int d, int h, int dias, int ea, int em, int ed, int eh)
    {
        var inicio = new DateTimeOffset(a, m, d, h, 0, 0, Colombia).ToUnixTimeMilliseconds();

        var vence = CalculadoraAns.Vence(inicio, dias, Apoyo.Catalogo.Festivos);

        Assert.Equal(new DateTimeOffset(ea, em, ed, eh, 0, 0, Colombia), vence);
        Assert.Equal(Colombia, vence.Offset);
    }

    [Fact]
    public void VenceIso_usa_zona_de_Colombia()
    {
        var inicio = new DateTimeOffset(2026, 11, 13, 10, 0, 0, Colombia).ToUnixTimeMilliseconds();

        Assert.Equal("2026-11-17T10:00:00-05:00", CalculadoraAns.VenceIso(inicio, 1, Apoyo.Catalogo.Festivos));
    }
}

using PqrsTriage.Api.Dominio;

namespace PqrsTriage.Tests;

public class ExtractorDatosTests
{
    [Fact]
    public void Extrae_nit_factura_y_local()
    {
        var extraidos = ExtractorDatos.Extraer("Somos NIT 900.123.456-7, la factura FE-88213 del local 104 llegó mal. Llamar al 310 555 1234.");

        Assert.Equal(["900.123.456-7"], extraidos.Nits);
        Assert.Equal(["FE-88213"], extraidos.Facturas);
        Assert.Equal(["Local 104"], extraidos.Locales);
        Assert.Equal(["310 555 1234"], extraidos.Telefonos);
    }

    [Fact]
    public void Extrae_radicado_y_juzgado_de_una_tutela()
    {
        var caso = Apoyo.Repositorio.Dificiles.First(c => c.Id == "DIF-01").AEntrada();

        var extraidos = ExtractorDatos.Extraer(caso);

        Assert.Contains("05001-31-03-012-2026-00321-00", extraidos.RadicadosJudiciales);
        Assert.Contains(extraidos.Juzgados, j => j.StartsWith("Juzgado Doce Civil del Circuito de Medellín"));
        Assert.Empty(extraidos.Telefonos);
    }
}

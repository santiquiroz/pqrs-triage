using PqrsTriage.Api.Dominio;

namespace PqrsTriage.Tests;

public class DeclaracionClienteTests
{
    private const string Motivo = "Corrección facturación";

    private static CasoEntrada CasoConTipo(string tipo) => Apoyo.Caso() with { TipoDeclarado = tipo };

    [Fact]
    public void El_tipo_del_cliente_manda_y_el_desacuerdo_queda_como_nota()
    {
        var decision = Apoyo.Decidir(Apoyo.Respuestas(Motivo, tipo: "Queja"), CasoConTipo("Petición"));

        Assert.Equal("Petición", decision.Tipo);
        Assert.Contains("«Queja»", Assert.Single(decision.Notas));
        Assert.Contains(decision.Correos, c => c.Tipo == TiposCorreo.NotificacionArea && c.CuerpoHtml.Contains("Nota de la IA"));
    }

    [Fact]
    public void La_tipologia_la_elige_el_modelo()
    {
        var decision = Apoyo.Decidir(Apoyo.Respuestas(Motivo), CasoConTipo("Petición"));

        Assert.Equal(Motivo, decision.Motivo);
        Assert.Equal(Destinos.Automatica, decision.Destino);
    }

    [Fact]
    public void No_deja_nota_si_el_modelo_coincide_con_el_cliente()
    {
        var decision = Apoyo.Decidir(Apoyo.Respuestas(Motivo, tipo: "Petición"), CasoConTipo("Petición"));

        Assert.Empty(decision.Notas);
        Assert.DoesNotContain(decision.Correos, c => c.CuerpoHtml.Contains("Nota de la IA"));
    }

    [Fact]
    public void La_tutela_que_detecta_el_modelo_gana_al_tipo_del_cliente()
    {
        var decision = Apoyo.Decidir(Apoyo.Respuestas(Motivo, tutela: 0.9), CasoConTipo("Felicitación"));

        Assert.Equal(Destinos.Tutela, decision.Destino);
    }

    [Fact]
    public void Rechaza_un_tipo_que_no_esta_en_el_catalogo()
    {
        Assert.Throws<ArgumentException>(() => DeclaracionCliente.Validar(CasoConTipo("Inventado"), Apoyo.Catalogo));
    }
}

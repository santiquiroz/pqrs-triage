using PqrsTriage.Api.Dominio;

namespace PqrsTriage.Tests;

public class CorreccionHumanaTests
{
    [Fact]
    public void Corregir_una_tutela_a_otra_tipologia_la_saca_de_juridica()
    {
        var respuestas = Apoyo.Respuestas("Corrección facturación", tutela: 0.9);

        var decision = Apoyo.Decidir(CorreccionHumana.ConMotivo(respuestas, "Corrección facturación"));

        Assert.NotEqual(Destinos.Tutela, decision.Destino);
        Assert.Equal("Corrección facturación", decision.Motivo);
        Assert.Contains(decision.Correos, c => c.Tipo == TiposCorreo.NotificacionArea);
    }

    [Fact]
    public void Corregir_con_confianza_baja_asigna_automaticamente()
    {
        var respuestas = Apoyo.Respuestas("Corrección facturación", confianzaTipologia: 0.3);

        var decision = Apoyo.Decidir(CorreccionHumana.ConMotivo(respuestas, "Corrección facturación"));

        Assert.Equal(Destinos.Automatica, decision.Destino);
    }
}

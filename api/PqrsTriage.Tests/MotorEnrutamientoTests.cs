using PqrsTriage.Api.Dominio;

namespace PqrsTriage.Tests;

public class MotorEnrutamientoTests
{
    private const string Juridica = "juridica@suruworks.example";
    private const string Mesa = "mesa-pqrs@suruworks.example";

    [Fact]
    public void Tutela_va_a_juridica_sin_acuse_al_remitente()
    {
        var decision = Apoyo.Decidir(Apoyo.Respuestas("Corrección facturación", tutela: 0.9));

        Assert.Equal(Destinos.Tutela, decision.Destino);
        Assert.Equal("juridica", decision.Area!.Id);
        Assert.Equal(Prioridades.Tutela, decision.Prioridad);
        Assert.Equal(2, decision.AnsDias);
        Assert.True(decision.RequiereRevisionHumana);
        Assert.DoesNotContain(decision.Correos, c => c.Tipo == TiposCorreo.AcuseCliente);
        var alerta = Assert.Single(decision.Correos);
        Assert.Equal([Juridica], alerta.Para);
        Assert.Equal([Mesa], alerta.Cc);
    }

    [Fact]
    public void Baja_confianza_en_tipologia_va_a_la_mesa_con_sugerencia()
    {
        var decision = Apoyo.Decidir(Apoyo.Respuestas("Corrección facturación", confianzaTipologia: 0.4));

        Assert.Equal(Destinos.Gestor, decision.Destino);
        Assert.Equal("Corrección facturación", decision.Motivo);
        Assert.NotNull(decision.Area);
        Assert.Null(decision.Responsable);
        Assert.Contains(decision.Correos, c => c.Tipo == TiposCorreo.BandejaGestor);
        Assert.Contains(decision.Correos, c => c.Tipo == TiposCorreo.AcuseCliente);
    }

    [Fact]
    public void Si_no_encaja_en_el_catalogo_no_hay_acuse()
    {
        var decision = Apoyo.Decidir(Apoyo.Respuestas("Corrección facturación", encaja: 0.1));

        Assert.Equal(Destinos.Gestor, decision.Destino);
        Assert.DoesNotContain(decision.Correos, c => c.Tipo == TiposCorreo.AcuseCliente);
    }

    [Fact]
    public void Asignacion_automatica_notifica_al_area_y_al_cliente()
    {
        var motivo = Apoyo.Catalogo.BuscarMotivo("Corrección facturación")!;

        var decision = Apoyo.Decidir(Apoyo.Respuestas(motivo.Motivo.Nombre));

        Assert.Equal(Destinos.Automatica, decision.Destino);
        Assert.NotNull(decision.Responsable);
        Assert.Equal(motivo.Area.Correo, decision.Correos.First(c => c.Tipo == TiposCorreo.NotificacionArea).Para.Single());
        Assert.Equal("ana@tiendauno.example", decision.Correos.First(c => c.Tipo == TiposCorreo.AcuseCliente).Para.Single());
        Assert.Contains(GeneradorCorreos.RadicadoProvisional, decision.Correos[0].Asunto);
    }

    [Fact]
    public void Grupo_sin_responsable_va_a_la_mesa()
    {
        var motivo = Apoyo.Catalogo.Motivos().First(m => m.Grupo.Nombre == "Actualización datos Maestros").Motivo.Nombre;

        var decision = Apoyo.Decidir(Apoyo.Respuestas(motivo));

        Assert.Equal(Destinos.Gestor, decision.Destino);
        Assert.Contains(decision.Razones, r => r.Disparada && r.Detalle == "La categoría no tiene responsable configurado");
    }

    [Fact]
    public void Derecho_de_peticion_limita_el_ans_a_15_dias()
    {
        var baja = Apoyo.MotivoConPrioridad("Baja");

        var decision = Apoyo.Decidir(Apoyo.Respuestas(baja, derechoPeticion: 0.9));

        Assert.Equal(10, decision.AnsDias);
        Assert.Contains(Modificadores.EtiquetaDerechoPeticion, decision.Etiquetas);
    }

    [Fact]
    public void La_urgencia_nunca_baja_la_prioridad_del_catalogo()
    {
        var urgente = Apoyo.MotivoConPrioridad("Urgente");

        var decision = Apoyo.Decidir(Apoyo.Respuestas(urgente, nivelUrgencia: 0));

        Assert.Equal("Urgente", decision.Prioridad);
    }

    [Fact]
    public void La_urgencia_critica_sube_la_prioridad_y_copia_a_la_mesa()
    {
        var baja = Apoyo.MotivoConPrioridad("Baja");

        var decision = Apoyo.Decidir(Apoyo.Respuestas(baja, nivelUrgencia: 3));

        Assert.Equal("Urgente", decision.Prioridad);
        Assert.Equal(5, decision.AnsDias);
        Assert.Contains(Mesa, decision.Correos.First(c => c.Tipo == TiposCorreo.NotificacionArea).Cc);
    }

    [Fact]
    public void Riesgo_legal_copia_a_juridica()
    {
        var decision = Apoyo.Decidir(Apoyo.Respuestas("Corrección facturación", riesgoLegal: 0.9));

        Assert.Contains(Modificadores.EtiquetaRiesgoLegal, decision.Etiquetas);
        Assert.Contains(Juridica, decision.Correos.First(c => c.Tipo == TiposCorreo.NotificacionArea).Cc);
    }

    [Fact]
    public void El_activo_declarado_manda_sobre_el_juzgado()
    {
        var caso = Apoyo.Caso() with { ActivoDeclarado = "Centro Comercial Arrayanes" };

        var decision = Apoyo.Decidir(Apoyo.Respuestas("Corrección facturación", activo: "Bodegas El Rosal"), caso);

        Assert.Equal("Centro Comercial Arrayanes", decision.Activo);
    }

    [Fact]
    public void Los_valores_del_caso_se_escapan_en_el_html()
    {
        var caso = Apoyo.Caso("<script>alert(1)</script>");

        var decision = Apoyo.Decidir(Apoyo.Respuestas("Corrección facturación"), caso);

        Assert.All(decision.Correos, c => Assert.DoesNotContain("<script>", c.CuerpoHtml));
    }
}

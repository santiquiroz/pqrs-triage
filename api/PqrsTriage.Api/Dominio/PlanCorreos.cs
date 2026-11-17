namespace PqrsTriage.Api.Dominio;

public static class PlanCorreos
{
    public static IReadOnlyList<Envio> Calcular(Decision decision, EntradaMotor entrada, LecturaRespuestas lectura) => decision.Destino switch
    {
        Destinos.Tutela => [AlertaJuridica(entrada.Catalogo)],
        Destinos.Gestor => ParaGestor(decision, entrada, lectura),
        _ => ParaArea(decision, entrada),
    };

    private static Envio AlertaJuridica(Catalogo catalogo) =>
        new(TiposCorreo.AlertaJuridica, [CorreoJuridica(catalogo)], [catalogo.CorreoMesa]);

    private static IReadOnlyList<Envio> ParaGestor(Decision decision, EntradaMotor entrada, LecturaRespuestas lectura)
    {
        var catalogo = entrada.Catalogo;
        var para = new[] { catalogo.CorreoMesa };
        var bandeja = new Envio(TiposCorreo.BandejaGestor, para, CopiasPorModificadores(decision, catalogo, para));
        var encaja = lectura.Encaja >= catalogo.Umbrales.EncajaCatalogo;
        return encaja ? [bandeja, Acuse(entrada.Caso)] : [bandeja];
    }

    private static IReadOnlyList<Envio> ParaArea(Decision decision, EntradaMotor entrada)
    {
        var para = new[] { decision.Area!.Correo };
        var notificacion = new Envio(TiposCorreo.NotificacionArea, para, CopiasPorModificadores(decision, entrada.Catalogo, para));
        return [notificacion, Acuse(entrada.Caso)];
    }

    private static Envio Acuse(CasoEntrada caso) => new(TiposCorreo.AcuseCliente, [caso.Remitente.Correo], []);

    private static IReadOnlyList<string> CopiasPorModificadores(Decision decision, Catalogo catalogo, IReadOnlyList<string> para)
    {
        var copias = new List<string>();
        if (decision.Etiquetas.Contains(Modificadores.EtiquetaRiesgoLegal)) copias.Add(CorreoJuridica(catalogo));
        if (decision.Etiquetas.Contains(Modificadores.EtiquetaUrgenciaCritica)) copias.Add(catalogo.CorreoMesa);
        return copias.Except(para, StringComparer.OrdinalIgnoreCase).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string CorreoJuridica(Catalogo catalogo) =>
        catalogo.BuscarArea(ConsultasCatalogo.AreaJuridica)?.Correo ?? catalogo.CorreoMesa;
}

namespace PqrsTriage.Api.Dominio;

public sealed class MotorEnrutamiento(GeneradorCorreos generadorCorreos)
{
    private const string PrioridadPorDefecto = "Media";

    public Decision Decidir(EntradaMotor entrada)
    {
        var lectura = LecturaRespuestas.Desde(DeclaracionCliente.Aplicar(entrada.Respuestas, entrada.Caso));
        var razonTutela = RazonTutela(lectura, entrada.Catalogo.Umbrales);
        var decision = razonTutela.Disparada
            ? DecidirTutela(entrada, lectura, razonTutela)
            : DecidirOrdinario(entrada, lectura, razonTutela);
        var notas = DeclaracionCliente.Notas(entrada.Respuestas, entrada.Caso, entrada.Catalogo.Umbrales.ConfianzaTipologia);
        return ConCorreos(decision with { Notas = notas }, entrada, lectura);
    }

    private static Decision DecidirTutela(EntradaMotor entrada, LecturaRespuestas lectura, Razon razonTutela)
    {
        var catalogo = entrada.Catalogo;
        var juridica = catalogo.BuscarArea(ConsultasCatalogo.AreaJuridica);
        var ubicado = catalogo.BuscarMotivo(ConsultasCatalogo.MotivoTutela);
        return new Decision(
            Destinos.Tutela,
            "Jurídica: posible acción de tutela",
            null,
            juridica is null ? null : Referencia(juridica),
            ubicado?.Grupo.Nombre,
            ConsultasCatalogo.MotivoTutela,
            Responsable(catalogo, ubicado),
            Prioridades.Tutela,
            catalogo.AnsTutelaDias,
            CalculadoraAns.VenceIso(entrada.CreadoMs, catalogo.AnsTutelaDias, catalogo.Festivos),
            catalogo.TipoDias,
            ActivoDelCaso(entrada.Caso, lectura),
            ["Tutela"],
            [razonTutela],
            true,
            [],
            []);
    }

    private static Decision DecidirOrdinario(EntradaMotor entrada, LecturaRespuestas lectura, Razon razonTutela)
    {
        var catalogo = entrada.Catalogo;
        var umbrales = catalogo.Umbrales;
        var ubicado = catalogo.BuscarMotivo(lectura.Motivo);
        var responsable = Responsable(catalogo, ubicado);
        var razonEncaja = RazonEncaja(lectura, umbrales);
        var razonConfianza = RazonConfianza(lectura, umbrales, ubicado);
        var razonResponsable = RazonResponsable(ubicado, responsable);
        var esGestor = razonEncaja.Disparada || razonConfianza.Disparada || razonResponsable.Disparada;
        var modificadores = Modificadores.Calcular(lectura, catalogo, ubicado?.Motivo.Prioridad ?? PrioridadPorDefecto);
        return new Decision(
            esGestor ? Destinos.Gestor : Destinos.Automatica,
            esGestor ? "Mesa de PQRS (Por clasificar)" : $"{ubicado!.Area.Nombre} › {ubicado.Grupo.Nombre} · {responsable!.Nombre}",
            lectura.Tipo,
            ubicado is null ? null : Referencia(ubicado.Area),
            ubicado?.Grupo.Nombre,
            ubicado?.Motivo.Nombre,
            esGestor ? null : responsable,
            modificadores.Prioridad,
            modificadores.AnsDias,
            CalculadoraAns.VenceIso(entrada.CreadoMs, modificadores.AnsDias, catalogo.Festivos),
            catalogo.TipoDias,
            ActivoDelCaso(entrada.Caso, lectura),
            modificadores.Etiquetas,
            [razonTutela, razonEncaja, razonConfianza, razonResponsable, .. modificadores.Razones],
            esGestor,
            [],
            []);
    }

    private Decision ConCorreos(Decision decision, EntradaMotor entrada, LecturaRespuestas lectura)
    {
        var envios = PlanCorreos.Calcular(decision, entrada, lectura);
        return decision with { Correos = generadorCorreos.Generar(envios, decision, entrada.Caso, entrada.Extraidos) };
    }

    private static Razon RazonTutela(LecturaRespuestas l, Umbrales u) =>
        new("Tutela", $"Probabilidad de tutela {FormatoEsCo.Probabilidad(l.Tutela)} (umbral {FormatoEsCo.Numero(u.Tutela)})", l.Tutela >= u.Tutela);

    private static Razon RazonEncaja(LecturaRespuestas l, Umbrales u) =>
        new("No encaja en el catálogo", $"Alguna tipología encaja: {FormatoEsCo.Probabilidad(l.Encaja)} (umbral {FormatoEsCo.Numero(u.EncajaCatalogo)})", l.Encaja < u.EncajaCatalogo);

    private static Razon RazonConfianza(LecturaRespuestas l, Umbrales u, MotivoUbicado? ubicado)
    {
        var detalle = $"Confianza en «{l.Motivo ?? "sin tipología"}»: {FormatoEsCo.Probabilidad(l.ConfianzaTipologia)} (umbral {FormatoEsCo.Numero(u.ConfianzaTipologia)})";
        return new("Baja confianza en la tipología", detalle, ubicado is null || l.ConfianzaTipologia < u.ConfianzaTipologia);
    }

    private static Razon RazonResponsable(MotivoUbicado? ubicado, ResponsableRef? responsable)
    {
        var sinResponsable = ubicado is not null && responsable is null;
        var detalle = sinResponsable ? "La categoría no tiene responsable configurado" : "La categoría tiene responsable configurado";
        return new("Sin responsable", detalle, sinResponsable);
    }

    private static ResponsableRef? Responsable(Catalogo catalogo, MotivoUbicado? ubicado)
    {
        var usuario = catalogo.BuscarUsuario(ubicado?.Grupo.Responsable ?? ubicado?.Grupo.Suplente);
        return usuario is null ? null : new ResponsableRef(usuario.Id, usuario.Nombre, usuario.Cargo);
    }

    private static string? ActivoDelCaso(CasoEntrada caso, LecturaRespuestas lectura)
    {
        if (!string.IsNullOrWhiteSpace(caso.ActivoDeclarado)) return caso.ActivoDeclarado;
        return lectura.Activo == ConstructorPreguntas.NoIdentificado ? null : lectura.Activo;
    }

    private static AreaRef Referencia(Area area) => new(area.Id, area.Nombre, area.Correo);
}

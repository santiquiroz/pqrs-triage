namespace PqrsTriage.Api.Dominio;

public static class ConstructorPreguntas
{
    public const string NoIdentificado = "No identificado";

    public static readonly IReadOnlyList<string> NivelesUrgencia =
    [
        "Puede esperar el término normal de respuesta",
        "Debe atenderse esta semana",
        "Debe atenderse hoy: afecta la operación del cliente",
        "Crítica: riesgo para personas, bienes, o un plazo legal o judicial corriendo",
    ];

    public static IReadOnlyList<Pregunta> Construir(Catalogo catalogo) =>
    [
        PreguntaTipo(catalogo),
        PreguntaTipologia(catalogo),
        PreguntaEncajaCatalogo(),
        PreguntaActivo(catalogo),
        PreguntaUrgencia(),
        PreguntaTutela(),
        PreguntaDerechoPeticion(),
        PreguntaRiesgoLegal(),
    ];

    public static IReadOnlyDictionary<string, string> ConstruirEstado(CasoEntrada caso)
    {
        var estado = new Dictionary<string, string>();
        AgregarSiTieneValor(estado, "canal", caso.Canal);
        AgregarSiTieneValor(estado, "buzon", caso.Buzon);
        AgregarSiTieneValor(estado, "remitente", DescribirRemitente(caso.Remitente));
        AgregarSiTieneValor(estado, "correoRemitente", caso.Remitente.Correo);
        AgregarSiTieneValor(estado, "activoDeclarado", caso.ActivoDeclarado);
        AgregarSiTieneValor(estado, "localDeclarado", caso.LocalDeclarado);
        AgregarSiTieneValor(estado, "asunto", caso.Asunto);
        AgregarSiTieneValor(estado, "mensaje", caso.Descripcion);
        return estado;
    }

    private static Pregunta PreguntaTipo(Catalogo catalogo) => new(
        "tipo",
        TipoPregunta.Choice,
        "¿Qué tipo de PQRS es, según estas definiciones de SuruWorks?",
        Opciones: catalogo.Tipos.Select(t => new Opcion(t.Id, t.Desc)).ToList());

    private static Pregunta PreguntaTipologia(Catalogo catalogo) => new(
        "tipologia",
        TipoPregunta.Choice,
        "¿Qué tipología del catálogo de SuruWorks describe mejor lo que el remitente pide o reporta?",
        Opciones: catalogo.Motivos().Select(DescribirMotivo).ToList());

    private static Pregunta PreguntaEncajaCatalogo() => new(
        "encaja_catalogo",
        TipoPregunta.Noul,
        "Alguna tipología del catálogo de SuruWorks describe esta solicitud.",
        Criterios: new(
            "Es una solicitud dirigida a SuruWorks como arrendador o administrador de sus activos, venga de un cliente actual o de uno interesado: trámites, contratos, facturas, pagos, espacios disponibles, eventos, mantenimiento, seguridad, quejas, sugerencias, felicitaciones o notificaciones legales.",
            "Es publicidad o venta de un tercero, spam, una respuesta automática o un tema sin relación con SuruWorks."));

    private static Pregunta PreguntaActivo(Catalogo catalogo) => new(
        "activo",
        TipoPregunta.Choice,
        "¿A qué activo inmobiliario de SuruWorks se refiere el mensaje?",
        Opciones: catalogo.Activos
            .Select(a => new Opcion(a.Nombre, a.Producto))
            .Append(new Opcion(NoIdentificado, "El mensaje no menciona ninguno de estos activos."))
            .ToList());

    private static Pregunta PreguntaUrgencia() => new(
        "urgencia",
        TipoPregunta.Score,
        "¿Qué tan urgente es atender este caso?",
        Niveles: NivelesUrgencia);

    private static Pregunta PreguntaTutela() => new(
        "tutela",
        TipoPregunta.Noul,
        "El mensaje es una acción de tutela o una comunicación de un juzgado sobre una tutela (auto admisorio, requerimiento, fallo o incidente de desacato).",
        Criterios: new(
            "Lo envía o reenvía un despacho judicial y trata de una acción de tutela contra SuruWorks.",
            "Es una queja, reclamo o petición común, aunque mencione abogados o amenace con poner una tutela."));

    private static Pregunta PreguntaDerechoPeticion() => new(
        "derecho_peticion",
        TipoPregunta.Noul,
        "El remitente invoca expresamente el derecho de petición (artículo 23 de la Constitución o Ley 1755 de 2015).");

    private static Pregunta PreguntaRiesgoLegal() => new(
        "riesgo_legal",
        TipoPregunta.Noul,
        "El mensaje trae un riesgo legal para SuruWorks: amenaza de demanda, denuncia, tutela o queja ante una superintendencia, o es un requerimiento de una autoridad.");

    private static Opcion DescribirMotivo(MotivoUbicado m) =>
        new(m.Motivo.Nombre, $"{m.Area.Nombre} › {m.Grupo.Nombre}. Para el cliente: {m.Motivo.NombreCliente}");

    private static string DescribirRemitente(Remitente r)
    {
        var empresa = string.IsNullOrWhiteSpace(r.Empresa) ? "" : $", {r.Empresa}";
        var tipo = string.IsNullOrWhiteSpace(r.Tipo) ? "" : $" ({r.Tipo})";
        return $"{r.Nombre}{empresa}{tipo}";
    }

    private static void AgregarSiTieneValor(Dictionary<string, string> estado, string clave, string? valor)
    {
        if (!string.IsNullOrWhiteSpace(valor)) estado[clave] = valor;
    }
}

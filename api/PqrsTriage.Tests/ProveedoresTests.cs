using System.Text.Json.Nodes;
using PqrsTriage.Api.Dominio;
using PqrsTriage.Api.Proveedores.Jev;
using PqrsTriage.Api.Proveedores.Reglas;
using PqrsTriage.Api.Proveedores.Tev1;

namespace PqrsTriage.Tests;

public class ProveedorJevTests
{
    private static readonly IReadOnlyDictionary<string, string> Estado = new Dictionary<string, string> { ["asunto"] = "Factura", ["mensaje"] = "Me cobraron doble" };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Lee_respuestas_oficiales_y_del_revendedor(bool envueltaEnData)
    {
        var manejador = new ManejadorFalso(cuerpo => Envolver(RespuestasPara(cuerpo!), envueltaEnData));
        var proveedor = Crear(manejador, maxPreguntas: 0);

        var resultado = await proveedor.DecidirAsync(Estado, Apoyo.Preguntas, CancellationToken.None);

        Assert.Equal(1, resultado.Llamadas);
        Assert.Equal(120, resultado.TokensEntrada);
        Assert.Equal("Reclamo", resultado.Respuestas["tipo"].Eleccion);
        Assert.Equal(0.8, resultado.Respuestas["tipo"].Confianza);
        Assert.Equal(1.4, resultado.Respuestas["urgencia"].Puntaje);
        Assert.Equal(0.1, resultado.Respuestas["tutela"].Probabilidad);
        Assert.True(resultado.Respuestas["tutela"].ConfianzaCalibrada);
    }

    [Fact]
    public async Task Parte_las_preguntas_en_lotes()
    {
        var manejador = new ManejadorFalso(cuerpo => Envolver(RespuestasPara(cuerpo!), true));
        var proveedor = Crear(manejador, maxPreguntas: 3);

        var resultado = await proveedor.DecidirAsync(Estado, Apoyo.Preguntas, CancellationToken.None);

        Assert.Equal(3, resultado.Llamadas);
        Assert.Equal(8, resultado.Respuestas.Count);
        Assert.All(manejador.Cuerpos, c => Assert.True(c!["questions"]!.AsObject().Count <= 3));
    }

    [Fact]
    public async Task Envia_criterios_tipados()
    {
        var manejador = new ManejadorFalso(cuerpo => Envolver(RespuestasPara(cuerpo!), false));

        await Crear(manejador, 0).DecidirAsync(Estado, Apoyo.Preguntas, CancellationToken.None);

        var preguntas = manejador.Cuerpos.Single()!["questions"]!;
        Assert.Equal("choice", preguntas["tipologia"]!["type"]!.GetValue<string>());
        Assert.Equal(38, preguntas["tipologia"]!["criteria"]!.AsObject().Count);
        Assert.Equal(4, preguntas["urgencia"]!["criteria"]!.AsArray().Count);
        Assert.NotNull(preguntas["tutela"]!["criteria"]!["true"]);
        Assert.Null(preguntas["derecho_peticion"]!["criteria"]);
    }

    private static ProveedorJev Crear(ManejadorFalso manejador, int maxPreguntas) =>
        new(new HttpClient(manejador), new JevOpciones { ApiKey = "clave-de-prueba", MaxPreguntasPorSolicitud = maxPreguntas });

    private static JsonNode Envolver(JsonObject respuestas, bool envueltaEnData)
    {
        var resultado = new JsonObject { ["answers"] = respuestas, ["usage"] = new JsonObject { ["input_tokens"] = 120 } };
        return envueltaEnData ? new JsonObject { ["data"] = new JsonObject { ["result"] = resultado }, ["creditsUsed"] = 1 } : resultado;
    }

    private static JsonObject RespuestasPara(JsonNode cuerpo)
    {
        var respuestas = new JsonObject();
        foreach (var (id, pregunta) in cuerpo["questions"]!.AsObject()) respuestas[id] = Respuesta(id, pregunta!);
        return respuestas;
    }

    private static JsonNode Respuesta(string id, JsonNode pregunta) => pregunta["type"]!.GetValue<string>() switch
    {
        "choice" => new JsonObject
        {
            ["type"] = "choice",
            ["choice"] = id == "tipo" ? "Reclamo" : pregunta["criteria"]!.AsObject().First().Key,
            ["confidence"] = 0.8,
            ["probabilities"] = new JsonObject { ["Reclamo"] = 0.8, ["Queja"] = 0.2 },
        },
        "score" => new JsonObject
        {
            ["type"] = "score",
            ["score"] = 1.4,
            ["confidence"] = 0.6,
            ["probabilities"] = new JsonObject { ["0"] = 0.1, ["1"] = 0.5, ["2"] = 0.3, ["3"] = 0.1 },
        },
        _ => new JsonObject { ["type"] = "noul", ["noul"] = 0.1 },
    };
}

public class ProveedorTev1Tests
{
    [Fact]
    public void Renormaliza_las_letras_validas_de_los_logprobs()
    {
        var respuesta = RespuestaTev1("B", ("B", Math.Log(0.6)), (" A", Math.Log(0.2)), ("Z", Math.Log(0.1)), ("hola", Math.Log(0.05)));

        var probabilidades = LectorTev1.Leer(respuesta, 3);

        Assert.True(probabilidades.DesdeLogprobs);
        Assert.Equal(1, probabilidades.Ganadora);
        Assert.Equal(0.75, probabilidades.Valores[1], 6);
        Assert.Equal(0.25, probabilidades.Valores[0], 6);
        Assert.Equal(0, probabilidades.Valores[2]);
    }

    [Fact]
    public void Sin_logprobs_usa_la_letra_con_probabilidad_uno()
    {
        var probabilidades = LectorTev1.Leer(new JsonObject { ["choices"] = new JsonArray(new JsonObject { ["message"] = new JsonObject { ["content"] = "C" } }) }, 4);

        Assert.False(probabilidades.DesdeLogprobs);
        Assert.Equal([0, 0, 1, 0], probabilidades.Valores);
    }

    [Fact]
    public void Una_letra_fuera_de_rango_es_un_error()
    {
        Assert.Throws<PqrsTriage.Api.Proveedores.ErrorProveedor>(() => LectorTev1.Leer(RespuestaTev1("Q"), 3));
    }

    [Fact]
    public async Task La_tipologia_se_resuelve_con_un_torneo()
    {
        var manejador = new ManejadorFalso(_ => RespuestaTev1("A", ("A", Math.Log(0.9)), ("B", Math.Log(0.1))));
        var proveedor = new ProveedorTev1(new HttpClient(manejador), new Tev1Opciones());

        var resultado = await proveedor.DecidirAsync(new Dictionary<string, string> { ["mensaje"] = "hola" }, [Apoyo.Pregunta("tipologia")], CancellationToken.None);

        Assert.Equal(3, resultado.Llamadas);
        var tipologia = resultado.Respuestas["tipologia"];
        Assert.Equal(Apoyo.Pregunta("tipologia").Opciones![0].Clave, tipologia.Eleccion);
        Assert.Equal(0.81, tipologia.Confianza!.Value, 6);
        Assert.All(manejador.Cuerpos, c => Assert.False(c!["chat_template_kwargs"]!["enable_thinking"]!.GetValue<bool>()));
    }

    [Fact]
    public async Task Ocho_preguntas_hacen_diez_llamadas()
    {
        var manejador = new ManejadorFalso(_ => RespuestaTev1("A", ("A", Math.Log(0.7)), ("B", Math.Log(0.3))));
        var proveedor = new ProveedorTev1(new HttpClient(manejador), new Tev1Opciones());

        var resultado = await proveedor.DecidirAsync(new Dictionary<string, string> { ["mensaje"] = "hola" }, Apoyo.Preguntas, CancellationToken.None);

        Assert.Equal(10, resultado.Llamadas);
        Assert.Equal(0.7, resultado.Respuestas["tutela"].Probabilidad!.Value, 6);
    }

    private static JsonNode RespuestaTev1(string letra, params (string Token, double Logprob)[] top) => new JsonObject
    {
        ["choices"] = new JsonArray(new JsonObject
        {
            ["message"] = new JsonObject { ["content"] = letra },
            ["logprobs"] = new JsonObject
            {
                ["content"] = new JsonArray(new JsonObject
                {
                    ["token"] = letra,
                    ["top_logprobs"] = new JsonArray(top.Select(t => (JsonNode)new JsonObject { ["token"] = t.Token, ["logprob"] = t.Logprob }).ToArray()),
                }),
            },
        }),
        ["usage"] = new JsonObject { ["prompt_tokens"] = 300 },
    };
}

public class ProveedorReglasTests
{
    [Fact]
    public async Task Detecta_tutela_solo_con_contexto_judicial()
    {
        var proveedor = new ProveedorReglas();
        var preguntas = Apoyo.Preguntas;

        var judicial = await proveedor.DecidirAsync(new Dictionary<string, string> { ["mensaje"] = "El Juzgado admite la acción de tutela" }, preguntas, CancellationToken.None);
        var amenaza = await proveedor.DecidirAsync(new Dictionary<string, string> { ["mensaje"] = "Si no me responden pongo una tutela" }, preguntas, CancellationToken.None);

        Assert.Equal(0.95, judicial.Respuestas["tutela"].Probabilidad);
        Assert.Equal(0.4, amenaza.Respuestas["tutela"].Probabilidad);
    }
}

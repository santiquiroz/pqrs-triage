using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PqrsTriage.Api;

public static class Json
{
    public static readonly JsonSerializerOptions Relajado = Configurar(new JsonSerializerOptions());

    public static readonly JsonSerializerOptions Indentado = Configurar(new JsonSerializerOptions { WriteIndented = true });

    public static JsonSerializerOptions Configurar(JsonSerializerOptions opciones)
    {
        opciones.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opciones.PropertyNameCaseInsensitive = true;
        opciones.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        opciones.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return opciones;
    }
}

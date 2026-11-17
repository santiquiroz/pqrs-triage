namespace PqrsTriage.Api.Proveedores.Llm;

public sealed class LlmOpciones
{
    public string Id { get; init; } = "llm";
    public string? Nombre { get; init; }
    public string VariableClave { get; init; } = "LLM_API_KEY";
    public string? ReasoningEffort { get; init; } = "none";
    public string BaseUrl { get; init; } = "http://localhost:11434/v1";
    public string Modelo { get; init; } = "qwen3.5-mem:latest";
    public string? ApiKey { get; init; }
    public string HeaderApiKey { get; init; } = "Authorization";
    public double CostoUsdPorMillonTokens { get; init; }
    public int TimeoutSegundos { get; init; } = 180;

    public string? ClaveEfectiva =>
        Environment.GetEnvironmentVariable(VariableClave) is { Length: > 0 } clave ? clave : ApiKey;

    public bool EsOllamaLocal => BaseUrl.Contains(":11434", StringComparison.Ordinal);

    public string RaizOllama => BaseUrl.TrimEnd('/').Replace("/v1", "", StringComparison.Ordinal);
}

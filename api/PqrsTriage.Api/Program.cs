using PqrsTriage.Api;
using PqrsTriage.Api.Endpoints;
using PqrsTriage.Api.Evaluacion;
using PqrsTriage.Api.Infraestructura;

var esComandoEvaluar = args.FirstOrDefault() == "evaluar";
var builder = WebApplication.CreateBuilder(esComandoEvaluar ? [] : args);
builder.WebHost.UseUrls(builder.Configuration["Urls"] ?? "http://localhost:5180");
builder.Services.AgregarTriage(builder.Configuration);
builder.Services.ConfigureHttpJsonOptions(o => Json.Configurar(o.SerializerOptions));
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
if (esComandoEvaluar) return await ComandoEvaluar.EjecutarAsync(app.Services, args[1..]);

app.UseMiddleware<ManejadorErrores>();
app.UseCors();
app.MapearEndpoints();
await app.RunAsync();
return 0;

public partial class Program;

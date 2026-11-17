using PqrsTriage.Api.Dominio;
using PqrsTriage.Api.Evaluacion;
using PqrsTriage.Api.Proveedores;
using PqrsTriage.Api.Proveedores.Jev;
using PqrsTriage.Api.Proveedores.Llm;
using PqrsTriage.Api.Proveedores.Reglas;
using PqrsTriage.Api.Proveedores.Tev1;

namespace PqrsTriage.Api.Infraestructura;

public static class RegistroServicios
{
    public static IServiceCollection AgregarTriage(this IServiceCollection servicios, IConfiguration configuracion)
    {
        var datos = new RepositorioDatos(RepositorioDatos.BuscarRuta(configuracion["Datos:Ruta"]));
        servicios.AddSingleton(datos);
        servicios.AddSingleton(TimeProvider.System);
        servicios.AddSingleton(new AlmacenCatalogo(datos.CatalogoInicial));
        servicios.AddSingleton(new ContadorRadicados(datos.Mockup.Concat(datos.Dificiles).Select(c => c.Id)));
        servicios.AddSingleton<BandejaSalida>();
        servicios.AddSingleton(new GeneradorCorreos(datos.Plantillas));
        servicios.AddSingleton<MotorEnrutamiento>();
        servicios.AddSingleton<RegistroProveedores>();
        servicios.AddSingleton<ServicioTriage>();
        servicios.AddSingleton<Evaluador>();
        AgregarProveedores(servicios, configuracion);
        return servicios;
    }

    private static void AgregarProveedores(IServiceCollection servicios, IConfiguration configuracion)
    {
        var jev = Opciones<JevOpciones>(configuracion, "Jev");
        var tev1 = Opciones<Tev1Opciones>(configuracion, "Tev1");
        var llm = Opciones<LlmOpciones>(configuracion, "Llm");
        servicios.AddSingleton(jev).AddSingleton(tev1).AddSingleton(llm);
        servicios.AddHttpClient<ProveedorJev>(c => c.Timeout = TimeSpan.FromSeconds(jev.TimeoutSegundos));
        servicios.AddHttpClient<ProveedorTev1>(c => c.Timeout = TimeSpan.FromSeconds(tev1.TimeoutSegundos));
        servicios.AddHttpClient<ProveedorLlm>(c => c.Timeout = TimeSpan.FromSeconds(llm.TimeoutSegundos));
        servicios.AddTransient<IProveedorDecisiones>(sp => sp.GetRequiredService<ProveedorJev>());
        servicios.AddTransient<IProveedorDecisiones>(sp => sp.GetRequiredService<ProveedorTev1>());
        servicios.AddTransient<IProveedorDecisiones>(sp => sp.GetRequiredService<ProveedorLlm>());
        servicios.AddSingleton<IProveedorDecisiones, ProveedorReglas>();
        foreach (var seccion in new[] { "Gpt4o", "Gemma" }) AgregarLlmAdicional(servicios, configuracion, seccion);
    }

    private static void AgregarLlmAdicional(IServiceCollection servicios, IConfiguration configuracion, string seccion)
    {
        if (!configuracion.GetSection(seccion).Exists()) return;
        var opciones = Opciones<LlmOpciones>(configuracion, seccion);
        servicios.AddHttpClient(opciones.Id, c => c.Timeout = TimeSpan.FromSeconds(opciones.TimeoutSegundos));
        servicios.AddTransient<IProveedorDecisiones>(sp =>
            new ProveedorLlm(sp.GetRequiredService<IHttpClientFactory>().CreateClient(opciones.Id), opciones));
    }

    private static T Opciones<T>(IConfiguration configuracion, string seccion) where T : new() =>
        configuracion.GetSection(seccion).Get<T>() ?? new T();
}

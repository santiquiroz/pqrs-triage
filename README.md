# pqrs-triage

Demo que clasifica PQRS escritas en lenguaje natural (tipo, tipología, área, prioridad, tutela/derecho de petición) con modelos de decisión y LLM, y enruta correos simulados.

SuruWorks, sus activos, personas, NIT y correos son ficticios.

## Arquitectura

- API .NET 10 en `api/` (puerto 5180).
- Web Angular 21 en `web/` (puerto 4200, proxy `/api` hacia 5180).
- Datos en `data/` (`catalogo.json`, `casos-mockup.json`, `casos-dificiles.json`, `plantillas-correo.json`, `bandeja.json`, `resultados/`).

Idea clave: "el código manda, la IA solo juzga": el modelo responde preguntas y el MotorEnrutamiento decide área, prioridad, plazos (ANS) y correos.

## Proveedores

| Proveedor | Descripción |
| --- | --- |
| `jev` | Jev de TypeSafe, API remota. Variable `JEV_API_KEY`. |
| `tev1` | Tev1-4B local con llama-server en `127.0.0.1:8091`. Ver `scripts/tev1.ps1`. |
| `llm` | LLM local con Ollama en `localhost:11434`. Modelo configurable en la sección `Llm`. |
| `gpt4o` | GPT-4o vía endpoint compatible con OpenAI, por defecto OpenRouter. Variable `GPT4O_API_KEY`. Para OpenAI directo o Azure se sobrescriben `Gpt4o__BaseUrl`, `Gpt4o__Modelo` y `Gpt4o__HeaderApiKey`. |
| `gemma` | Gemma 4 31B gratis en NVIDIA NIM. Variable `NVIDIA_NIM_API_KEY`. |
| `reglas` | Palabras clave, sin IA, línea base. |

Un proveedor sin su clave aparece como no disponible en la web.

## Cómo correr

Requisitos: .NET 10 SDK y Node 22+.

```powershell
dotnet run --project api/PqrsTriage.Api
```

```powershell
cd web; npm install; npx ng serve
```

Abrir http://localhost:4200.

Para usar un proveedor remoto, ponga su clave antes de arrancar la API:

```powershell
$env:NVIDIA_NIM_API_KEY = "<tu clave>"
```

## Evaluación por consola

```powershell
dotnet run --project api/PqrsTriage.Api -- evaluar --proveedor tev1 --conjunto todos --salida data/resultados/tev1.json
```

La vista Evaluación de la web lee esos JSON.

Nota: los JSON incluidos en `data/resultados` se generaron antes de renombrar los activos a nombres ficticios; vuelva a correr la evaluación para regenerarlos.

## Pruebas

```powershell
dotnet test api/PqrsTriage.slnx
```

```powershell
cd web; npx ng test
```

## Privacidad

Los proveedores remotos (`jev`, `gpt4o`, `gemma`) envían el texto del caso a un tercero; no use datos reales de clientes con ellos.

## Licencia

MIT, ver LICENSE.

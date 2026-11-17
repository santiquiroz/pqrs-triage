# Contrato: web de la demo de triage de PQRS

Angular 21 ya creado en `web/` (standalone, vitest, SCSS, proxy `/api` → `http://localhost:5180` en `proxy.conf.json`). Los tipos y endpoints de la API están en `docs/contrato-api.md` (sección «Tipos» y «Endpoints»): cópialos como interfaces TypeScript en `web/src/app/api/tipos.ts` y crea un único servicio `web/src/app/api/triage-api.service.ts` con un método por endpoint (`HttpClient`, devuelve `Observable`). La API puede no estar corriendo mientras programas: no inventes datos de ejemplo dentro de los componentes.

Inspiración: la demo pública de Jev, «Watch Jev triage a support inbox» (https://thejevai.com/demo/support-triage): una bandeja simulada a la izquierda y a la derecha un panel de decisión con 4 pasos y tres juicios tipados. Aquí es lo mismo para la mesa de PQRS de SuruWorks, en español y con la marca de SuruWorks.

## Reglas generales

- Componentes standalone, `signal`/`computed`, `ChangeDetectionStrategy.OnPush`, control flow `@if`/`@for`, `inject()`. Sin NgModules, sin Angular Material, sin Tailwind, sin librerías nuevas (no agregar dependencias a `package.json`).
- Formularios con `ReactiveFormsModule` o señales; validación visible.
- Código atómico: componentes pequeños, funciones puras para formatear (porcentajes, fechas es-CO, colores por prioridad) en `web/src/app/util/`. Sin comentarios de documentación.
- Fechas con `Intl.DateTimeFormat('es-CO', { timeZone: 'America/Bogota', ... })`. Porcentajes con 0 decimales (`93 %`).
- Todo texto visible en español de Colombia. Nada en inglés salvo nombres propios (Jev, Tev1, TypeSafe).
- Accesibilidad: contraste AA, foco visible, `aria-live="polite"` en el panel de decisión, botones reales (`<button>`), etiquetas en todos los campos.
- Responsive: a menos de 1024 px el panel de decisión baja debajo de la bandeja; a 375 px no hay scroll horizontal.
- Estados de carga y error en cada llamada (mensaje de `error` que devuelve la API; si la API no responde: «No hay conexión con la API de la demo (¿corriste `dotnet run`?)»).

## Marca SuruWorks (obligatoria)

`web/src/styles.scss` define los tokens en `:root` y carga Sora (títulos) y DM Sans (texto) desde Google Fonts en `index.html`.

```
--navy: #081142;      /* logo, botones primarios, títulos */
--azul: #005787;      /* enlaces, acentos secundarios */
--cian: #22c3e6;      /* resaltados, barras de probabilidad, foco */
--fondo: #F4F6F9;  --superficie: #FFFFFF;  --borde: #DDE3EA;  --texto: #1B2333;  --texto-suave: #4A5A70;  --gris: #A3ADBB;
--teal: #2a9d8f;  --dorado: #d39b22;  --acero: #5b8fc7;  --terracota: #c8643b;  --verde: #7aa53a;
```

- Prioridades: Tutela = navy con borde cian y texto blanco; Urgente = terracota; Alta = dorado; Media = acero; Baja = verde. Destinos: tutela = navy, automática = teal, gestor = dorado.
- **Prohibido** rosado, morado, fucsia y degradados de colores. Fondo claro, tarjetas blancas con borde `--borde`, radio 12 px en tarjetas y 8 px en controles, sombras suaves solo en la tarjeta activa.
- Logo: `public/logo-suruworks.svg` en el encabezado (alto 28 px) con el texto «Mesa de PQRS · Demo de triage con IA». `public/logo-suruworks.svg` disponible para fondos navy.
- Los tres tipos de juicio llevan una insignia: `Choice` (cian), `Score` (dorado), `Noul` (teal), con el nombre en español al lado (Elección, Puntaje, Sí/No/Incierto).

## Estructura

Encabezado fijo con logo y navegación: **Triage en vivo** (`/triage`, ruta por defecto), **Radicar** (`/radicar`), **Evaluación** (`/evaluacion`), **Configuración** (`/configuracion`), **Cómo funciona** (`/como-funciona`). A la derecha del encabezado, el selector global de proveedor (lista de `GET /api/proveedores`; los no disponibles aparecen deshabilitados con su `motivoNoDisponible` en el `title`; por defecto el primero disponible en este orden: jev, tev1, llm, reglas). Guardar el proveedor elegido en un servicio de estado con señal, y en `localStorage` dentro de try/catch.

## Vista `/triage` (la principal, tipo demo de Jev)

1. **Encabezado de la vista**: título «Mira a la IA clasificar la bandeja de PQRS» y bajada «La IA juzga el caso. Las reglas de SuruWorks (áreas, responsables, ANS y correos) las aplica el código.». A la derecha, tarjeta de tarea: «Tarea: clasificar, priorizar y enrutar cada PQRS» con un interruptor **Manual / Automático · 3 s** (en automático, recorre la bandeja: clasifica el caso, espera 3 s, pasa al siguiente; se detiene al terminar o si el usuario pulsa una tarjeta).
2. **Columna izquierda — «Bandeja simulada»** (`GET /api/bandeja`): lista de tarjetas con canal (icono correo o portal), remitente y empresa, asunto (2 líneas), fecha relativa, y después de clasificar una píldora con la prioridad y el destino. La tarjeta activa se resalta. Debajo de la lista, botón **Escribir un caso** que abre un formulario (canal, nombre, empresa, correo, asunto, mensaje) y lo agrega a la bandeja local.
   - Debajo, el **detalle del caso activo**: asunto, remitente, correo, canal y buzón, el mensaje completo (respetando saltos de línea) y, si vienen, activo/local/tipología declarados por el cliente.
3. **Columna derecha — «Panel de decisión»**:
   - Fila superior: nombre y modelo del proveedor, insignia «API real de Jev» / «Modelo local» / «Sin IA (reglas)» según `tipo`, y latencia, llamadas y costo de la última corrida (`metricas`).
   - **Stepper de 4 pasos** que se anima al clasificar (cada paso se marca hecho en secuencia, ~300 ms entre pasos, solo visual): 1 Leer el caso · 2 Hacer 8 preguntas · 3 Revisar las respuestas · 4 Enrutar.
   - Botón primario **Clasificar con {proveedor}** → `POST /api/triage`.
   - Subtítulo «Una solicitud · ocho juicios» y una **tarjeta por pregunta** (en el orden de `preguntas`): insignia de tipo, instrucción corta, la elección o el valor (choice: clave elegida; score: nivel en texto + `puntaje` con 1 decimal sobre el máximo; noul: Sí / No y la probabilidad), confianza, y **barras de probabilidad**: choice muestra las 3 opciones más probables; score todos los niveles; noul una barra Sí/No. Etiqueta «confianza calibrada» o «confianza autodeclarada» según `confianzaCalibrada`.
   - **Datos extraídos** (de `extraidos`): chips de NIT, facturas, locales, radicados judiciales, juzgados, teléfonos. Nota pequeña: «Extraídos con reglas: el modelo de decisión no extrae texto.»
   - **Tarjeta de decisión** (`decision`): destino grande con su color (`destinoTexto`), tipo, área › grupo › motivo, responsable, prioridad con su color, ANS («2 días hábiles · vence 13 nov. 2026, 10:00 a. m.»), activo, etiquetas, aviso «Requiere revisión humana» si aplica. Lista **«Por qué»** con las `razones`: las disparadas con check y en negrita, las no disparadas en gris.
   - **Correos propuestos**: una tarjeta por correo (tipo, para, cc, asunto) con botón «Ver correo» que despliega el `cuerpoHtml` dentro de un `<iframe sandbox srcdoc>` (no usar `innerHTML`).
   - Acciones: **Aprobar y enrutar** → `POST /api/triage/enrutar`, muestra el radicado asignado y el aviso «Correos guardados en la bandeja de salida simulada (no se envió nada)»; **Corregir** → permite cambiar área, motivo y prioridad desde el catálogo (`GET /api/catalogo`) antes de enrutar; **Ver solicitud y respuesta** → panel plegable con `crudo` formateado (`<pre>`, JSON indentado).
   - **Comparar proveedores**: botón que corre el mismo caso con todos los proveedores disponibles en paralelo y muestra una tabla: proveedor, destino, motivo, prioridad, tutela (probabilidad), latencia, costo.
   - Regla visible al pie del panel (texto fijo, tomando los umbrales de `GET /api/catalogo`): «Tutela ≥ 50 % → Jurídica con ANS de 2 días hábiles. Si ninguna tipología encaja (< 50 %) o la confianza de la tipología es < 55 %, el caso va a la mesa (Por clasificar). La IA puede subir la prioridad del catálogo, nunca bajarla.»

## Vista `/radicar` — formulario público simplificado

Muestra la propuesta: el cliente escribe en lenguaje natural y la IA sugiere lo que hoy son 11 campos del formulario del mockup.

- Izquierda: formulario corto con 4 campos obligatorios: **Nombre**, **Correo**, **Empresa** (opcional) y **Cuéntanos qué pasó** (textarea). Botón **Radicar**.
- Mientras escribe (debounce 1,2 s, solo si el texto tiene ≥ 40 caracteres) llama a `POST /api/triage` con el proveedor elegido y muestra a la derecha «Esto entendimos»: tipo, tipología (`nombreCliente` del motivo), activo, prioridad y si parece tutela o derecho de petición, como chips que el usuario puede cambiar con un `<select>` (catálogo público: solo motivos `publico: true`) antes de radicar.
- Al radicar: `POST /api/triage/enrutar` y pantalla de confirmación con el radicado, el ANS y el texto del acuse que recibirá el cliente.
- Debajo, una comparación fija «Antes: 11 campos (tipo de solicitante, NIT, activo, local, área, tipología, asunto, descripción, …) · Ahora: 4 campos + sugerencia de la IA».

## Vista `/evaluacion`

- Selector de conjunto (`GET /api/evaluacion/conjuntos`: «Casos del mockup de diseño (56)», «Casos difíciles (22)», «Todos») y casillas de proveedores disponibles. Botón **Evaluar**.
- Corre `POST /api/evaluacion/caso` caso por caso (concurrencia 2 por proveedor) con barra de progreso por proveedor y botón **Detener**. Al terminar cada proveedor, `POST /api/evaluacion/resumen`.
- Tabla comparativa (una columna por proveedor): exactitud de tipo, área, tipología, activo, exactitud en los automáticos, tutela (recall y precisión, con vp/fn/fp), % automático / gestor / tutela, latencia p50 y p95, costo por cada 1.000 PQRS, errores. Resaltar el mejor valor de cada fila.
- Tabla de casos con filtro «solo fallos»: id, asunto, esperado vs obtenido por campo (verde acierto, terracota fallo, gris no aplica), confianza, latencia, error. Clic en una fila → abre ese caso en `/triage` (query param `caso=<id>` y `conjunto=`; `/triage` lo carga desde `GET /api/evaluacion/casos`).
- Botón **Descargar resultados (JSON)**: genera el archivo en el navegador (`Blob` + enlace de descarga).

## Vista `/configuracion`

- **Correos de las áreas**: tabla con área, correo (editable, validación de formato) y estado «validado», botón Guardar por fila → `PUT /api/catalogo/areas/{id}`. Correo de la mesa (solo lectura).
- **Umbrales**: sliders 0–100 % para confianzaTipologia, encajaCatalogo, tutela, derechoPeticion, riesgoLegal y uno 0–3 para urgenciaCritica, con el texto de qué hace cada uno; Guardar → `PUT /api/catalogo/umbrales`.
- **ANS por prioridad** (solo lectura): Urgente 5, Alta 6, Media 8, Baja 10 días hábiles; tutela 2; derecho de petición máximo 15. Próximos festivos (los 5 siguientes a hoy).
- **Catálogo**: árbol área › grupo › motivo con prioridad, responsable y suplente (nombres desde `usuarios`), solo lectura.
- **Bandeja de salida simulada** (`GET /api/correos`): lista de correos «enviados» con radicado, para, asunto, hora y vista previa en `<iframe sandbox srcdoc>`.

## Vista `/como-funciona`

Página estática, bien diagramada (sin imágenes externas), con:
1. Diagrama en SVG en línea del flujo: Caso → 8 preguntas tipadas → modelo de decisión (Jev / Tev1 / LLM / reglas) → probabilidades → reglas de SuruWorks en código (tutela, mesa, asignación, ANS, correos) → radicado y correos.
2. Las 8 preguntas con su tipo y para qué sirve cada una (tabla).
3. Los proveedores: qué es cada uno, dónde corre y qué datos salen de SuruWorks (Jev: API de TypeSafe en EE. UU., no entrena con datos de clientes, retención cero solo en plan empresarial; Tev1: pesos abiertos, corre en el equipo; LLM: Ollama local o Azure OpenAI; reglas: sin IA).
4. Por qué el código manda: la IA nunca asigna ANS ni correos; solo juzga.

## Pruebas (vitest, `npx ng test --watch=false`)

- `util/`: formatear porcentaje, color por prioridad, top-3 de probabilidades, formatear fecha es-CO.
- Servicio de estado del proveedor: elige el primer disponible en el orden jev, tev1, llm, reglas.
- Componente de tarjeta de juicio: muestra choice, score y noul con datos de ejemplo de prueba.

## Verificación

`cd web && npx ng build` sin errores ni advertencias de presupuesto (ajusta `budgets` en `angular.json` si hace falta: inicial 1 MB) y `npx ng test --watch=false` en verde.

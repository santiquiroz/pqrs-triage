export type TipoPregunta = 'choice' | 'score' | 'noul';
export type Destino = 'tutela' | 'automatica' | 'gestor';
export type Prioridad = 'Tutela' | 'Urgente' | 'Alta' | 'Media' | 'Baja';
export type TipoProveedor = 'decision' | 'llm' | 'reglas';
export type TipoCorreo = 'notificacion-area' | 'acuse-cliente' | 'alerta-juridica' | 'bandeja-gestor';

export interface Remitente {
  nombre: string;
  empresa?: string | null;
  correo: string;
  tipo?: string | null;
  nit?: string | null;
}

export interface CasoEntrada {
  id?: string | null;
  canal: 'Correo' | 'Portal';
  buzon?: string | null;
  remitente: Remitente;
  asunto: string;
  descripcion: string;
  activoDeclarado?: string | null;
  localDeclarado?: string | null;
  tipoDeclarado?: string | null;
  creado?: number | null;
}

export interface Esperado {
  tipo: string | null;
  area: string | null;
  grupo?: string | null;
  motivo: string | null;
  motivosAceptables?: string[] | null;
  esTutela: boolean;
  derechoPeticion?: boolean | null;
  riesgoLegal?: boolean | null;
  destino?: Destino | null;
  activo: string | null;
  prioridad?: string | null;
}

export interface CasoEvaluacion extends CasoEntrada {
  id: string;
  esperado: Esperado | null;
}

export interface Opcion {
  clave: string;
  descripcion: string;
}

export interface Pregunta {
  id: string;
  tipo: TipoPregunta;
  instrucciones: string;
  opciones?: Opcion[] | null;
  niveles?: string[] | null;
  criterios?: { verdadero: string; falso: string } | null;
}

export interface Respuesta {
  id: string;
  tipo: TipoPregunta;
  eleccion?: string | null;
  puntaje?: number | null;
  probabilidad?: number | null;
  confianza?: number | null;
  probabilidades: Record<string, number>;
  confianzaCalibrada: boolean;
}

export interface Extraidos {
  nits: string[];
  facturas: string[];
  locales: string[];
  correos: string[];
  telefonos: string[];
  radicadosJudiciales: string[];
  juzgados: string[];
}

export interface Razon {
  regla: string;
  detalle: string;
  disparada: boolean;
}

export interface CorreoPropuesto {
  tipo: TipoCorreo;
  para: string[];
  cc: string[];
  asunto: string;
  cuerpoHtml: string;
}

export interface CorreoEnviado extends CorreoPropuesto {
  id: string;
  radicado: string;
  enviadoEn: string;
}

export interface Decision {
  destino: Destino;
  destinoTexto: string;
  tipo: string | null;
  area: { id: string; nombre: string; correo: string } | null;
  grupo: string | null;
  motivo: string | null;
  responsable: { id: string; nombre: string; cargo: string | null } | null;
  prioridad: Prioridad;
  ansDias: number;
  venceEn: string;
  tipoDias: string;
  activo: string | null;
  etiquetas: string[];
  razones: Razon[];
  requiereRevisionHumana: boolean;
  notas: string[];
  correos: CorreoPropuesto[];
}

export interface ProveedorInfo {
  id: string;
  nombre: string;
  tipo: TipoProveedor;
  modelo: string;
  privacidad: string;
  costoUsdPorMillonTokens: number;
  descripcion: string;
}

export interface Proveedor extends ProveedorInfo {
  disponible: boolean;
  motivoNoDisponible: string | null;
}

export interface Metricas {
  latenciaMs: number;
  llamadas: number;
  tokensEntrada: number;
  costoUsd: number;
}

export interface ResultadoTriage {
  proveedor: ProveedorInfo;
  estado: Record<string, string>;
  preguntas: Pregunta[];
  respuestas: Record<string, Respuesta>;
  extraidos: Extraidos;
  decision: Decision;
  metricas: Metricas;
  crudo: { solicitudes: unknown[]; respuestas: unknown[] };
}

export interface ResultadoEnrutar {
  radicado: string;
  correos: CorreoEnviado[];
}

export interface Motivo {
  nombre: string;
  prioridad: string;
  nombreCliente: string;
  publico: boolean;
}

export interface Grupo {
  nombre: string;
  nombreCliente?: string | null;
  responsable: string | null;
  suplente: string | null;
  motivos: Motivo[];
}

export interface Area {
  id: string;
  nombre: string;
  correo: string;
  validado: boolean;
  grupos: Grupo[];
}

export interface Umbrales {
  confianzaTipologia: number;
  encajaCatalogo: number;
  tutela: number;
  derechoPeticion: number;
  riesgoLegal: number;
  urgenciaCritica: number;
}

export interface Usuario {
  id: string;
  nombre: string;
  rol: string;
  area?: string | null;
  cargo?: string | null;
}

export interface Catalogo {
  tipoDias: string;
  prioridades: Record<string, number>;
  tipos: { id: string; desc: string }[];
  areas: Area[];
  activos: { nombre: string; producto: string }[];
  usuarios: Usuario[];
  festivos: string[];
  correoMesa: string;
  ansTutelaDias: number;
  ansDerechoPeticionDias: number;
  umbrales: Umbrales;
}

export interface Obtenido {
  tipo: string | null;
  area: string | null;
  motivo: string | null;
  prioridad: string;
  esTutela: boolean;
  activo: string | null;
  destino: Destino;
  confianzaTipologia: number | null;
}

export interface Aciertos {
  tipo: boolean | null;
  area: boolean | null;
  motivo: boolean | null;
  tutela: boolean | null;
  activo: boolean | null;
  destino: boolean | null;
}

export interface FilaEvaluacion {
  casoId: string;
  conjunto: string;
  esperado: Esperado | null;
  obtenido: Obtenido | null;
  aciertos: Aciertos | null;
  latenciaMs: number;
  costoUsd: number;
  error: string | null;
}

export interface ResumenEvaluacion {
  proveedor: string;
  total: number;
  errores: number;
  exactitudTipo: number | null;
  exactitudArea: number | null;
  exactitudTipologia: number | null;
  exactitudActivo: number | null;
  exactitudEnAutomaticos: number | null;
  tutela: { vp: number; fn: number; fp: number; recall: number | null; precision: number | null };
  porcentajeAutomatico: number;
  porcentajeGestor: number;
  porcentajeTutela: number;
  latenciaP50Ms: number;
  latenciaP95Ms: number;
  costoUsdPorMil: number;
}

export interface ConjuntoInfo {
  id: 'mockup' | 'dificiles' | 'todos';
  nombre: string;
  total: number;
  etiquetados: number;
}

export interface FilaPorProveedor {
  proveedor: string;
  fila: FilaEvaluacion;
}

export interface ResultadoGuardado {
  archivo: string;
  generadoEn: string;
  resumenes: ResumenEvaluacion[];
  filas: FilaPorProveedor[];
}

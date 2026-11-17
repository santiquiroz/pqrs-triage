import { CorreoPropuesto, Prioridad, Respuesta, TipoCorreo, TipoPregunta, TipoProveedor } from '../api/tipos';

const ZONA = 'America/Bogota';
const fechaCorta = new Intl.DateTimeFormat('es-CO', { timeZone: ZONA, day: 'numeric', month: 'short', year: 'numeric', hour: 'numeric', minute: '2-digit' });
const soloFecha = new Intl.DateTimeFormat('es-CO', { timeZone: ZONA, day: 'numeric', month: 'short', year: 'numeric' });
const tiempoRelativo = new Intl.RelativeTimeFormat('es-CO', { numeric: 'auto' });

export interface Barra {
  clave: string;
  etiqueta: string;
  valor: number;
  texto?: string;
}

export function porcentaje(valor: number | null | undefined): string {
  if (valor === null || valor === undefined || Number.isNaN(valor)) return 'n/a';
  return `${Math.round(valor * 100)} %`;
}

export function fechaEsCo(iso: string | number): string {
  return fechaCorta.format(new Date(iso));
}

export function diaEsCo(iso: string | number): string {
  return soloFecha.format(new Date(iso));
}

export function haceCuanto(epochMs: number | null | undefined, ahora: number = Date.now()): string {
  if (!epochMs) return '';
  const dias = Math.round((epochMs - ahora) / 86_400_000);
  if (Math.abs(dias) >= 1) return tiempoRelativo.format(dias, 'day');
  const horas = Math.round((epochMs - ahora) / 3_600_000);
  if (horas !== 0) return tiempoRelativo.format(horas, 'hour');
  return tiempoRelativo.format(Math.min(-1, Math.round((epochMs - ahora) / 60_000)), 'minute');
}

export function clasePrioridad(prioridad: string | null | undefined): string {
  return `p-${(prioridad ?? 'media').toLowerCase()}`;
}

export function claseDestino(destino: string | null | undefined): string {
  return `d-${destino ?? 'gestor'}`;
}

export function principalesProbabilidades(probabilidades: Record<string, number>, cantidad = 3): Barra[] {
  return Object.entries(probabilidades)
    .sort(([, a], [, b]) => b - a)
    .slice(0, cantidad)
    .map(([clave, valor]) => ({ clave, etiqueta: clave, valor }));
}

export function barrasDeRespuesta(respuesta: Respuesta, niveles: string[] = []): Barra[] {
  if (respuesta.tipo === 'noul') return barrasNoul(respuesta.probabilidad ?? 0);
  if (respuesta.tipo === 'score') return barrasNiveles(respuesta.probabilidades, niveles);
  return principalesProbabilidades(respuesta.probabilidades);
}

export function confianzaDe(respuesta: Respuesta): number | null {
  if (respuesta.confianza !== null && respuesta.confianza !== undefined) return respuesta.confianza;
  if (respuesta.tipo === 'noul') return Math.max(respuesta.probabilidad ?? 0, 1 - (respuesta.probabilidad ?? 0));
  const valores = Object.values(respuesta.probabilidades);
  return valores.length ? Math.max(...valores) : null;
}

export function decimal(valor: number | null | undefined, digitos = 1): string {
  if (valor === null || valor === undefined) return 'n/a';
  return valor.toLocaleString('es-CO', { minimumFractionDigits: digitos, maximumFractionDigits: digitos });
}

export function milisegundos(valor: number): string {
  return valor >= 1000 ? `${decimal(valor / 1000)} s` : `${Math.round(valor)} ms`;
}

export function valorPrincipal(respuesta: Respuesta): string {
  if (respuesta.tipo === 'noul') return (respuesta.probabilidad ?? 0) >= 0.5 ? 'Sí' : 'No';
  return respuesta.eleccion ?? 'Sin respuesta';
}

export function nombreTipo(tipo: TipoPregunta): string {
  const nombres: Record<TipoPregunta, string> = { choice: 'Elección', score: 'Puntaje', noul: 'Sí / No' };
  return nombres[tipo];
}

export function nombreCorreo(tipo: TipoCorreo): string {
  const nombres: Record<TipoCorreo, string> = {
    'notificacion-area': 'Notificación al área',
    'acuse-cliente': 'Acuse al cliente',
    'alerta-juridica': 'Alerta a Jurídica',
    'bandeja-gestor': 'Mesa de PQRS',
  };
  return nombres[tipo];
}

export function destinatarios(correo: CorreoPropuesto): string {
  const cc = correo.cc.length ? ` · cc ${correo.cc.join(', ')}` : '';
  return `${correo.para.join(', ')}${cc}`;
}

export function rangoPrioridad(prioridad: string): number {
  const orden: Prioridad[] = ['Baja', 'Media', 'Alta', 'Urgente', 'Tutela'];
  return orden.indexOf(prioridad as Prioridad);
}

export function plazo(dias: number, tipoDias: string): string {
  const tipo = tipoDias.toLowerCase().startsWith('h') ? 'hábiles' : tipoDias;
  return `${dias} ${dias === 1 ? 'día' : 'días'} ${tipo}`;
}

export function origenConfianza(tipo: TipoProveedor, calibrada: boolean): string {
  if (calibrada) return 'calibrada';
  if (tipo === 'llm') return 'autodeclarada';
  if (tipo === 'reglas') return 'heurística de reglas';
  return 'de logprobs, sin calibrar';
}

export function usd(valor: number): string {
  return `US$ ${valor.toFixed(valor > 0 && valor < 0.01 ? 4 : 2)}`;
}

function barrasNoul(probabilidad: number): Barra[] {
  return [{ clave: 'si', etiqueta: 'Probabilidad de «sí»', valor: probabilidad }];
}

function barrasNiveles(probabilidades: Record<string, number>, niveles: string[]): Barra[] {
  return niveles.map((etiqueta, i) => ({ clave: String(i), etiqueta, valor: probabilidades[String(i)] ?? 0 }));
}

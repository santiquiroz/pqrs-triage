import { Respuesta, Umbrales } from '../api/tipos';
import { decimal, porcentaje } from './formato';

export const MAXIMO_URGENCIA = 3;

export interface Corte {
  posicion: number;
  etiqueta: string;
  efecto: string;
  cruzado: boolean;
}

interface Definicion {
  umbral: (u: Umbrales) => number;
  valor: (r: Respuesta) => number;
  siCruza: string;
  siNo: string;
}

const probabilidadSi = (r: Respuesta): number => r.probabilidad ?? 0;

const confianzaPrincipal = (r: Respuesta): number => r.confianza ?? Math.max(0, ...Object.values(r.probabilidades));

const DEFINICIONES: Record<string, Definicion> = {
  tipologia: {
    umbral: (u) => u.confianzaTipologia,
    valor: confianzaPrincipal,
    siCruza: 'asigna directo al responsable',
    siNo: 'va a la mesa con la sugerencia',
  },
  encaja_catalogo: {
    umbral: (u) => u.encajaCatalogo,
    valor: probabilidadSi,
    siCruza: 'sigue con el catálogo',
    siNo: 'va a la mesa (Por clasificar)',
  },
  urgencia: {
    umbral: (u) => u.urgenciaCritica / MAXIMO_URGENCIA,
    valor: (r) => (r.puntaje ?? 0) / MAXIMO_URGENCIA,
    siCruza: 'copia a la mesa por urgencia crítica',
    siNo: 'sin escalar',
  },
  tutela: {
    umbral: (u) => u.tutela,
    valor: probabilidadSi,
    siCruza: 'Jurídica con ANS de 2 días hábiles',
    siNo: 'flujo normal',
  },
  derecho_peticion: {
    umbral: (u) => u.derechoPeticion,
    valor: probabilidadSi,
    siCruza: 'ANS máximo de 15 días hábiles',
    siNo: 'ANS del catálogo',
  },
  riesgo_legal: {
    umbral: (u) => u.riesgoLegal,
    valor: probabilidadSi,
    siCruza: 'copia a Jurídica',
    siNo: 'sin copia a Jurídica',
  },
};

export function corteDe(preguntaId: string, respuesta: Respuesta, umbrales: Umbrales | null): Corte | null {
  const definicion = DEFINICIONES[preguntaId];
  if (!definicion || !umbrales) return null;
  const posicion = definicion.umbral(umbrales);
  const cruzado = definicion.valor(respuesta) >= posicion;
  return { posicion, etiqueta: etiquetaCorte(preguntaId, posicion), efecto: cruzado ? definicion.siCruza : definicion.siNo, cruzado };
}

function etiquetaCorte(preguntaId: string, posicion: number): string {
  if (preguntaId === 'urgencia') return `${decimal(posicion * MAXIMO_URGENCIA)} de ${MAXIMO_URGENCIA}`;
  return porcentaje(posicion);
}

import { FilaEvaluacion, FilaPorProveedor, ResultadoGuardado, ResumenEvaluacion } from '../api/tipos';
import { milisegundos, porcentaje, usd } from './formato';

export type Sentido = 'mayor' | 'menor' | 'ninguno';

export interface Metrica {
  nombre: string;
  sentido: Sentido;
  valor: (r: ResumenEvaluacion) => number | null;
  texto: (r: ResumenEvaluacion) => string;
}

const fraccion = (nombre: string, valor: (r: ResumenEvaluacion) => number | null): Metrica => ({
  nombre,
  sentido: 'mayor',
  valor,
  texto: (r) => porcentaje(valor(r)),
});

export const METRICAS: Metrica[] = [
  fraccion('Exactitud de tipo', (r) => r.exactitudTipo),
  fraccion('Exactitud de área', (r) => r.exactitudArea),
  fraccion('Exactitud de tipología', (r) => r.exactitudTipologia),
  fraccion('Exactitud de activo', (r) => r.exactitudActivo),
  fraccion('Exactitud en los automáticos', (r) => r.exactitudEnAutomaticos),
  {
    nombre: 'Tutela: recall',
    sentido: 'mayor',
    valor: (r) => r.tutela.recall,
    texto: (r) => `${porcentaje(r.tutela.recall)} (vp ${r.tutela.vp} · fn ${r.tutela.fn})`,
  },
  {
    nombre: 'Tutela: precisión',
    sentido: 'mayor',
    valor: (r) => r.tutela.precision,
    texto: (r) => `${porcentaje(r.tutela.precision)} (fp ${r.tutela.fp})`,
  },
  {
    nombre: 'Automático / mesa / tutela',
    sentido: 'ninguno',
    valor: () => null,
    texto: (r) => `${porcentaje(r.porcentajeAutomatico)} / ${porcentaje(r.porcentajeGestor)} / ${porcentaje(r.porcentajeTutela)}`,
  },
  { nombre: 'Latencia p50', sentido: 'menor', valor: (r) => r.latenciaP50Ms, texto: (r) => milisegundos(r.latenciaP50Ms) },
  { nombre: 'Latencia p95', sentido: 'menor', valor: (r) => r.latenciaP95Ms, texto: (r) => milisegundos(r.latenciaP95Ms) },
  { nombre: 'Costo por 1.000 PQRS', sentido: 'menor', valor: (r) => r.costoUsdPorMil, texto: (r) => usd(r.costoUsdPorMil) },
  { nombre: 'Errores', sentido: 'menor', valor: (r) => r.errores, texto: (r) => `${r.errores} de ${r.total}` },
];

export function mejorValor(metrica: Metrica, resumenes: ResumenEvaluacion[]): number | null {
  if (metrica.sentido === 'ninguno' || resumenes.length < 2) return null;
  const valores = resumenes.map(metrica.valor).filter((v): v is number => v !== null);
  if (!valores.length) return null;
  return metrica.sentido === 'mayor' ? Math.max(...valores) : Math.min(...valores);
}

export function esFallo(fila: FilaEvaluacion): boolean {
  if (fila.error) return true;
  const a = fila.aciertos;
  if (!a) return false;
  return [a.tipo, a.area, a.motivo, a.tutela, a.activo].some((v) => v === false);
}

export const CLAVE_COMBINADO = 'combinado';

export function combinarGuardados(guardados: ResultadoGuardado[]): ResultadoGuardado | null {
  if (guardados.length < 2) return null;
  const vistos = new Set<string>();
  const resumenes: ResumenEvaluacion[] = [];
  const filas: FilaPorProveedor[] = [];
  for (const g of guardados) {
    for (const r of g.resumenes.filter((x) => !vistos.has(x.proveedor))) {
      vistos.add(r.proveedor);
      resumenes.push(r);
      filas.push(...g.filas.filter((f) => f.proveedor === r.proveedor));
    }
  }
  return { archivo: 'Última corrida de cada proveedor', generadoEn: guardados[0].generadoEn, resumenes, filas };
}

export function descargarJson(nombre: string, datos: unknown): void {
  const blob = new Blob([JSON.stringify(datos, null, 2)], { type: 'application/json' });
  const url = URL.createObjectURL(blob);
  const enlace = document.createElement('a');
  enlace.href = url;
  enlace.download = nombre;
  enlace.click();
  URL.revokeObjectURL(url);
}

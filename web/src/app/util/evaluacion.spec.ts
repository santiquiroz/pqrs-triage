import { FilaEvaluacion, ResultadoGuardado, ResumenEvaluacion } from '../api/tipos';
import { METRICAS, combinarGuardados, esFallo, mejorValor } from './evaluacion';

function resumen(proveedor: string, exactitudTipologia: number, latenciaP50Ms: number): ResumenEvaluacion {
  return {
    proveedor, total: 10, errores: 0, exactitudTipo: 0.8, exactitudArea: 0.8, exactitudTipologia, exactitudActivo: 1, exactitudEnAutomaticos: 0.9,
    tutela: { vp: 1, fn: 0, fp: 0, recall: 1, precision: 1 }, porcentajeAutomatico: 0.7, porcentajeGestor: 0.2, porcentajeTutela: 0.1,
    latenciaP50Ms, latenciaP95Ms: latenciaP50Ms * 2, costoUsdPorMil: 0,
  };
}

describe('evaluacion', () => {
  it('combina la última corrida de cada proveedor sin repetirlo', () => {
    const nuevo: ResultadoGuardado = { archivo: 'tev1.json', generadoEn: '2026-11-11', resumenes: [resumen('tev1', 0.99, 20000)], filas: [] };
    const viejo: ResultadoGuardado = { archivo: 'tev1-dificiles.json', generadoEn: '2026-11-10', resumenes: [resumen('tev1', 0.95, 23000)], filas: [] };
    const reglas: ResultadoGuardado = { archivo: 'reglas.json', generadoEn: '2026-11-09', resumenes: [resumen('reglas', 0.77, 1)], filas: [] };
    const combinado = combinarGuardados([nuevo, viejo, reglas]);
    expect(combinado?.resumenes.map((r) => `${r.proveedor}:${r.exactitudTipologia}`)).toEqual(['tev1:0.99', 'reglas:0.77']);
  });

  it('elige el máximo en exactitudes y el mínimo en latencia', () => {
    const resumenes = [resumen('reglas', 0.77, 1), resumen('tev1', 0.95, 23000)];
    const tipologia = METRICAS.find((m) => m.nombre === 'Exactitud de tipología')!;
    const latencia = METRICAS.find((m) => m.nombre === 'Latencia p50')!;
    expect(mejorValor(tipologia, resumenes)).toBe(0.95);
    expect(mejorValor(latencia, resumenes)).toBe(1);
  });

  it('cuenta como fallo un campo errado o un error, no un campo sin etiqueta', () => {
    const base: FilaEvaluacion = { casoId: 'X', conjunto: 'mockup', esperado: null, obtenido: null, aciertos: null, latenciaMs: 1, costoUsd: 0, error: null };
    const sinEtiqueta = { ...base, aciertos: { tipo: null, area: true, motivo: true, tutela: true, activo: null, destino: null } };
    expect(esFallo(sinEtiqueta)).toBe(false);
    expect(esFallo({ ...sinEtiqueta, aciertos: { ...sinEtiqueta.aciertos, motivo: false } })).toBe(true);
    expect(esFallo({ ...base, error: 'timeout' })).toBe(true);
  });
});

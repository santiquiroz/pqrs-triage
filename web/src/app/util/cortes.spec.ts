import { Respuesta, Umbrales } from '../api/tipos';
import { corteDe } from './cortes';

const umbrales: Umbrales = { confianzaTipologia: 0.55, encajaCatalogo: 0.5, tutela: 0.5, derechoPeticion: 0.5, riesgoLegal: 0.5, urgenciaCritica: 2.5 };

function noul(probabilidad: number): Respuesta {
  return { id: 'tutela', tipo: 'noul', probabilidad, probabilidades: {}, confianzaCalibrada: true };
}

describe('corteDe', () => {
  it('marca la tutela como cruzada desde el umbral', () => {
    expect(corteDe('tutela', noul(0.5), umbrales)?.cruzado).toBe(true);
    expect(corteDe('tutela', noul(0.49), umbrales)?.efecto).toBe('flujo normal');
  });

  it('lleva la urgencia a la escala de 0 a 3', () => {
    const urgencia: Respuesta = { id: 'urgencia', tipo: 'score', puntaje: 2.7, probabilidades: {}, confianzaCalibrada: true };
    const corte = corteDe('urgencia', urgencia, umbrales);
    expect(corte?.posicion).toBeCloseTo(2.5 / 3);
    expect(corte?.cruzado).toBe(true);
    expect(corte?.etiqueta).toBe('2,5 de 3');
  });

  it('no dibuja corte en preguntas sin regla', () => {
    expect(corteDe('tipo', noul(0.9), umbrales)).toBeNull();
    expect(corteDe('tutela', noul(0.9), null)).toBeNull();
  });
});

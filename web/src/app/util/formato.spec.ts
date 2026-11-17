import { Respuesta } from '../api/tipos';
import { barrasDeRespuesta, clasePrioridad, confianzaDe, plazo, fechaEsCo, porcentaje, principalesProbabilidades, valorPrincipal } from './formato';

describe('formato', () => {
  it('formatea porcentajes sin decimales y n/a para nulos', () => {
    expect(porcentaje(0.934)).toBe('93 %');
    expect(porcentaje(null)).toBe('n/a');
  });

  it('escribe el plazo en días hábiles', () => {
    expect(plazo(2, 'habiles')).toBe('2 días hábiles');
    expect(plazo(1, 'habiles')).toBe('1 día hábiles');
  });

  it('asigna la clase de color por prioridad', () => {
    expect(clasePrioridad('Urgente')).toBe('p-urgente');
    expect(clasePrioridad(null)).toBe('p-media');
  });

  it('devuelve las tres opciones más probables en orden', () => {
    const barras = principalesProbabilidades({ a: 0.1, b: 0.6, c: 0.2, d: 0.1 });
    expect(barras.map((b) => b.clave)).toEqual(['b', 'c', 'a']);
  });

  it('formatea fechas en hora de Colombia', () => {
    expect(fechaEsCo('2026-11-17T10:00:00-05:00')).toContain('17');
    expect(fechaEsCo('2026-11-17T10:00:00-05:00')).toContain('2026');
  });

  it('convierte un noul en una barra con la probabilidad de sí', () => {
    const respuesta: Respuesta = { id: 'tutela', tipo: 'noul', probabilidad: 0.8, probabilidades: {}, confianzaCalibrada: true };
    expect(barrasDeRespuesta(respuesta).map((b) => b.valor)).toEqual([0.8]);
    expect(valorPrincipal(respuesta)).toBe('Sí');
    expect(confianzaDe(respuesta)).toBe(0.8);
  });
});

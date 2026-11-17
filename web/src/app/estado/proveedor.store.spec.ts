import { Proveedor } from '../api/tipos';
import { elegirPorDefecto } from './proveedor.store';

function proveedor(id: string, disponible: boolean): Proveedor {
  return { id, nombre: id, tipo: 'decision', modelo: id, privacidad: '', costoUsdPorMillonTokens: 0, descripcion: '', disponible, motivoNoDisponible: null };
}

describe('elegirPorDefecto', () => {
  it('elige el primer disponible en el orden jev, tev1, llm, reglas', () => {
    const lista = [proveedor('reglas', true), proveedor('llm', true), proveedor('tev1', true), proveedor('jev', false)];
    expect(elegirPorDefecto(lista, null)).toBe('tev1');
  });

  it('respeta el guardado si sigue disponible', () => {
    const lista = [proveedor('reglas', true), proveedor('tev1', true)];
    expect(elegirPorDefecto(lista, 'reglas')).toBe('reglas');
  });

  it('ignora el guardado si ya no está disponible', () => {
    const lista = [proveedor('reglas', true), proveedor('tev1', false)];
    expect(elegirPorDefecto(lista, 'tev1')).toBe('reglas');
  });
});

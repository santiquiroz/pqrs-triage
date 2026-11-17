import { TestBed } from '@angular/core/testing';
import { Pregunta, Respuesta, TipoProveedor } from '../api/tipos';
import { TarjetaJuicio } from './tarjeta-juicio';

function montar(pregunta: Pregunta, respuesta: Respuesta, tipoProveedor: TipoProveedor = 'decision'): HTMLElement {
  const fixture = TestBed.createComponent(TarjetaJuicio);
  fixture.componentRef.setInput('pregunta', pregunta);
  fixture.componentRef.setInput('respuesta', respuesta);
  fixture.componentRef.setInput('tipoProveedor', tipoProveedor);
  fixture.detectChanges();
  return fixture.nativeElement as HTMLElement;
}

describe('TarjetaJuicio', () => {
  it('muestra la elección y las tres opciones más probables de un choice', () => {
    const el = montar(
      { id: 'tipo', tipo: 'choice', instrucciones: 'Tipo de PQRS' },
      { id: 'tipo', tipo: 'choice', eleccion: 'Queja', probabilidades: { Queja: 0.7, Reclamo: 0.2, Petición: 0.06, Sugerencia: 0.04 }, confianzaCalibrada: true },
    );
    expect(el.querySelector('.eleccion')?.textContent).toContain('Queja');
    expect(el.querySelectorAll('.fila').length).toBe(3);
    expect(el.textContent).toContain('calibrada');
  });

  it('muestra el nivel y el puntaje de un score', () => {
    const el = montar(
      { id: 'urgencia', tipo: 'score', instrucciones: 'Urgencia', niveles: ['Baja', 'Media', 'Alta', 'Crítica'] },
      { id: 'urgencia', tipo: 'score', eleccion: 'Alta', puntaje: 2.1, probabilidades: { '0': 0.05, '1': 0.15, '2': 0.45, '3': 0.35 }, confianzaCalibrada: true },
    );
    expect(el.querySelector('.puntaje')?.textContent).toContain('2,1 de 3');
    expect(el.textContent).toContain('Crítica');
  });

  it('muestra Sí o No y la probabilidad de un noul', () => {
    const el = montar(
      { id: 'tutela', tipo: 'noul', instrucciones: '¿Es tutela?' },
      { id: 'tutela', tipo: 'noul', eleccion: 'no', probabilidad: 0.1, probabilidades: { si: 0.1, no: 0.9 }, confianzaCalibrada: false },
      'llm',
    );
    expect(el.querySelector('.eleccion')?.textContent).toContain('No');
    expect(el.textContent).toContain('10 %');
    expect(el.textContent).toContain('autodeclarada');
  });
});

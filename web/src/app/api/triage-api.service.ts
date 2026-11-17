import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  Area,
  CasoEntrada,
  CasoEvaluacion,
  Catalogo,
  ConjuntoInfo,
  CorreoEnviado,
  Decision,
  FilaEvaluacion,
  Proveedor,
  ResultadoEnrutar,
  ResultadoGuardado,
  Respuesta,
  ResultadoTriage,
  ResumenEvaluacion,
  Umbrales,
} from './tipos';

const SIN_CONEXION = 'No hay conexión con la API de la demo (¿corriste dotnet run?).';

export function mensajeDeError(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) return 'Ocurrió un error inesperado.';
  if (error.status === 0 || error.status === 504) return SIN_CONEXION;
  const cuerpo = error.error as { error?: string } | null;
  return cuerpo?.error ?? `La API respondió ${error.status}.`;
}

@Injectable({ providedIn: 'root' })
export class TriageApiService {
  private readonly http = inject(HttpClient);

  proveedores(): Observable<Proveedor[]> {
    return this.http.get<Proveedor[]>('/api/proveedores');
  }

  catalogo(): Observable<Catalogo> {
    return this.http.get<Catalogo>('/api/catalogo');
  }

  actualizarCorreoArea(id: string, correo: string): Observable<Area> {
    return this.http.put<Area>(`/api/catalogo/areas/${encodeURIComponent(id)}`, { correo });
  }

  actualizarUmbrales(umbrales: Umbrales): Observable<Umbrales> {
    return this.http.put<Umbrales>('/api/catalogo/umbrales', umbrales);
  }

  bandeja(): Observable<CasoEntrada[]> {
    return this.http.get<CasoEntrada[]>('/api/bandeja');
  }

  clasificar(proveedor: string, caso: CasoEntrada): Observable<ResultadoTriage> {
    return this.http.post<ResultadoTriage>('/api/triage', { proveedor, caso });
  }

  enrutar(caso: CasoEntrada, decision: Decision): Observable<ResultadoEnrutar> {
    return this.http.post<ResultadoEnrutar>('/api/triage/enrutar', { caso, decision });
  }

  decidir(caso: CasoEntrada, respuestas: Record<string, Respuesta>): Observable<Decision> {
    return this.http.post<Decision>('/api/triage/decidir', { caso, respuestas });
  }

  corregir(caso: CasoEntrada, respuestas: Record<string, Respuesta>, motivo: string): Observable<Decision> {
    return this.http.post<Decision>('/api/triage/corregir', { caso, respuestas, motivo });
  }

  correos(): Observable<CorreoEnviado[]> {
    return this.http.get<CorreoEnviado[]>('/api/correos');
  }

  conjuntos(): Observable<ConjuntoInfo[]> {
    return this.http.get<ConjuntoInfo[]>('/api/evaluacion/conjuntos');
  }

  casos(conjunto: string): Observable<CasoEvaluacion[]> {
    return this.http.get<CasoEvaluacion[]>('/api/evaluacion/casos', { params: { conjunto } });
  }

  evaluarCaso(proveedor: string, conjunto: string, casoId: string): Observable<FilaEvaluacion> {
    return this.http.post<FilaEvaluacion>('/api/evaluacion/caso', { proveedor, conjunto, casoId });
  }

  guardados(): Observable<ResultadoGuardado[]> {
    return this.http.get<ResultadoGuardado[]>('/api/evaluacion/guardados');
  }

  resumir(proveedor: string, filas: FilaEvaluacion[]): Observable<ResumenEvaluacion> {
    return this.http.post<ResumenEvaluacion>('/api/evaluacion/resumen', { proveedor, filas });
  }
}

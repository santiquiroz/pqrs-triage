import { Injectable, computed, inject, signal } from '@angular/core';
import { TriageApiService, mensajeDeError } from '../api/triage-api.service';
import { Proveedor } from '../api/tipos';

const CLAVE_GUARDADA = 'pqrs-demo-proveedor';
export const ORDEN_PREFERIDO = ['jev', 'tev1', 'gpt4o', 'gemma', 'llm', 'reglas'];

export function elegirPorDefecto(proveedores: Proveedor[], guardado: string | null): string | null {
  const disponibles = proveedores.filter((p) => p.disponible);
  if (guardado && disponibles.some((p) => p.id === guardado)) return guardado;
  const preferido = ORDEN_PREFERIDO.find((id) => disponibles.some((p) => p.id === id));
  return preferido ?? disponibles[0]?.id ?? null;
}

function leerGuardado(): string | null {
  try {
    return localStorage.getItem(CLAVE_GUARDADA);
  } catch {
    return null;
  }
}

function guardar(id: string): void {
  try {
    localStorage.setItem(CLAVE_GUARDADA, id);
  } catch {
    return;
  }
}

@Injectable({ providedIn: 'root' })
export class ProveedorStore {
  private readonly api = inject(TriageApiService);

  readonly proveedores = signal<Proveedor[]>([]);
  readonly seleccionado = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly actual = computed(() => this.proveedores().find((p) => p.id === this.seleccionado()) ?? null);
  readonly disponibles = computed(() => this.proveedores().filter((p) => p.disponible));

  cargar(): void {
    this.api.proveedores().subscribe({
      next: (lista) => this.alCargar(lista),
      error: (e) => this.error.set(mensajeDeError(e)),
    });
  }

  elegir(id: string): void {
    this.seleccionado.set(id);
    guardar(id);
  }

  private alCargar(lista: Proveedor[]): void {
    this.error.set(null);
    this.proveedores.set(lista);
    this.seleccionado.set(elegirPorDefecto(lista, leerGuardado()));
  }
}

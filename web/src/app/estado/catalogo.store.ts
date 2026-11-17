import { Injectable, computed, inject, signal } from '@angular/core';
import { TriageApiService, mensajeDeError } from '../api/triage-api.service';
import { Catalogo, Umbrales } from '../api/tipos';

@Injectable({ providedIn: 'root' })
export class CatalogoStore {
  private readonly api = inject(TriageApiService);

  readonly catalogo = signal<Catalogo | null>(null);
  readonly error = signal<string | null>(null);
  readonly umbrales = computed(() => this.catalogo()?.umbrales ?? null);

  cargar(forzar = false): void {
    if (this.catalogo() && !forzar) return;
    this.api.catalogo().subscribe({
      next: (c) => {
        this.error.set(null);
        this.catalogo.set(c);
      },
      error: (e) => this.error.set(mensajeDeError(e)),
    });
  }

  conUmbrales(umbrales: Umbrales): void {
    const actual = this.catalogo();
    if (actual) this.catalogo.set({ ...actual, umbrales });
  }
}

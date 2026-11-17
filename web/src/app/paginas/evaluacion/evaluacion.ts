import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, Subject, catchError, from, mergeMap, of, takeUntil } from 'rxjs';
import { TriageApiService, mensajeDeError } from '../../api/triage-api.service';
import { CasoEvaluacion, ConjuntoInfo, FilaEvaluacion, FilaPorProveedor, ResultadoGuardado, ResumenEvaluacion } from '../../api/tipos';
import { ProveedorStore } from '../../estado/proveedor.store';
import { CLAVE_COMBINADO, METRICAS, Metrica, combinarGuardados, descargarJson, esFallo, mejorValor } from '../../util/evaluacion';
import { fechaEsCo, milisegundos, porcentaje } from '../../util/formato';

const CONCURRENCIA = 2;

interface Progreso {
  hechos: number;
  total: number;
}

type Campo = 'tipo' | 'area' | 'motivo' | 'tutela' | 'activo';

@Component({
  selector: 'app-evaluacion',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './evaluacion.html',
  styleUrl: './evaluacion.scss',
})
export class EvaluacionPage implements OnInit {
  private readonly api = inject(TriageApiService);
  private readonly router = inject(Router);
  protected readonly proveedores = inject(ProveedorStore);
  private readonly detener$ = new Subject<void>();

  protected readonly conjuntos = signal<ConjuntoInfo[]>([]);
  protected readonly conjunto = signal('dificiles');
  protected readonly elegidos = signal<string[]>(['reglas']);
  protected readonly guardados = signal<ResultadoGuardado[]>([]);
  protected readonly origen = signal<string | null>(null);
  protected readonly asuntos = signal<Record<string, string>>({});
  protected readonly progreso = signal<Record<string, Progreso>>({});
  protected readonly filas = signal<FilaPorProveedor[]>([]);
  protected readonly resumenes = signal<ResumenEvaluacion[]>([]);
  protected readonly corriendo = signal(false);
  protected readonly soloFallos = signal(false);
  protected readonly vistaProveedor = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);

  protected readonly metricas = METRICAS;
  protected readonly claveCombinado = CLAVE_COMBINADO;
  protected readonly combinado = computed(() => combinarGuardados(this.guardados()));
  protected readonly campos: { id: Campo; nombre: string }[] = [
    { id: 'tipo', nombre: 'Tipo' },
    { id: 'area', nombre: 'Área' },
    { id: 'motivo', nombre: 'Tipología' },
    { id: 'tutela', nombre: 'Tutela' },
    { id: 'activo', nombre: 'Activo' },
  ];

  protected readonly proveedoresConFilas = computed(() => [...new Set(this.filas().map((f) => f.proveedor))]);
  protected readonly filasVisibles = computed(() => {
    const proveedor = this.vistaProveedor() ?? this.proveedoresConFilas()[0];
    return this.filas()
      .filter((f) => f.proveedor === proveedor)
      .filter((f) => !this.soloFallos() || esFallo(f.fila));
  });

  protected readonly ms = milisegundos;
  protected readonly pct = porcentaje;
  protected readonly fecha = fechaEsCo;

  constructor() {
    inject(DestroyRef).onDestroy(() => this.detener());
  }

  ngOnInit(): void {
    this.api.conjuntos().subscribe({ next: (c) => this.conjuntos.set(c), error: (e) => this.error.set(mensajeDeError(e)) });
    this.api.casos('todos').subscribe({ next: (c) => this.asuntos.set(Object.fromEntries(c.map((x) => [x.id, x.asunto]))) });
    this.api.guardados().subscribe({
      next: (g) => {
        this.guardados.set(g);
        const inicial = this.combinado() ?? g[0];
        if (inicial && !this.filas().length) this.cargarGuardado(inicial);
      },
    });
  }

  protected alternar(id: string, evento: Event): void {
    const marcado = (evento.target as HTMLInputElement).checked;
    this.elegidos.update((lista) => (marcado ? [...lista, id] : lista.filter((x) => x !== id)));
  }

  protected cargarGuardado(g: ResultadoGuardado): void {
    this.filas.set(g.filas);
    this.resumenes.set(g.resumenes);
    this.progreso.set({});
    this.vistaProveedor.set(null);
    this.origen.set(`${g.archivo} · ${fechaEsCo(g.generadoEn)}`);
  }

  protected elegirGuardado(evento: Event): void {
    const valor = (evento.target as HTMLSelectElement).value;
    const g = valor === CLAVE_COMBINADO ? this.combinado() : this.guardados().find((x) => x.archivo === valor);
    if (g) this.cargarGuardado(g);
  }

  protected evaluar(): void {
    const proveedores = this.elegidos();
    if (!proveedores.length || this.corriendo()) return;
    this.reiniciar();
    this.api.casos(this.conjunto()).subscribe({
      next: (casos) => this.correr(proveedores, casos.filter((c) => c.esperado)),
      error: (e) => this.error.set(mensajeDeError(e)),
    });
  }

  protected detener(): void {
    this.detener$.next();
    this.corriendo.set(false);
  }

  protected abrir(casoId: string, conjunto: string): void {
    this.router.navigate(['/triage'], { queryParams: { caso: casoId, conjunto } });
  }

  protected descargar(): void {
    descargarJson(`evaluacion-${this.conjunto()}.json`, { resumenes: this.resumenes(), filas: this.filas() });
  }

  protected mejor(metrica: Metrica, r: ResumenEvaluacion): boolean {
    const mejor = mejorValor(metrica, this.resumenes());
    return mejor !== null && metrica.valor(r) === mejor;
  }

  protected acierto(fila: FilaEvaluacion, campo: Campo): string {
    const valor = fila.aciertos?.[campo];
    if (valor === null || valor === undefined) return 'na';
    return valor ? 'si' : 'no';
  }

  protected obtenido(fila: FilaEvaluacion, campo: Campo): string {
    const o = fila.obtenido;
    if (!o) return '—';
    if (campo === 'tutela') return o.esTutela ? 'Sí' : 'No';
    return o[campo] ?? '—';
  }

  protected esperadoDe(fila: FilaEvaluacion, campo: Campo): string {
    const e = fila.esperado;
    if (!e) return '';
    if (campo === 'tutela') return e.esTutela ? 'Sí' : 'No';
    return e[campo] ?? '';
  }

  protected progresoDe(id: string): Progreso | null {
    return this.progreso()[id] ?? null;
  }

  private reiniciar(): void {
    this.error.set(null);
    this.filas.set([]);
    this.resumenes.set([]);
    this.origen.set(null);
    this.vistaProveedor.set(null);
    this.corriendo.set(true);
  }

  private correr(proveedores: string[], casos: CasoEvaluacion[]): void {
    let pendientes = proveedores.length;
    this.progreso.set(Object.fromEntries(proveedores.map((p) => [p, { hechos: 0, total: casos.length }])));
    for (const proveedor of proveedores) {
      const filas: FilaEvaluacion[] = [];
      from(casos)
        .pipe(
          mergeMap((c) => this.evaluarCaso(proveedor, c), CONCURRENCIA),
          takeUntil(this.detener$),
        )
        .subscribe({
          next: (fila) => this.alEvaluar(proveedor, fila, filas),
          complete: () => {
            this.resumir(proveedor, filas);
            pendientes -= 1;
            if (pendientes === 0) this.corriendo.set(false);
          },
        });
    }
  }

  private evaluarCaso(proveedor: string, caso: CasoEvaluacion): Observable<FilaEvaluacion> {
    return this.api.evaluarCaso(proveedor, this.conjunto(), caso.id).pipe(
      catchError((e) =>
        of<FilaEvaluacion>({ casoId: caso.id, conjunto: this.conjunto(), esperado: caso.esperado, obtenido: null, aciertos: null, latenciaMs: 0, costoUsd: 0, error: mensajeDeError(e) }),
      ),
    );
  }

  private alEvaluar(proveedor: string, fila: FilaEvaluacion, acumuladas: FilaEvaluacion[]): void {
    acumuladas.push(fila);
    this.filas.update((todas) => [...todas, { proveedor, fila }]);
    this.progreso.update((p) => ({ ...p, [proveedor]: { ...p[proveedor], hechos: p[proveedor].hechos + 1 } }));
  }

  private resumir(proveedor: string, filas: FilaEvaluacion[]): void {
    if (!filas.length) return;
    this.api.resumir(proveedor, filas).subscribe({
      next: (r) => this.resumenes.update((lista) => [...lista.filter((x) => x.proveedor !== proveedor), r]),
      error: (e) => this.error.set(mensajeDeError(e)),
    });
  }
}

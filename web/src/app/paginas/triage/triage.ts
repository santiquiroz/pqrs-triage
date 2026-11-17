import { JsonPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { forkJoin, map, of, catchError, Observable } from 'rxjs';
import { TriageApiService, mensajeDeError } from '../../api/triage-api.service';
import { CasoEntrada, Esperado, Proveedor, ResultadoEnrutar, ResultadoTriage } from '../../api/tipos';
import { FormularioCaso } from '../../componentes/formulario-caso';
import { PasosTriage } from '../../componentes/pasos-triage';
import { TarjetaDecision } from '../../componentes/tarjeta-decision';
import { TarjetaJuicio } from '../../componentes/tarjeta-juicio';
import { VistaCorreo } from '../../componentes/vista-correo';
import { CatalogoStore } from '../../estado/catalogo.store';
import { ProveedorStore } from '../../estado/proveedor.store';
import { motivosPlanos } from '../../util/catalogo';
import { corteDe } from '../../util/cortes';
import { claseDestino, clasePrioridad, haceCuanto, milisegundos, porcentaje, usd } from '../../util/formato';

const PASO_MS = 300;
const PAUSA_AUTOMATICA_MS = 3000;

interface FilaComparacion {
  proveedor: string;
  destino?: string;
  motivo?: string | null;
  prioridad?: string;
  tutela?: number | null;
  latenciaMs?: number;
  costoUsd?: number;
  error?: string;
}

interface GrupoExtraido {
  etiqueta: string;
  valores: string[];
}

@Component({
  selector: 'app-triage',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [JsonPipe, FormularioCaso, PasosTriage, TarjetaDecision, TarjetaJuicio, VistaCorreo],
  templateUrl: './triage.html',
  styleUrl: './triage.scss',
})
export class TriagePage implements OnInit {
  private readonly api = inject(TriageApiService);
  private readonly ruta = inject(ActivatedRoute);
  protected readonly proveedores = inject(ProveedorStore);
  protected readonly catalogo = inject(CatalogoStore);
  private temporizadores: ReturnType<typeof setTimeout>[] = [];

  protected readonly casos = signal<CasoEntrada[]>([]);
  protected readonly activoId = signal<string | null>(null);
  protected readonly resultados = signal<Record<string, ResultadoTriage>>({});
  protected readonly corregidos = signal<Record<string, boolean>>({});
  protected readonly enrutados = signal<Record<string, ResultadoEnrutar>>({});
  protected readonly esperados = signal<Record<string, Esperado | null>>({});
  protected readonly errorBandeja = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly clasificandoId = signal<string | null>(null);
  protected readonly paso = signal(0);
  protected readonly automatico = signal(false);
  protected readonly formularioAbierto = signal(false);
  protected readonly corrigiendo = signal(false);
  protected readonly motivoCorreccion = signal('');
  protected readonly enrutando = signal(false);
  protected readonly comparando = signal(false);
  protected readonly comparacion = signal<FilaComparacion[] | null>(null);

  protected readonly caso = computed(() => this.casos().find((c) => c.id === this.activoId()) ?? null);
  protected readonly resultado = computed(() => this.resultadoDe(this.activoId()));
  protected readonly enrutado = computed<ResultadoEnrutar | null>(() => this.enrutados()[this.activoId() ?? ''] ?? null);
  protected readonly esperado = computed<Esperado | null>(() => this.esperados()[this.activoId() ?? ''] ?? null);
  protected readonly puedeClasificar = computed(() => !!this.caso() && !!this.proveedores.seleccionado() && !this.clasificandoId());
  protected readonly nombreProveedor = computed(() => this.proveedores.actual()?.nombre.split(' · ')[0] ?? 'el modelo');
  protected readonly motivos = computed(() => motivosPlanos(this.catalogo.catalogo()));

  protected readonly juicios = computed(() => {
    const r = this.resultado();
    if (!r) return [];
    const umbrales = this.catalogo.umbrales();
    return r.preguntas
      .filter((p) => r.respuestas[p.id])
      .map((p) => ({ pregunta: p, respuesta: r.respuestas[p.id], corte: corteDe(p.id, r.respuestas[p.id], umbrales) }));
  });

  protected readonly extraidos = computed<GrupoExtraido[]>(() => {
    const e = this.resultado()?.extraidos;
    if (!e) return [];
    const grupos: GrupoExtraido[] = [
      { etiqueta: 'NIT', valores: e.nits },
      { etiqueta: 'Facturas', valores: e.facturas },
      { etiqueta: 'Locales', valores: e.locales },
      { etiqueta: 'Radicados judiciales', valores: e.radicadosJudiciales },
      { etiqueta: 'Juzgados', valores: e.juzgados },
      { etiqueta: 'Teléfonos', valores: e.telefonos },
    ];
    return grupos.filter((g) => g.valores.length);
  });

  protected readonly estado = computed(() => {
    const r = this.resultado();
    if (this.clasificandoId()) return `Clasificando con ${this.nombreProveedor()}…`;
    return r ? `Clasificado: ${r.decision.destinoTexto}, prioridad ${r.decision.prioridad}.` : '';
  });

  protected readonly pct = porcentaje;
  protected readonly ms = milisegundos;
  protected readonly usd = usd;
  protected readonly clasePrioridad = clasePrioridad;
  protected readonly claseDestino = claseDestino;

  constructor() {
    inject(DestroyRef).onDestroy(() => this.limpiarTemporizadores());
  }

  ngOnInit(): void {
    this.catalogo.cargar();
    this.cargarBandeja();
  }

  protected hace(creado: number | null | undefined): string {
    return haceCuanto(creado);
  }

  protected etiquetaDestino(destino: string): string {
    return ({ tutela: 'Tutela', automatica: 'Automático', gestor: 'Mesa' } as Record<string, string>)[destino] ?? destino;
  }

  protected insignia(p: Proveedor | null): string {
    if (!p) return '';
    if (p.id === 'jev') return 'API real de Jev';
    return p.tipo === 'reglas' ? 'Sin IA (reglas)' : 'Modelo local';
  }

  protected elegir(id: string): void {
    this.detenerAutomatico();
    this.seleccionar(id);
  }

  protected agregarCaso(caso: CasoEntrada): void {
    this.casos.update((lista) => [caso, ...lista]);
    this.formularioAbierto.set(false);
    this.seleccionar(caso.id!);
  }

  protected clasificar(): void {
    const caso = this.caso();
    const proveedor = this.proveedores.seleccionado();
    if (!caso?.id || !proveedor || this.clasificandoId()) return;
    this.iniciarClasificacion(caso.id);
    this.api.clasificar(proveedor, caso).subscribe({
      next: (r) => this.alClasificar(caso.id!, r),
      error: (e) => this.alFallar(mensajeDeError(e)),
    });
  }

  protected iniciarAutomatico(): void {
    if (this.automatico()) return;
    this.automatico.set(true);
    if (!this.activoId() && this.casos().length) this.seleccionar(this.casos()[0].id!);
    this.procesarAutomatico();
  }

  protected detenerAutomatico(): void {
    this.automatico.set(false);
    this.limpiarTemporizadores();
  }

  protected abrirCorreccion(): void {
    this.motivoCorreccion.set(this.resultado()?.decision.motivo ?? '');
    this.corrigiendo.set(true);
  }

  protected aplicarCorreccion(): void {
    const caso = this.caso();
    const r = this.resultado();
    const motivo = this.motivoCorreccion();
    if (!caso?.id || !r || !motivo) return;
    this.api.corregir(caso, r.respuestas, motivo).subscribe({
      next: (decision) => {
        this.guardarResultado(caso.id!, { ...r, decision });
        this.corregidos.update((c) => ({ ...c, [caso.id!]: true }));
        this.corrigiendo.set(false);
      },
      error: (e) => this.error.set(mensajeDeError(e)),
    });
  }

  protected enrutar(): void {
    const caso = this.caso();
    const r = this.resultado();
    if (!caso?.id || !r) return;
    this.enrutando.set(true);
    this.api.enrutar(caso, r.decision).subscribe({
      next: (res) => {
        this.enrutados.update((e) => ({ ...e, [caso.id!]: res }));
        this.enrutando.set(false);
      },
      error: (e) => {
        this.error.set(mensajeDeError(e));
        this.enrutando.set(false);
      },
    });
  }

  protected comparar(): void {
    const caso = this.caso();
    const disponibles = this.proveedores.disponibles();
    if (!caso || !disponibles.length) return;
    this.comparando.set(true);
    this.comparacion.set(null);
    forkJoin(disponibles.map((p) => this.filaComparacion(p, caso))).subscribe((filas) => {
      this.comparacion.set(filas);
      this.comparando.set(false);
    });
  }

  private filaComparacion(p: Proveedor, caso: CasoEntrada): Observable<FilaComparacion> {
    return this.api.clasificar(p.id, caso).pipe(
      map((r) => ({
        proveedor: p.nombre,
        destino: r.decision.destino,
        motivo: r.decision.motivo,
        prioridad: r.decision.prioridad,
        tutela: r.respuestas['tutela']?.probabilidad ?? null,
        latenciaMs: r.metricas.latenciaMs,
        costoUsd: r.metricas.costoUsd,
      })),
      catchError((e) => of({ proveedor: p.nombre, error: mensajeDeError(e) })),
    );
  }

  private cargarBandeja(): void {
    this.api.bandeja().subscribe({
      next: (lista) => {
        this.errorBandeja.set(null);
        this.casos.set(lista);
        this.cargarCasoDeLaUrl(lista);
      },
      error: (e) => this.errorBandeja.set(mensajeDeError(e)),
    });
  }

  private cargarCasoDeLaUrl(bandeja: CasoEntrada[]): void {
    const params = this.ruta.snapshot.queryParamMap;
    const id = params.get('caso');
    if (!id) {
      if (bandeja.length) this.seleccionar(bandeja[0].id!);
      return;
    }
    this.api.casos(params.get('conjunto') ?? 'todos').subscribe({
      next: (casos) => {
        const encontrado = casos.find((c) => c.id === id);
        if (!encontrado) return this.seleccionar(bandeja[0]?.id ?? '');
        this.esperados.update((e) => ({ ...e, [id]: encontrado.esperado }));
        if (!bandeja.some((c) => c.id === id)) this.casos.update((lista) => [encontrado, ...lista]);
        this.seleccionar(id);
      },
      error: (e) => this.errorBandeja.set(mensajeDeError(e)),
    });
  }

  private seleccionar(id: string): void {
    this.activoId.set(id || null);
    this.paso.set(this.resultadoDe(id) ? 4 : 0);
    this.corrigiendo.set(false);
    this.comparacion.set(null);
    this.error.set(null);
  }

  private iniciarClasificacion(id: string): void {
    this.error.set(null);
    this.comparacion.set(null);
    this.corrigiendo.set(false);
    this.clasificandoId.set(id);
    this.paso.set(1);
    this.programar(() => this.paso.set(2), PASO_MS);
  }

  private alClasificar(id: string, r: ResultadoTriage): void {
    this.guardarResultado(id, r);
    this.corregidos.update((c) => ({ ...c, [id]: false }));
    this.clasificandoId.set(null);
    if (this.activoId() !== id) return;
    this.programar(() => this.paso.set(3), PASO_MS);
    this.programar(() => this.paso.set(4), PASO_MS * 2);
    if (this.automatico()) this.programar(() => this.siguienteAutomatico(), PAUSA_AUTOMATICA_MS);
  }

  private alFallar(mensaje: string): void {
    this.clasificandoId.set(null);
    this.paso.set(0);
    this.error.set(mensaje);
    this.detenerAutomatico();
  }

  private procesarAutomatico(): void {
    if (!this.automatico()) return;
    if (this.resultado()) {
      this.programar(() => this.siguienteAutomatico(), PAUSA_AUTOMATICA_MS);
      return;
    }
    this.clasificar();
  }

  private siguienteAutomatico(): void {
    const lista = this.casos();
    const indice = lista.findIndex((c) => c.id === this.activoId());
    const siguiente = lista[indice + 1];
    if (!this.automatico() || !siguiente?.id) {
      this.automatico.set(false);
      return;
    }
    this.seleccionar(siguiente.id);
    this.procesarAutomatico();
  }

  private guardarResultado(id: string, r: ResultadoTriage): void {
    this.resultados.update((todos) => ({ ...todos, [id]: r }));
  }

  private resultadoDe(id: string | null): ResultadoTriage | null {
    return id ? (this.resultados()[id] ?? null) : null;
  }

  private programar(accion: () => void, ms: number): void {
    this.temporizadores.push(setTimeout(accion, ms));
  }

  private limpiarTemporizadores(): void {
    this.temporizadores.forEach(clearTimeout);
    this.temporizadores = [];
  }
}

import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Subscription, debounceTime, filter } from 'rxjs';
import { TriageApiService, mensajeDeError } from '../../api/triage-api.service';
import { CasoEntrada, CorreoPropuesto, ResultadoEnrutar, ResultadoTriage } from '../../api/tipos';
import { VistaCorreo } from '../../componentes/vista-correo';
import { CatalogoStore } from '../../estado/catalogo.store';
import { ProveedorStore } from '../../estado/proveedor.store';
import { clasePrioridad, fechaEsCo, plazo, porcentaje } from '../../util/formato';

const ESPERA_MS = 1200;
const MINIMO_CARACTERES = 40;

@Component({
  selector: 'app-radicar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, VistaCorreo],
  templateUrl: './radicar.html',
  styleUrl: './radicar.scss',
})
export class RadicarPage implements OnInit {
  private readonly api = inject(TriageApiService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly proveedores = inject(ProveedorStore);
  protected readonly catalogo = inject(CatalogoStore);
  private consulta: Subscription | null = null;
  private recalculo: Subscription | null = null;

  protected readonly form = new FormGroup({
    nombre: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    correo: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    empresa: new FormControl('', { nonNullable: true }),
    tipo: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    mensaje: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(MINIMO_CARACTERES)] }),
  });

  protected readonly sugerencia = signal<ResultadoTriage | null>(null);
  protected readonly pensando = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly radicando = signal(false);
  protected readonly radicado = signal<ResultadoEnrutar | null>(null);

  private readonly tipoElegido = toSignal(this.form.controls.tipo.valueChanges, { initialValue: '' });
  protected readonly tipos = computed(() => this.catalogo.catalogo()?.tipos ?? []);
  protected readonly ayudaTipo = computed(() => this.tipos().find((t) => t.id === this.tipoElegido())?.desc ?? '');
  protected readonly decision = computed(() => this.sugerencia()?.decision ?? null);
  protected readonly confianzaTipologia = computed(() => {
    const r = this.sugerencia()?.respuestas['tipologia'];
    if (!r) return null;
    return r.confianza ?? Math.max(0, ...Object.values(r.probabilidades));
  });
  protected readonly senales = computed(() => {
    const r = this.sugerencia()?.respuestas;
    const u = this.catalogo.umbrales();
    if (!r || !u) return [];
    return [
      { etiqueta: 'Parece una tutela', p: r['tutela']?.probabilidad ?? 0, activa: (r['tutela']?.probabilidad ?? 0) >= u.tutela },
      { etiqueta: 'Parece un derecho de petición', p: r['derecho_peticion']?.probabilidad ?? 0, activa: (r['derecho_peticion']?.probabilidad ?? 0) >= u.derechoPeticion },
    ];
  });
  protected readonly acuse = computed<CorreoPropuesto | null>(
    () => this.radicado()?.correos.find((c) => c.tipo === 'acuse-cliente') ?? null,
  );

  protected readonly pct = porcentaje;
  protected readonly clasePrioridad = clasePrioridad;
  protected readonly fecha = fechaEsCo;
  protected readonly plazo = plazo;

  ngOnInit(): void {
    this.catalogo.cargar();
    this.form.controls.mensaje.valueChanges
      .pipe(
        debounceTime(ESPERA_MS),
        filter((texto) => texto.trim().length >= MINIMO_CARACTERES),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => this.sugerir());
    this.form.controls.tipo.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.recalcular());
  }

  protected invalido(campo: keyof typeof this.form.controls): boolean {
    const control = this.form.controls[campo];
    return control.invalid && (control.touched || control.dirty);
  }

  protected radicar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const decision = this.decision();
    if (!decision) {
      this.sugerir();
      return;
    }
    this.radicando.set(true);
    this.api.enrutar(this.caso(), decision).subscribe({
      next: (r) => {
        this.radicado.set(r);
        this.radicando.set(false);
      },
      error: (e) => {
        this.error.set(mensajeDeError(e));
        this.radicando.set(false);
      },
    });
  }

  protected otraSolicitud(): void {
    this.form.reset();
    this.sugerencia.set(null);
    this.radicado.set(null);
  }

  private sugerir(): void {
    const proveedor = this.proveedores.seleccionado();
    if (!proveedor || this.form.controls.mensaje.value.trim().length < MINIMO_CARACTERES) return;
    this.consulta?.unsubscribe();
    this.pensando.set(true);
    this.error.set(null);
    this.consulta = this.api.clasificar(proveedor, this.caso()).subscribe({
      next: (r) => {
        this.sugerencia.set(r);
        this.pensando.set(false);
        this.recalcular();
      },
      error: (e) => {
        this.error.set(mensajeDeError(e));
        this.pensando.set(false);
      },
    });
  }

  private recalcular(): void {
    const r = this.sugerencia();
    if (!r) return;
    this.recalculo?.unsubscribe();
    this.recalculo = this.api.decidir(this.caso(), r.respuestas).subscribe({
      next: (decision) => this.sugerencia.set({ ...r, decision }),
      error: (e) => this.error.set(mensajeDeError(e)),
    });
  }

  private caso(): CasoEntrada {
    const v = this.form.getRawValue();
    return {
      canal: 'Portal',
      remitente: { nombre: v.nombre || 'Cliente', empresa: v.empresa || null, correo: v.correo || 'cliente@ejemplo.example' },
      asunto: v.mensaje.slice(0, 80),
      descripcion: v.mensaje,
      tipoDeclarado: v.tipo || null,
    };
  }
}

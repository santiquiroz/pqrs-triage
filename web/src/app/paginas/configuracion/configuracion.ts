import { ChangeDetectionStrategy, Component, OnInit, computed, effect, inject, signal } from '@angular/core';
import { TriageApiService, mensajeDeError } from '../../api/triage-api.service';
import { Area, CorreoEnviado, Umbrales } from '../../api/tipos';
import { VistaCorreo } from '../../componentes/vista-correo';
import { CatalogoStore } from '../../estado/catalogo.store';
import { nombreUsuario, proximosFestivos } from '../../util/catalogo';
import { decimal, diaEsCo, fechaEsCo, plazo, porcentaje } from '../../util/formato';

const CORREO = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

interface Deslizador {
  clave: keyof Umbrales;
  nombre: string;
  efecto: string;
  maximo: number;
  paso: number;
}

@Component({
  selector: 'app-configuracion',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [VistaCorreo],
  templateUrl: './configuracion.html',
  styleUrl: './configuracion.scss',
})
export class ConfiguracionPage implements OnInit {
  private readonly api = inject(TriageApiService);
  protected readonly catalogo = inject(CatalogoStore);

  protected readonly borradores = signal<Record<string, string>>({});
  protected readonly guardando = signal<string | null>(null);
  protected readonly avisos = signal<Record<string, string>>({});
  protected readonly umbrales = signal<Umbrales | null>(null);
  protected readonly avisoUmbrales = signal<string | null>(null);
  protected readonly correos = signal<CorreoEnviado[]>([]);
  protected readonly error = signal<string | null>(null);

  protected readonly deslizadores: Deslizador[] = [
    { clave: 'tutela', nombre: 'Tutela', efecto: 'Desde aquí el caso va directo a Jurídica con ANS de 2 días hábiles.', maximo: 1, paso: 0.01 },
    { clave: 'encajaCatalogo', nombre: 'Encaja en el catálogo', efecto: 'Por debajo, el caso va a la mesa como «Por clasificar».', maximo: 1, paso: 0.01 },
    { clave: 'confianzaTipologia', nombre: 'Confianza en la tipología', efecto: 'Por debajo, va a la mesa con la tipología como sugerencia.', maximo: 1, paso: 0.01 },
    { clave: 'derechoPeticion', nombre: 'Derecho de petición', efecto: 'Desde aquí el ANS se limita a 15 días hábiles.', maximo: 1, paso: 0.01 },
    { clave: 'riesgoLegal', nombre: 'Riesgo legal', efecto: 'Desde aquí se copia a Jurídica.', maximo: 1, paso: 0.01 },
    { clave: 'urgenciaCritica', nombre: 'Urgencia crítica (0 a 3)', efecto: 'Desde aquí se copia a la mesa.', maximo: 3, paso: 0.1 },
  ];

  protected readonly ans = computed(() => Object.entries(this.catalogo.catalogo()?.prioridades ?? {}));
  protected readonly festivos = computed(() => proximosFestivos(this.catalogo.catalogo()?.festivos ?? [], new Date().toISOString().slice(0, 10)));

  protected readonly fecha = fechaEsCo;
  protected readonly dia = diaEsCo;
  protected readonly plazo = plazo;

  constructor() {
    effect(() => {
      const u = this.catalogo.umbrales();
      if (u && !this.umbrales()) this.umbrales.set({ ...u });
    });
  }

  ngOnInit(): void {
    this.catalogo.cargar(true);
    this.cargarCorreos();
  }

  protected usuario(id: string | null): string {
    return nombreUsuario(this.catalogo.catalogo(), id);
  }

  protected correoDe(area: Area): string {
    return this.borradores()[area.id] ?? area.correo;
  }

  protected escribirCorreo(area: Area, evento: Event): void {
    const valor = (evento.target as HTMLInputElement).value;
    this.borradores.update((b) => ({ ...b, [area.id]: valor }));
  }

  protected correoValido(area: Area): boolean {
    return CORREO.test(this.correoDe(area).trim());
  }

  protected guardarCorreo(area: Area): void {
    if (!this.correoValido(area)) return;
    this.guardando.set(area.id);
    this.api.actualizarCorreoArea(area.id, this.correoDe(area).trim()).subscribe({
      next: () => {
        this.guardando.set(null);
        this.avisos.update((a) => ({ ...a, [area.id]: 'Guardado' }));
        this.catalogo.cargar(true);
      },
      error: (e) => {
        this.guardando.set(null);
        this.avisos.update((a) => ({ ...a, [area.id]: mensajeDeError(e) }));
      },
    });
  }

  protected valorUmbral(clave: keyof Umbrales): number {
    return this.umbrales()?.[clave] ?? 0;
  }

  protected textoUmbral(d: Deslizador): string {
    const valor = this.valorUmbral(d.clave);
    return d.maximo === 1 ? porcentaje(valor) : decimal(valor);
  }

  protected moverUmbral(clave: keyof Umbrales, evento: Event): void {
    const valor = Number((evento.target as HTMLInputElement).value);
    this.umbrales.update((u) => (u ? { ...u, [clave]: valor } : u));
  }

  protected guardarUmbrales(): void {
    const u = this.umbrales();
    if (!u) return;
    this.api.actualizarUmbrales(u).subscribe({
      next: (guardados) => {
        this.catalogo.conUmbrales(guardados);
        this.avisoUmbrales.set('Umbrales guardados. Aplican desde la próxima clasificación.');
      },
      error: (e) => this.avisoUmbrales.set(mensajeDeError(e)),
    });
  }

  protected cargarCorreos(): void {
    this.api.correos().subscribe({ next: (c) => this.correos.set(c), error: (e) => this.error.set(mensajeDeError(e)) });
  }
}

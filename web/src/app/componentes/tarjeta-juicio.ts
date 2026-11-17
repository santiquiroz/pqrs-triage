import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { Pregunta, Respuesta, TipoProveedor } from '../api/tipos';
import { BarrasProbabilidad } from './barras-probabilidad';
import { Corte, MAXIMO_URGENCIA } from '../util/cortes';
import { Barra, barrasDeRespuesta, confianzaDe, decimal, nombreTipo, origenConfianza, porcentaje, valorPrincipal } from '../util/formato';

@Component({
  selector: 'app-tarjeta-juicio',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [BarrasProbabilidad],
  template: `
    <article class="juicio" [class.cruzado]="corte()?.cruzado">
      <header>
        <span class="insignia" [class]="'insignia t-' + pregunta().tipo">{{ insignia() }}</span>
        <span class="tipo">{{ nombre() }}</span>
        <span class="id">{{ pregunta().id }}</span>
      </header>
      <p class="instruccion" [title]="pregunta().instrucciones">{{ pregunta().instrucciones }}</p>
      <div class="respuesta">
        <strong class="eleccion">{{ valor() }}</strong>
        @if (respuesta().tipo === 'score') {
          <span class="cifra puntaje">{{ puntaje() }} de {{ maximo }}</span>
        }
      </div>
      <p class="confianza suave">
        Confianza <span class="cifra">{{ confianza() }}</span> · {{ origen() }}
      </p>
      @if (escala(); as e) {
        <app-barras-probabilidad [barras]="e" [corte]="corte()" />
        <app-barras-probabilidad class="niveles" [barras]="barras()" [destacarPrimera]="respuesta().tipo !== 'score'" />
      } @else {
        <app-barras-probabilidad [barras]="barras()" [corte]="corte()" />
      }
      @if (corte(); as c) {
        <p class="regla">
          <span class="marca" aria-hidden="true">{{ c.cruzado ? '▲' : '▽' }}</span>
          Regla en código: {{ c.cruzado ? 'supera' : 'no llega a' }} {{ c.etiqueta }} → {{ c.efecto }}
        </p>
      }
    </article>
  `,
  styles: `
    :host { display: block; }
    .juicio {
      height: 100%; display: grid; gap: 8px; align-content: start;
      background: var(--superficie); border: 1px solid var(--borde); border-radius: var(--radio-tarjeta); padding: 14px 16px;
      animation: aparecer 360ms var(--curva) both;
    }
    .juicio.cruzado { border-color: color-mix(in srgb, var(--navy) 35%, var(--borde)); }
    header { display: flex; align-items: center; gap: 8px; }
    .insignia { font-size: 0.66rem; font-weight: 700; letter-spacing: 0.08em; text-transform: uppercase; padding: 2px 7px; border-radius: 3px; color: var(--navy); }
    .t-choice { background: var(--cian-suave); box-shadow: inset 0 0 0 1px var(--cian); }
    .t-score { background: #f8eed6; box-shadow: inset 0 0 0 1px var(--dorado); }
    .t-noul { background: #e3f2f0; box-shadow: inset 0 0 0 1px var(--teal); }
    .tipo { font-size: 0.78rem; color: var(--texto-suave); }
    .id { margin-left: auto; font-family: ui-monospace, 'Cascadia Code', monospace; font-size: 0.72rem; color: var(--gris); }
    .instruccion { font-size: 0.82rem; color: var(--texto-suave); display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; }
    .respuesta { display: flex; align-items: baseline; gap: 10px; flex-wrap: wrap; }
    .eleccion { font-family: var(--fuente-titulo); font-size: 1.02rem; font-weight: 600; color: var(--navy); line-height: 1.25; }
    .puntaje { font-size: 0.85rem; font-weight: 600; color: var(--texto-suave); }
    .confianza { font-size: 0.78rem; }
    .niveles { margin-top: 2px; }
    .regla { font-size: 0.78rem; color: var(--navy); border-top: 1px dashed var(--borde-fuerte); padding-top: 8px; }
    .marca { color: var(--azul); margin-right: 2px; }
    @keyframes aparecer { from { opacity: 0; transform: translateY(6px); } to { opacity: 1; transform: none; } }
  `,
})
export class TarjetaJuicio {
  readonly pregunta = input.required<Pregunta>();
  readonly respuesta = input.required<Respuesta>();
  readonly corte = input<Corte | null>(null);
  readonly tipoProveedor = input<TipoProveedor>('decision');

  protected readonly maximo = MAXIMO_URGENCIA;

  protected readonly insignia = computed(() => ({ choice: 'Choice', score: 'Score', noul: 'Noul' })[this.pregunta().tipo]);
  protected readonly nombre = computed(() => nombreTipo(this.pregunta().tipo));
  protected readonly valor = computed(() => valorPrincipal(this.respuesta()));
  protected readonly confianza = computed(() => porcentaje(confianzaDe(this.respuesta())));
  protected readonly puntaje = computed(() => decimal(this.respuesta().puntaje));
  protected readonly barras = computed(() => barrasDeRespuesta(this.respuesta(), this.pregunta().niveles ?? []));

  protected readonly origen = computed(() => origenConfianza(this.tipoProveedor(), this.respuesta().confianzaCalibrada));

  protected readonly escala = computed<Barra[] | null>(() => {
    const r = this.respuesta();
    if (r.tipo === 'score') {
      const puntaje = r.puntaje ?? 0;
      return [{ clave: 'escala', etiqueta: 'Puntaje esperado', valor: puntaje / MAXIMO_URGENCIA, texto: `${decimal(puntaje)}/${MAXIMO_URGENCIA}` }];
    }
    if (r.tipo === 'choice' && this.corte()) return [{ clave: 'confianza', etiqueta: 'Confianza en la elección', valor: confianzaDe(r) ?? 0 }];
    return null;
  });
}

import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { Decision } from '../api/tipos';
import { claseDestino, clasePrioridad, fechaEsCo, plazo } from '../util/formato';

@Component({
  selector: 'app-tarjeta-decision',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="decision" [class]="'decision ' + destinoClase()" aria-labelledby="titulo-decision">
      <span class="eyebrow">Decisión del código{{ corregida() ? ' · corregida por una persona' : '' }}</span>
      <h3 id="titulo-decision">
        <span class="pildora" [class]="'pildora ' + destinoClase()">{{ etiquetaDestino() }}</span>
        {{ decision().destinoTexto }}
      </h3>
      <dl>
        <div><dt>Tipo</dt><dd>{{ decision().tipo ?? 'Sin tipo' }}</dd></div>
        <div class="ancho"><dt>Área › grupo › tipología</dt><dd>{{ ruta() }}</dd></div>
        <div><dt>Responsable</dt><dd>{{ responsable() }}</dd></div>
        <div><dt>Prioridad</dt><dd><span class="pildora" [class]="'pildora ' + prioridadClase()">{{ decision().prioridad }}</span></dd></div>
        <div class="ancho"><dt>ANS</dt><dd class="cifra">{{ plazoAns() }} · vence {{ vence() }}</dd></div>
        <div><dt>Activo</dt><dd>{{ decision().activo ?? 'No identificado' }}</dd></div>
        @if (decision().etiquetas.length) {
          <div class="ancho"><dt>Etiquetas</dt><dd class="chips">@for (e of decision().etiquetas; track e) { <span class="chip">{{ e }}</span> }</dd></div>
        }
      </dl>
      @if (decision().notas.length) {
        <div class="nota" role="note">
          <strong>Nota de la IA</strong>
          <ul>@for (n of decision().notas; track n) { <li>{{ n }}</li> }</ul>
        </div>
      }
      @if (decision().requiereRevisionHumana) {
        <p class="aviso">Requiere revisión humana antes de responder al cliente.</p>
      }
      <h4>Por qué</h4>
      <ul class="razones">
        @for (r of decision().razones; track r.regla) {
          <li [class.disparada]="r.disparada">
            <span class="icono" aria-hidden="true">{{ r.disparada ? '✓' : '·' }}</span>
            <span><span class="regla">{{ r.regla }}</span> — {{ r.detalle }}</span>
            <span class="visualmente-oculto">{{ r.disparada ? '(se aplicó)' : '(no se aplicó)' }}</span>
          </li>
        }
      </ul>
    </section>
  `,
  styles: `
    .decision {
      position: relative; display: grid; gap: 12px; padding: 18px 20px 18px 24px;
      background: var(--superficie); border: 1px solid var(--borde); border-radius: var(--radio-tarjeta); box-shadow: var(--sombra-activa);
    }
    .decision::before { content: ''; position: absolute; left: 0; top: 14px; bottom: 14px; width: 5px; border-radius: 0 3px 3px 0; background: var(--dorado); }
    .decision.d-tutela::before { background: var(--navy); }
    .decision.d-automatica::before { background: var(--teal); }
    h3 { display: flex; flex-wrap: wrap; align-items: center; gap: 10px; font-size: 1.2rem; }
    dl { margin: 0; display: grid; grid-template-columns: repeat(auto-fill, minmax(150px, 1fr)); gap: 10px 18px; }
    dl .ancho { grid-column: 1 / -1; }
    dt { font-size: 0.7rem; font-weight: 700; letter-spacing: 0.08em; text-transform: uppercase; color: var(--texto-suave); }
    dd { margin: 2px 0 0; font-size: 0.92rem; }
    .chips { display: flex; flex-wrap: wrap; gap: 6px; }
    h4 { font-size: 0.85rem; margin-top: 2px; }
    .razones { list-style: none; margin: 0; padding: 0; display: grid; gap: 5px; font-size: 0.84rem; color: var(--gris); }
    .razones li { display: grid; grid-template-columns: 18px 1fr; gap: 6px; }
    .disparada { color: var(--texto); }
    .disparada .regla { font-weight: 700; color: var(--navy); }
    .icono { font-weight: 700; color: var(--teal); text-align: center; }
    .nota { padding: 10px 12px; border-left: 3px solid var(--azul); background: var(--cian-suave); font-size: 0.86rem; }
    .nota strong { color: var(--navy); }
    .nota ul { margin: 6px 0 0; padding-left: 18px; display: grid; gap: 4px; }
  `,
})
export class TarjetaDecision {
  readonly decision = input.required<Decision>();
  readonly corregida = input(false);

  protected readonly destinoClase = computed(() => claseDestino(this.decision().destino));
  protected readonly prioridadClase = computed(() => clasePrioridad(this.decision().prioridad));
  protected readonly vence = computed(() => fechaEsCo(this.decision().venceEn));
  protected readonly plazoAns = computed(() => plazo(this.decision().ansDias, this.decision().tipoDias));
  protected readonly etiquetaDestino = computed(() => ({ tutela: 'Tutela', automatica: 'Automático', gestor: 'Mesa' })[this.decision().destino]);
  protected readonly ruta = computed(() => {
    const d = this.decision();
    return [d.area?.nombre, d.grupo, d.motivo].filter(Boolean).join(' › ') || 'Sin ubicar en el catálogo';
  });
  protected readonly responsable = computed(() => {
    const r = this.decision().responsable;
    return r ? `${r.nombre}${r.cargo ? ' · ' + r.cargo : ''}` : 'Lo asigna la mesa';
  });
}

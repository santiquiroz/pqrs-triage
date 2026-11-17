import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Barra, porcentaje } from '../util/formato';
import { Corte } from '../util/cortes';

@Component({
  selector: 'app-barras-probabilidad',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="barras" [class.con-corte]="corte()" [style.--corte]="corte()?.posicion ?? 0">
      @if (corte(); as c) {
        <span class="corte" aria-hidden="true"><span class="bandera">corte {{ c.etiqueta }}</span></span>
      }
      @for (barra of barras(); track barra.clave; let primera = $first) {
        <div class="fila" [class.principal]="primera && destacarPrimera()">
          <span class="etiqueta" [title]="barra.etiqueta">{{ barra.etiqueta }}</span>
          <span class="pista" role="meter" aria-valuemin="0" aria-valuemax="100" [attr.aria-valuenow]="valorAria(barra)" [attr.aria-label]="barra.etiqueta">
            <span class="relleno" [style.width.%]="barra.valor * 100"></span>
          </span>
          <span class="cifra valor">{{ barra.texto ?? pct(barra.valor) }}</span>
        </div>
      }
    </div>
  `,
  styles: `
    :host { display: block; }
    .barras { position: relative; display: grid; gap: 7px; padding-right: 3.6rem; }
    .barras.con-corte { padding-top: 20px; }
    .fila { display: grid; gap: 3px; position: relative; }
    .etiqueta { position: relative; z-index: 2; justify-self: start; max-width: 100%; padding-right: 6px; background: var(--superficie); font-size: 0.8rem; color: var(--texto-suave); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .principal .etiqueta { color: var(--texto); font-weight: 600; }
    .pista { display: block; height: 8px; border-radius: 2px; background: var(--fondo); box-shadow: inset 0 0 0 1px var(--borde); overflow: hidden; }
    .relleno { display: block; height: 100%; background: color-mix(in srgb, var(--cian) 45%, white); transform-origin: left; animation: crecer 520ms var(--curva) both; }
    .principal .relleno { background: var(--cian); }
    .valor { position: absolute; right: -3.6rem; bottom: -4px; width: 3.3rem; text-align: right; font-size: 0.82rem; font-weight: 600; color: var(--texto); }
    .corte {
      position: absolute; top: 0; bottom: -2px; z-index: 1; width: 0;
      left: calc(var(--corte) * (100% - 3.6rem));
      border-left: 2px dashed var(--navy);
    }
    .bandera {
      position: absolute; top: 0; left: -1px; transform: translateX(-50%);
      background: var(--navy); color: #fff; border-radius: 3px;
      font-size: 0.66rem; font-weight: 700; letter-spacing: 0.04em; padding: 1px 6px; white-space: nowrap;
    }
    @keyframes crecer { from { transform: scaleX(0); } to { transform: scaleX(1); } }
  `,
})
export class BarrasProbabilidad {
  readonly barras = input.required<Barra[]>();
  readonly corte = input<Corte | null>(null);
  readonly destacarPrimera = input(true);

  protected pct(valor: number): string {
    return porcentaje(valor);
  }

  protected valorAria(barra: Barra): number {
    return Math.round(barra.valor * 100);
  }
}

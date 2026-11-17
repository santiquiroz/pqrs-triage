import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-pasos-triage',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ol class="pasos" aria-label="Avance del triage">
      @for (paso of pasos; track paso; let i = $index) {
        <li [class.hecho]="actual() > i + 1" [class.activo]="actual() === i + 1" [attr.aria-current]="actual() === i + 1 ? 'step' : null">
          <span class="punto cifra" aria-hidden="true">{{ actual() > i + 1 ? '✓' : i + 1 }}</span>
          <span class="texto">{{ paso }}</span>
        </li>
      }
    </ol>
  `,
  styles: `
    .pasos { list-style: none; margin: 0; padding: 0; display: grid; grid-template-columns: repeat(4, 1fr); gap: 6px; }
    li { position: relative; display: grid; justify-items: center; gap: 6px; text-align: center; font-size: 0.78rem; color: var(--gris); }
    li:not(:last-child)::after {
      content: ''; position: absolute; top: 13px; left: calc(50% + 18px); right: calc(-50% + 18px);
      height: 2px; background: var(--borde); transition: background var(--duracion) var(--curva);
    }
    .punto {
      width: 28px; height: 28px; border-radius: 50%; display: grid; place-items: center;
      font-weight: 700; font-size: 0.8rem; background: var(--superficie); border: 2px solid var(--borde-fuerte); color: var(--gris);
      transition: all var(--duracion) var(--curva);
    }
    .activo { color: var(--navy); font-weight: 600; }
    .activo .punto { border-color: var(--cian); color: var(--navy); box-shadow: 0 0 0 4px var(--cian-suave); animation: latido 1.2s ease-in-out infinite; }
    .hecho { color: var(--texto); }
    .hecho .punto { background: var(--navy); border-color: var(--navy); color: #fff; }
    .hecho::after { background: var(--navy) !important; }
    @keyframes latido { 50% { box-shadow: 0 0 0 7px var(--cian-suave); } }
  `,
})
export class PasosTriage {
  readonly actual = input(0);
  protected readonly pasos = ['Leer el caso', 'Hacer 8 preguntas', 'Revisar las respuestas', 'Enrutar'];
}

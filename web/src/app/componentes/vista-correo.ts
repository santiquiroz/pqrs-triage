import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { CorreoPropuesto } from '../api/tipos';
import { destinatarios, nombreCorreo } from '../util/formato';

@Component({
  selector: 'app-vista-correo',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article class="correo">
      <div class="cabecera">
        <div class="datos">
          <span class="eyebrow">{{ tipo() }}</span>
          <strong class="asunto">{{ correo().asunto }}</strong>
          <span class="para suave">Para {{ para() }}</span>
        </div>
        <button type="button" class="boton secundario" (click)="abierto.set(!abierto())" [attr.aria-expanded]="abierto()">
          {{ abierto() ? 'Ocultar correo' : 'Ver correo' }}
        </button>
      </div>
      @if (abierto()) {
        <iframe sandbox="" [srcdoc]="cuerpo()" [title]="'Vista previa: ' + correo().asunto"></iframe>
      }
    </article>
  `,
  styles: `
    .correo { border: 1px solid var(--borde); border-radius: 10px; padding: 12px 14px; background: var(--superficie); }
    .cabecera { display: flex; gap: 12px; align-items: center; justify-content: space-between; flex-wrap: wrap; }
    .datos { display: grid; gap: 2px; min-width: 0; }
    .asunto { font-size: 0.9rem; color: var(--texto); overflow-wrap: anywhere; }
    .para { font-size: 0.78rem; overflow-wrap: anywhere; }
    iframe { display: block; width: 100%; height: 440px; margin-top: 12px; border: 1px solid var(--borde); border-radius: 6px; background: #fff; }
  `,
})
export class VistaCorreo {
  private readonly sanitizer = inject(DomSanitizer);

  readonly correo = input.required<CorreoPropuesto>();
  protected readonly abierto = signal(false);
  protected readonly tipo = computed(() => nombreCorreo(this.correo().tipo));
  protected readonly para = computed(() => destinatarios(this.correo()));
  // iframe sin allow-scripts: el HTML no ejecuta código, solo conserva los estilos en línea de la plantilla.
  protected readonly cuerpo = computed<SafeHtml>(() => this.sanitizer.bypassSecurityTrustHtml(this.correo().cuerpoHtml));
}

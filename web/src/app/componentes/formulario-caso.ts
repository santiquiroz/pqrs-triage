import { ChangeDetectionStrategy, Component, output } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { CasoEntrada } from '../api/tipos';

@Component({
  selector: 'app-formulario-caso',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  template: `
    <form class="formulario tarjeta" [formGroup]="form" (ngSubmit)="agregar()" novalidate>
      <h3>Escribir un caso</h3>
      <div class="dos">
        <label class="campo">Canal
          <select formControlName="canal"><option value="Correo">Correo</option><option value="Portal">Portal</option></select>
        </label>
        <label class="campo">Nombre
          <input formControlName="nombre" autocomplete="off" />
          @if (invalido('nombre')) { <span class="error-campo">Escribe el nombre.</span> }
        </label>
        <label class="campo">Empresa (opcional)
          <input formControlName="empresa" autocomplete="off" />
        </label>
        <label class="campo">Correo
          <input formControlName="correo" type="email" autocomplete="off" />
          @if (invalido('correo')) { <span class="error-campo">Escribe un correo válido.</span> }
        </label>
      </div>
      <label class="campo">Asunto
        <input formControlName="asunto" autocomplete="off" />
      </label>
      <label class="campo">Mensaje
        <textarea formControlName="mensaje" rows="5"></textarea>
        @if (invalido('mensaje')) { <span class="error-campo">Cuenta el caso en al menos 20 caracteres.</span> }
      </label>
      <div class="acciones">
        <button type="button" class="boton secundario" (click)="cancelado.emit()">Cancelar</button>
        <button type="submit" class="boton">Agregar a la bandeja</button>
      </div>
    </form>
  `,
  styles: `
    .formulario { display: grid; gap: 12px; }
    .dos { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 12px; }
    .acciones { display: flex; justify-content: flex-end; gap: 8px; }
  `,
})
export class FormularioCaso {
  readonly agregado = output<CasoEntrada>();
  readonly cancelado = output<void>();

  protected readonly form = new FormGroup({
    canal: new FormControl<'Correo' | 'Portal'>('Correo', { nonNullable: true }),
    nombre: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    empresa: new FormControl('', { nonNullable: true }),
    correo: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    asunto: new FormControl('', { nonNullable: true }),
    mensaje: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(20)] }),
  });

  protected invalido(campo: keyof typeof this.form.controls): boolean {
    const control = this.form.controls[campo];
    return control.invalid && (control.touched || control.dirty);
  }

  protected agregar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    this.agregado.emit({
      id: `LOCAL-${Date.now().toString(36).toUpperCase()}`,
      canal: v.canal,
      buzon: v.canal === 'Correo' ? 'pqrs@suruworks.example' : null,
      remitente: { nombre: v.nombre, empresa: v.empresa || null, correo: v.correo },
      asunto: v.asunto || v.mensaje.slice(0, 70),
      descripcion: v.mensaje,
      creado: Date.now(),
    });
    this.form.reset();
  }
}

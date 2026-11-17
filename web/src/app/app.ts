import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ProveedorStore } from './estado/proveedor.store';

@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit {
  protected readonly proveedores = inject(ProveedorStore);

  protected readonly enlaces = [
    { ruta: '/triage', texto: 'Triage en vivo' },
    { ruta: '/radicar', texto: 'Radicar' },
    { ruta: '/evaluacion', texto: 'Evaluación' },
    { ruta: '/configuracion', texto: 'Configuración' },
    { ruta: '/como-funciona', texto: 'Cómo funciona' },
  ];

  ngOnInit(): void {
    this.proveedores.cargar();
  }

  protected cambiarProveedor(evento: Event): void {
    this.proveedores.elegir((evento.target as HTMLSelectElement).value);
  }
}

import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'triage' },
  { path: 'triage', title: 'Triage en vivo · Mesa de PQRS', loadComponent: () => import('./paginas/triage/triage').then((m) => m.TriagePage) },
  { path: 'radicar', title: 'Radicar · Mesa de PQRS', loadComponent: () => import('./paginas/radicar/radicar').then((m) => m.RadicarPage) },
  { path: 'evaluacion', title: 'Evaluación · Mesa de PQRS', loadComponent: () => import('./paginas/evaluacion/evaluacion').then((m) => m.EvaluacionPage) },
  { path: 'configuracion', title: 'Configuración · Mesa de PQRS', loadComponent: () => import('./paginas/configuracion/configuracion').then((m) => m.ConfiguracionPage) },
  { path: 'como-funciona', title: 'Cómo funciona · Mesa de PQRS', loadComponent: () => import('./paginas/como-funciona/como-funciona').then((m) => m.ComoFuncionaPage) },
  { path: '**', redirectTo: 'triage' },
];

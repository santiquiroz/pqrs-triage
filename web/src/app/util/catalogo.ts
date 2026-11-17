import { Catalogo, Motivo, Usuario } from '../api/tipos';

export interface MotivoPlano {
  area: string;
  areaId: string;
  grupo: string;
  motivo: Motivo;
}

export function motivosPlanos(catalogo: Catalogo | null, soloPublicos = false): MotivoPlano[] {
  if (!catalogo) return [];
  return catalogo.areas.flatMap((area) =>
    area.grupos.flatMap((grupo) =>
      grupo.motivos
        .filter((motivo) => !soloPublicos || motivo.publico)
        .map((motivo) => ({ area: area.nombre, areaId: area.id, grupo: grupo.nombre, motivo })),
    ),
  );
}

export function nombreUsuario(catalogo: Catalogo | null, id: string | null): string {
  if (!id) return 'Sin asignar';
  const usuario: Usuario | undefined = catalogo?.usuarios.find((u) => u.id === id);
  return usuario?.nombre ?? id;
}

export function proximosFestivos(festivos: string[], hoy: string, cantidad = 5): string[] {
  return festivos.filter((f) => f >= hoy).sort().slice(0, cantidad);
}

import { ChangeDetectionStrategy, Component } from '@angular/core';

interface PreguntaDoc {
  id: string;
  tipo: 'choice' | 'score' | 'noul';
  pregunta: string;
  uso: string;
}

interface ProveedorDoc {
  nombre: string;
  donde: string;
  datos: string;
}

@Component({
  selector: 'app-como-funciona',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './como-funciona.html',
  styleUrl: './como-funciona.scss',
})
export class ComoFuncionaPage {
  protected readonly nombresTipo = { choice: 'Elección', score: 'Puntaje', noul: 'Sí / No' };

  protected readonly preguntas: PreguntaDoc[] = [
    { id: 'tipo', tipo: 'choice', pregunta: '¿Qué tipo de PQRS es?', uso: 'Petición, queja, reclamo, sugerencia o felicitación, con las definiciones de SuruWorks.' },
    { id: 'tipologia', tipo: 'choice', pregunta: '¿Qué tipología del catálogo describe el caso?', uso: 'Ubica el área, el grupo, el responsable y la prioridad base. Son 38 opciones.' },
    { id: 'encaja_catalogo', tipo: 'noul', pregunta: '¿Alguna tipología describe la solicitud?', uso: 'Si no encaja (spam, publicidad), va a la mesa en vez de forzar una tipología.' },
    { id: 'activo', tipo: 'choice', pregunta: '¿A qué activo se refiere?', uso: 'Centro comercial, edificio o bodega; «No identificado» si no lo nombra.' },
    { id: 'urgencia', tipo: 'score', pregunta: '¿Qué tan urgente es?', uso: 'De 0 a 3. Puede subir la prioridad del catálogo; si es crítica, copia a la mesa.' },
    { id: 'tutela', tipo: 'noul', pregunta: '¿Es una acción de tutela?', uso: 'Si lo es: Jurídica, ANS de 2 días hábiles y sin acuse automático al remitente.' },
    { id: 'derecho_peticion', tipo: 'noul', pregunta: '¿Invoca el derecho de petición?', uso: 'Ley 1755 de 2015: el ANS no puede pasar de 15 días hábiles.' },
    { id: 'riesgo_legal', tipo: 'noul', pregunta: '¿Trae riesgo legal?', uso: 'Amenaza de demanda, queja ante una superintendencia o requerimiento de autoridad: copia a Jurídica.' },
  ];

  protected readonly proveedores: ProveedorDoc[] = [
    {
      nombre: 'Jev (TypeSafe)',
      donde: 'API de TypeSafe en EE. UU. Modelo de decisiones: responde preguntas tipadas con probabilidades calibradas.',
      datos: 'Sale el texto del caso. No entrena con datos de clientes; retención cero solo en el plan empresarial. Exige revisar Ley 1581 (transferencia internacional).',
    },
    {
      nombre: 'Tev1 (Together AI)',
      donde: 'Pesos abiertos de 4B corriendo en este equipo (llama.cpp). Probabilidades desde los logprobs.',
      datos: 'Ningún dato sale de SuruWorks.',
    },
    {
      nombre: 'LLM generativo',
      donde: 'Ollama local en la demo; en producción podría ser Azure OpenAI en la suscripción de SuruWorks.',
      datos: 'Local: nada sale. Azure OpenAI: queda en el tenant de SuruWorks. La confianza es autodeclarada, no calibrada.',
    },
    { nombre: 'Reglas', donde: 'Palabras clave dentro de la API. Línea base sin IA.', datos: 'Ningún dato sale de SuruWorks.' },
  ];
}

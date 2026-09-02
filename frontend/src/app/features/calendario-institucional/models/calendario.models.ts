import type { PciCalendarEvent, PciCalendarEventVariant } from '@davillawitte/pci-design-system';

import type {
  MarcacaoItem,
  OrigemMarcacao,
  StatusCalendarioAno,
  TipoMarcacaoCalendario,
} from '../services/calendario-api.service';

export const TIPO_MARCACAO_OPTIONS: { label: string; value: TipoMarcacaoCalendario }[] = [
  { label: 'Feriado nacional', value: 'FeriadoNacional' },
  { label: 'Feriado estadual', value: 'FeriadoEstadual' },
  { label: 'Feriado municipal', value: 'FeriadoMunicipal' },
  { label: 'Ponto facultativo', value: 'PontoFacultativo' },
  { label: 'Evento institucional', value: 'EventoInstitucional' },
];

export function tipoMarcacaoLabel(tipo: TipoMarcacaoCalendario): string {
  return TIPO_MARCACAO_OPTIONS.find((t) => t.value === tipo)?.label ?? tipo;
}

export function origemLabel(origem: OrigemMarcacao): string {
  switch (origem) {
    case 'Fixo':
      return 'Fixo';
    case 'Calculado':
      return 'Calculado';
    case 'Manual':
      return 'Manual';
    default:
      return origem;
  }
}

export function statusCalendarioLabel(status: StatusCalendarioAno): string {
  return status === 'Publicado' ? 'Publicado' : 'Rascunho';
}

export function statusCalendarioVariant(status: StatusCalendarioAno): 'success' | 'warning' {
  return status === 'Publicado' ? 'success' : 'warning';
}

export function tipoMarcacaoVariant(
  tipo: TipoMarcacaoCalendario,
): 'primary' | 'secondary' | 'warning' {
  if (tipo === 'EventoInstitucional') return 'primary';
  if (tipo === 'PontoFacultativo') return 'warning';
  return 'secondary';
}

export function isEvento(tipo: TipoMarcacaoCalendario): boolean {
  return tipo === 'EventoInstitucional';
}

export function tipoMarcacaoCalendarVariant(tipo: TipoMarcacaoCalendario): PciCalendarEventVariant {
  switch (tipo) {
    case 'EventoInstitucional':
      return 'primary';
    case 'PontoFacultativo':
      return 'warning';
    case 'FeriadoMunicipal':
      return 'accent';
    case 'FeriadoEstadual':
      return 'info';
    case 'FeriadoNacional':
    default:
      return 'success';
  }
}

export function marcacaoToCalendarEvent(m: MarcacaoItem): PciCalendarEvent {
  return {
    date: m.data.slice(0, 10),
    label: m.nome,
    description: m.local ?? undefined,
    variant: tipoMarcacaoCalendarVariant(m.tipo),
    meta: `${tipoMarcacaoLabel(m.tipo)} · ${origemLabel(m.origem)}`,
  };
}

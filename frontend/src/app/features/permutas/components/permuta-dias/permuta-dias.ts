import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import {
  PciAlertComponent,
  PciBadgeComponent,
  PciIconButtonComponent,
  PciIconComponent,
  PciSpinnerComponent,
} from '@davillawitte/pci-design-system';
import type { PciBadgeVariant } from '@davillawitte/pci-design-system';

import type { PermutaDia, PermutaEscalaServidor } from '../../models/permutas.models';

const MESES = [
  'Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho',
  'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro',
];
const DIAS_ABREV = ['Dom', 'Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb'];
const DIAS_NOME = [
  'Domingo', 'Segunda-feira', 'Terça-feira', 'Quarta-feira', 'Quinta-feira', 'Sexta-feira', 'Sábado',
];

const AFASTAMENTO = new Set(['FR', 'LP', 'LM', 'LO']);
const FOLGA = new Set(['D', 'F', 'R']);
const EXPEDIENTE = new Set(['M', 'T']);

function diaDaSemana(iso: string): number {
  const [y, m, d] = iso.split('-').map(Number);
  return new Date(y, m - 1, d).getDay();
}

/**
 * Escala de um servidor no mês, em tabela clicável (seleção única) — escolhe o dia da permuta.
 * Implementado aqui porque o design system não tem lista/tabela de seleção única: o
 * `pci-selection-list` é de múltipla escolha com checkbox (ver docs/design-system-pendencias.md).
 */
@Component({
  selector: 'app-permuta-dias',
  imports: [PciAlertComponent, PciBadgeComponent, PciIconButtonComponent, PciIconComponent, PciSpinnerComponent],
  templateUrl: './permuta-dias.html',
  styleUrl: './permuta-dias.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PermutaDias {
  /** Ex.: "Escala do solicitante". */
  readonly titulo = input.required<string>();
  readonly ano = input.required<number>();
  readonly mes = input.required<number>();
  readonly escala = input<PermutaEscalaServidor | null>(null);
  readonly loading = input(false);
  /** Servidor ainda não escolhido: mostra só o aviso. */
  readonly semServidor = input(false);
  /** Data selecionada (ISO). */
  readonly selecionada = input<string | null>(null);

  readonly diaClicado = output<string>();
  readonly mesAlterado = output<{ ano: number; mes: number }>();

  readonly subtitulo = computed(() => {
    const mes = `${MESES[this.mes() - 1]}/${this.ano()}`;
    const lotacao = this.escala()?.lotacaoNome;
    return lotacao ? `${mes} • ${lotacao}` : mes;
  });
  readonly temEscala = computed(() => !!this.escala()?.escalaId);
  readonly dias = computed(() => this.escala()?.dias ?? []);
  readonly temPermuta = computed(() => this.dias().some((d) => d.permuta));

  dataCurta(dia: PermutaDia): string {
    const [, m, d] = dia.data.split('-');
    return `${DIAS_ABREV[diaDaSemana(dia.data)]} ${d}/${m}`;
  }

  nomeDiaSemana(dia: PermutaDia): string {
    return DIAS_NOME[diaDaSemana(dia.data)];
  }

  fimDeSemana(dia: PermutaDia): boolean {
    const semana = diaDaSemana(dia.data);
    return semana === 0 || semana === 6;
  }

  variante(codigo: string | null | undefined): PciBadgeVariant {
    const c = (codigo ?? '').toUpperCase();
    if (EXPEDIENTE.has(c)) return 'success';
    if (c.startsWith('TL')) return 'info';
    if (FOLGA.has(c)) return 'secondary';
    if (AFASTAMENTO.has(c)) return 'warning';
    return 'primary';
  }

  mudarMes(delta: number): void {
    const data = new Date(this.ano(), this.mes() - 1 + delta, 1);
    this.mesAlterado.emit({ ano: data.getFullYear(), mes: data.getMonth() + 1 });
  }
}

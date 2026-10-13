import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { PciAlertComponent, PciIconButtonComponent, PciSpinnerComponent } from '@davillawitte/pci-design-system';

import type { PermutaDia, PermutaEscalaServidor } from '../../models/permutas.models';

const MESES = [
  'Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho',
  'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro',
];
const DIAS_SEMANA = ['Dom', 'Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb'];

/**
 * Dias da escala de um servidor no mês, em lista clicável (seleção única) — escolhe o dia da
 * permuta. Implementado aqui porque o `pci-selection-list` do design system é de múltipla escolha
 * com checkbox (ver docs/design-system-pendencias.md).
 */
@Component({
  selector: 'app-permuta-dias',
  imports: [PciAlertComponent, PciIconButtonComponent, PciSpinnerComponent],
  templateUrl: './permuta-dias.html',
  styleUrl: './permuta-dias.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PermutaDias {
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

  readonly titulo = computed(() => `${MESES[this.mes() - 1]}/${this.ano()}`);
  readonly temEscala = computed(() => !!this.escala()?.escalaId);
  readonly dias = computed(() => this.escala()?.dias ?? []);
  readonly temPermuta = computed(() => this.dias().some((d) => d.permuta));

  rotulo(dia: PermutaDia): string {
    const [y, m, d] = dia.data.split('-').map(Number);
    const semana = DIAS_SEMANA[new Date(y, m - 1, d).getDay()];
    return `${semana} ${String(d).padStart(2, '0')}/${String(m).padStart(2, '0')}`;
  }

  fimDeSemana(dia: PermutaDia): boolean {
    const [y, m, d] = dia.data.split('-').map(Number);
    const semana = new Date(y, m - 1, d).getDay();
    return semana === 0 || semana === 6;
  }

  mudarMes(delta: number): void {
    const data = new Date(this.ano(), this.mes() - 1 + delta, 1);
    this.mesAlterado.emit({ ano: data.getFullYear(), mes: data.getMonth() + 1 });
  }
}

import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import {
  PciAlertComponent,
  PciBadgeComponent,
  PciButtonComponent,
  PciFormCardComponent,
  PciIconComponent,
  PciFormActionsComponent,
  PciSpinnerComponent,
} from '@davillawitte/pci-design-system';

import { AppDialogHeaderComponent } from '../../../../shared/dialogs/dialog-header/dialog-header';
import { maskCpf, maskMatricula, maskTelefone } from '../../../../shared/input-masks';
import type { ServidorListItem, StatusServidor } from '../../models/admin.models';
import { AdminApiService } from '../../services/admin-api.service';

export interface ServidorDetailDialogData {
  id: string;
  /** Nome já conhecido pela listagem — evita cabeçalho vazio enquanto o detalhe carrega. */
  nome?: string;
  /** Libera o atalho "Editar" no rodapé; a listagem decide conforme a permissão da linha. */
  podeEditar?: boolean;
}

/** Fechou pedindo pra editar o servidor (a listagem é quem navega) ou só fechou. */
export type ServidorDetailDialogResult = 'editar' | null;

const STATUS_VARIANTES: Record<StatusServidor, 'success' | 'warning' | 'info' | 'outline'> = {
  Ativo: 'success',
  Afastado: 'warning',
  Cedido: 'info',
  Aposentado: 'outline',
};

/** Código do cargo "Outros" no catálogo — espelha `CargoCodes.Outros` no backend. */
const CARGO_CODIGO_OUTROS = 'OUTROS';

/** Detalhes do servidor em leitura, abertos pelo olho da listagem — evita entrar na tela de
 * edição só pra conferir dados. Busca `GET /api/servidores/{id}`, que traz o CPF (a listagem
 * não traz, por ser PII). */
@Component({
  selector: 'app-servidor-detail-dialog',
  imports: [
    CommonModule,
    MatDialogModule,
    PciAlertComponent,
    PciBadgeComponent,
    PciButtonComponent,
    PciFormActionsComponent,
    PciFormCardComponent,
    PciIconComponent,
    PciSpinnerComponent,
    AppDialogHeaderComponent,
  ],
  templateUrl: './servidor-detail-dialog.html',
  styleUrl: './servidor-detail-dialog.scss',
})
export class ServidorDetailDialog implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly ref =
    inject(MatDialogRef<ServidorDetailDialog, ServidorDetailDialogResult>);
  readonly data = inject<ServidorDetailDialogData>(MAT_DIALOG_DATA);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly servidor = signal<ServidorListItem | null>(null);

  readonly titulo = computed(() => this.servidor()?.nome ?? this.data.nome ?? 'Servidor');

  readonly cargo = computed(() => {
    const s = this.servidor();
    if (!s) {
      return '—';
    }
    return s.cargoCodigo === CARGO_CODIGO_OUTROS && s.cargoOutroTexto
      ? `${s.cargo} — ${s.cargoOutroTexto}`
      : s.cargo;
  });

  readonly lotacao = computed(() => {
    const s = this.servidor();
    if (!s) {
      return '—';
    }
    if (s.setorNome) {
      return s.setorNome;
    }
    return s.nucleoNome ? `${s.nucleoNome} (núcleo)` : '—';
  });

  readonly usuario = computed(() => {
    const s = this.servidor();
    if (!s?.possuiUsuario) {
      return 'Sem usuário';
    }
    return s.usuarioAtivo ? 'Usuário ativo' : 'Usuário inativo';
  });

  readonly statusVariante = computed(() => {
    const status = this.servidor()?.status;
    return status ? STATUS_VARIANTES[status] : 'outline';
  });

  ngOnInit(): void {
    this.api.getServidor(this.data.id).subscribe({
      next: (servidor) => {
        this.servidor.set(servidor);
        this.loading.set(false);
      },
      error: (err: { error?: { message?: string } }) => {
        this.error.set(err.error?.message ?? 'Não foi possível carregar o servidor.');
        this.loading.set(false);
      },
    });
  }

  matricula(valor: string): string {
    return valor ? maskMatricula(valor) : '—';
  }

  cpf(valor: string): string {
    return valor ? maskCpf(valor) : '—';
  }

  telefone(valor: string | null | undefined): string {
    return valor ? maskTelefone(valor) : '—';
  }

  /** `dataNascimento` vem como `YYYY-MM-DD`; formatar na mão evita o deslocamento de fuso
   * que o pipe `date` causa ao interpretar a string como UTC. */
  dataNascimento(valor: string | null | undefined): string {
    const partes = (valor ?? '').slice(0, 10).split('-');
    return partes.length === 3 ? `${partes[2]}/${partes[1]}/${partes[0]}` : '—';
  }

  texto(valor: string | null | undefined): string {
    return valor?.trim() ? valor : '—';
  }

  editar(): void {
    this.ref.close('editar');
  }

  fechar(): void {
    this.ref.close(null);
  }
}

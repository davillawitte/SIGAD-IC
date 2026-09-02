import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import {
  PciAlertComponent,
  PciButtonComponent,
  PciFormActionsComponent,
  PciFormCardComponent,
  PciIconComponent,
  PciSelectionListComponent,
} from '@davillawitte/pci-design-system';
import type { PciSelectionListItem } from '@davillawitte/pci-design-system';

import { AdminApiService } from '../../services/admin-api.service';
import type { PerfilListItem, UsuarioComSenha } from '../../models/admin.models';
import { AppFormColDirective, AppFormSectionComponent } from '../../../../shared/form-layout';
import { AppDialogHeaderComponent } from '../../../../shared/dialogs/dialog-header/dialog-header';

export interface UsuarioDialogData {
  servidorId: string;
  servidorLabel: string;
}

/** Versão só-criação de `usuario-form` embutida em `MatDialog`, com o servidor sempre fixo
 * (definido por quem abre — hoje, `servidor-form` logo após cadastrar um servidor). Mesmo
 * padrão de `AfastamentoDialog`/`ServidorDialog` — sem `Router`, fecha com o usuário criado ou
 * `false`. Mantém o mesmo fluxo de duas etapas da página: mostra a senha temporária uma única
 * vez antes de permitir fechar. */
@Component({
  selector: 'app-usuario-dialog',
  imports: [
    CommonModule,
    MatDialogModule,
    PciAlertComponent,
    PciButtonComponent,
    PciFormActionsComponent,
    PciFormCardComponent,
    PciIconComponent,
    PciSelectionListComponent,
    AppFormSectionComponent,
    AppFormColDirective,
    AppDialogHeaderComponent,
  ],
  templateUrl: './usuario-dialog.html',
})
export class UsuarioDialog implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly dialogRef = inject(MatDialogRef<UsuarioDialog, UsuarioComSenha | false>);
  readonly data = inject<UsuarioDialogData>(MAT_DIALOG_DATA);

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly perfis = signal<PerfilListItem[]>([]);
  readonly selectedPerfilIds = signal<string[]>([]);
  readonly created = signal<UsuarioComSenha | null>(null);

  readonly perfilItems = computed<PciSelectionListItem[]>(() =>
    this.perfis().map((perfil) => ({ id: perfil.id, label: perfil.nome })),
  );

  ngOnInit(): void {
    this.api.listPerfis({ page: 1, pageSize: 100 }).subscribe({
      next: (result) => this.perfis.set(result.items.filter((p) => p.ativo)),
    });
  }

  save(): void {
    if (this.selectedPerfilIds().length === 0) {
      this.error.set('Selecione ao menos um perfil.');
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    this.api
      .createUsuario({
        servidorId: this.data.servidorId,
        perfilIds: this.selectedPerfilIds(),
      })
      .subscribe({
        next: (created) => {
          this.created.set(created);
          this.saving.set(false);
        },
        error: (err: { error?: { message?: string } }) => {
          this.error.set(err.error?.message ?? 'Falha ao criar usuário.');
          this.saving.set(false);
        },
      });
  }

  concluir(): void {
    this.dialogRef.close(this.created()!);
  }

  cancel(): void {
    this.dialogRef.close(false);
  }

  close(): void {
    this.dialogRef.close(this.created() ?? false);
  }
}

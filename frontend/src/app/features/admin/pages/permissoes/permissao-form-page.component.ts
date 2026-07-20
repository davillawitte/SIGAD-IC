import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import {
  PciAlertComponent,
  PciButtonComponent,
  PciCardComponent,
  PciCardContentComponent,
  PciCardDescriptionComponent,
  PciCardHeaderComponent,
  PciCardTitleComponent,
  PciInputComponent,
  PciStackComponent,
} from '@davillawitte/pci-design-system';

import { AdminApiService } from '../../services/admin-api.service';

@Component({
  selector: 'app-permissao-form-page',
  imports: [
    CommonModule,
    FormsModule,
    PciAlertComponent,
    PciButtonComponent,
    PciCardComponent,
    PciCardContentComponent,
    PciCardDescriptionComponent,
    PciCardHeaderComponent,
    PciCardTitleComponent,
    PciInputComponent,
    PciStackComponent,
  ],
  template: `
    <section class="page">
      <pci-card>
        <pci-card-header>
          <pci-card-title>{{ isEdit() ? 'Editar permissão' : 'Nova permissão' }}</pci-card-title>
          <pci-card-description>
            Permissões controlam o acesso às operações do sistema.
          </pci-card-description>
        </pci-card-header>
        <pci-card-content>
          <pci-stack gap="4" [fullWidth]="true">
            @if (error()) {
              <pci-alert variant="error" title="Erro" [message]="error()!" />
            }

            @if (!isEdit()) {
              <pci-input
                label="Código"
                [(ngModel)]="codigo"
                name="codigo"
                hint="Ex.: laudos.criar"
              />
            } @else {
              <p class="meta"><strong>Código:</strong> {{ codigo }}</p>
            }

            <pci-input label="Nome" [(ngModel)]="nome" name="nome" />
            <pci-input label="Módulo" [(ngModel)]="modulo" name="modulo" hint="Ex.: laudos" />
            <pci-input label="Descrição" [(ngModel)]="descricao" name="descricao" />

            <pci-stack direction="horizontal" gap="3">
              <pci-button
                variant="primary"
                icon="save"
                [loading]="saving()"
                [disabled]="!nome.trim() || !modulo.trim() || (!isEdit() && !codigo.trim())"
                (clicked)="save()"
              >
                Salvar
              </pci-button>
              @if (isEdit() && !isSistema()) {
                <pci-button variant="danger" icon="trash" [loading]="saving()" (clicked)="remove()">
                  Desativar
                </pci-button>
              }
              <pci-button variant="ghost" (clicked)="cancel()">Cancelar</pci-button>
            </pci-stack>
          </pci-stack>
        </pci-card-content>
      </pci-card>
    </section>
  `,
  styles: `
    .meta {
      margin: 0;
      font-size: 0.875rem;
      color: var(--pci-color-text-secondary, #6b7280);
    }
  `,
})
export class PermissaoFormPageComponent implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly isEdit = signal(false);
  readonly isSistema = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  private editId: string | null = null;
  codigo = '';
  nome = '';
  modulo = '';
  descricao = '';

  ngOnInit(): void {
    this.editId = this.route.snapshot.paramMap.get('id');
    this.isEdit.set(!!this.editId);

    if (this.editId) {
      this.api.getPermissao(this.editId).subscribe({
        next: (item) => {
          this.codigo = item.codigo;
          this.nome = item.nome;
          this.modulo = item.modulo;
          this.descricao = item.descricao ?? '';
          this.isSistema.set(item.sistema);
        },
        error: () => this.error.set('Não foi possível carregar a permissão.'),
      });
    }
  }

  save(): void {
    this.saving.set(true);
    this.error.set(null);

    if (!this.isEdit()) {
      this.api
        .createPermissao({
          codigo: this.codigo.trim(),
          nome: this.nome.trim(),
          modulo: this.modulo.trim(),
          descricao: this.descricao.trim() || null,
        })
        .subscribe({
          next: () => void this.router.navigateByUrl('/permissoes'),
          error: (err: { error?: { message?: string } }) => this.fail(err.error?.message),
        });
      return;
    }

    this.api
      .updatePermissao(this.editId!, {
        nome: this.nome.trim(),
        modulo: this.modulo.trim(),
        descricao: this.descricao.trim() || null,
      })
      .subscribe({
        next: () => void this.router.navigateByUrl('/permissoes'),
        error: (err: { error?: { message?: string } }) => this.fail(err.error?.message),
      });
  }

  remove(): void {
    if (!this.editId) {
      return;
    }

    this.saving.set(true);
    this.api.deletePermissao(this.editId).subscribe({
      next: () => void this.router.navigateByUrl('/permissoes'),
      error: (err: { error?: { message?: string } }) => this.fail(err.error?.message),
    });
  }

  cancel(): void {
    void this.router.navigateByUrl('/permissoes');
  }

  private fail(message?: string): void {
    this.error.set(message ?? 'Operação não concluída.');
    this.saving.set(false);
  }
}

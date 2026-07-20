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
  PciCheckboxComponent,
  PciInputComponent,
  PciStackComponent,
} from '@davillawitte/pci-design-system';

import { AdminApiService } from '../../services/admin-api.service';
import { PermissaoItem } from '../../models/admin.models';

@Component({
  selector: 'app-perfil-form-page',
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
    PciCheckboxComponent,
    PciInputComponent,
    PciStackComponent,
  ],
  template: `
    <section class="page">
      <pci-card>
        <pci-card-header>
          <pci-card-title>{{ isEdit() ? 'Editar perfil' : 'Novo perfil' }}</pci-card-title>
          <pci-card-description>
            Defina o perfil e associe as permissões disponíveis.
          </pci-card-description>
        </pci-card-header>
        <pci-card-content>
          <pci-stack gap="4" [fullWidth]="true">
            @if (error()) {
              <pci-alert variant="error" title="Erro" [message]="error()!" />
            }

            <pci-input label="Nome" [(ngModel)]="nome" name="nome" />
            @if (!isEdit()) {
              <pci-input label="Código" [(ngModel)]="codigo" name="codigo" hint="Ex.: ANALISTA" />
            } @else {
              <p class="meta"><strong>Código:</strong> {{ codigo }}</p>
            }
            <pci-input label="Descrição" [(ngModel)]="descricao" name="descricao" />

            <div class="checks">
              <strong>Permissões</strong>
              @for (permissao of permissoes(); track permissao.id) {
                <pci-checkbox
                  [label]="permissao.codigo + ' — ' + permissao.nome"
                  [checked]="selectedPermissaoIds().includes(permissao.id)"
                  (changed)="togglePermissao(permissao.id, $event)"
                />
              }
            </div>

            <pci-stack direction="horizontal" gap="3">
              <pci-button
                variant="primary"
                icon="save"
                [loading]="saving()"
                [disabled]="!nome.trim() || (!isEdit() && !codigo.trim())"
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
    .checks {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      max-height: 20rem;
      overflow: auto;
    }

    .meta {
      margin: 0;
      font-size: 0.875rem;
      color: var(--pci-color-text-secondary, #6b7280);
    }
  `,
})
export class PerfilFormPageComponent implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly isEdit = signal(false);
  readonly isSistema = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly permissoes = signal<PermissaoItem[]>([]);
  readonly selectedPermissaoIds = signal<string[]>([]);

  private editId: string | null = null;
  nome = '';
  codigo = '';
  descricao = '';

  ngOnInit(): void {
    this.editId = this.route.snapshot.paramMap.get('id');
    this.isEdit.set(!!this.editId);

    this.api.listPermissoes({ page: 1, pageSize: 100 }).subscribe({
      next: (result) => this.permissoes.set(result.items.filter((p) => p.ativo)),
    });

    if (this.editId) {
      this.api.getPerfil(this.editId).subscribe({
        next: (perfil) => {
          this.nome = perfil.nome;
          this.codigo = perfil.codigo;
          this.descricao = perfil.descricao ?? '';
          this.isSistema.set(perfil.sistema);
          this.selectedPermissaoIds.set(perfil.permissaoIds ?? []);
        },
        error: () => this.error.set('Não foi possível carregar o perfil.'),
      });
    }
  }

  togglePermissao(id: string, checked: boolean): void {
    const current = new Set(this.selectedPermissaoIds());
    if (checked) {
      current.add(id);
    } else {
      current.delete(id);
    }
    this.selectedPermissaoIds.set([...current]);
  }

  save(): void {
    this.saving.set(true);
    this.error.set(null);

    if (!this.isEdit()) {
      this.api
        .createPerfil({
          nome: this.nome.trim(),
          codigo: this.codigo.trim(),
          descricao: this.descricao.trim() || null,
          permissaoIds: this.selectedPermissaoIds(),
        })
        .subscribe({
          next: () => void this.router.navigateByUrl('/perfis'),
          error: (err: { error?: { message?: string } }) => this.fail(err.error?.message),
        });
      return;
    }

    const id = this.editId!;
    this.api
      .updatePerfil(id, {
        nome: this.nome.trim(),
        descricao: this.descricao.trim() || null,
      })
      .subscribe({
        next: () => {
          this.api.setPerfilPermissoes(id, this.selectedPermissaoIds()).subscribe({
            next: () => void this.router.navigateByUrl('/perfis'),
            error: (err: { error?: { message?: string } }) => this.fail(err.error?.message),
          });
        },
        error: (err: { error?: { message?: string } }) => this.fail(err.error?.message),
      });
  }

  remove(): void {
    if (!this.editId) {
      return;
    }

    this.saving.set(true);
    this.api.deletePerfil(this.editId).subscribe({
      next: () => void this.router.navigateByUrl('/perfis'),
      error: (err: { error?: { message?: string } }) => this.fail(err.error?.message),
    });
  }

  cancel(): void {
    void this.router.navigateByUrl('/perfis');
  }

  private fail(message?: string): void {
    this.error.set(message ?? 'Operação não concluída.');
    this.saving.set(false);
  }
}

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
import { PerfilListItem, ServidorListItem, SetorListItem } from '../../models/admin.models';

@Component({
  selector: 'app-usuario-form-page',
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
          <pci-card-title>{{ isEdit() ? 'Editar usuário' : 'Novo usuário' }}</pci-card-title>
          <pci-card-description>
            {{
              isEdit()
                ? 'Atualize perfis e status do usuário.'
                : 'Todo usuário precisa estar vinculado a um servidor sem login.'
            }}
          </pci-card-description>
        </pci-card-header>
        <pci-card-content>
          <pci-stack gap="4" [fullWidth]="true">
            @if (error()) {
              <pci-alert variant="error" title="Erro" [message]="error()!" />
            }

            @if (!isEdit()) {
              <label class="field">
                <span>Servidor</span>
                <select [(ngModel)]="servidorId" name="servidorId">
                  <option value="">Selecione um servidor</option>
                  @for (servidor of servidores(); track servidor.id) {
                    <option [value]="servidor.id">
                      {{ servidor.nome }} — {{ servidor.matricula }}
                    </option>
                  }
                </select>
              </label>

              @if (servidores().length === 0) {
                <pci-alert
                  variant="info"
                  title="Nenhum servidor disponível"
                  message="Cadastre um servidor abaixo para liberar a criação de usuário."
                />

                <pci-stack gap="3" [fullWidth]="true">
                  <pci-input label="Nome do servidor" [(ngModel)]="novoServidor.nome" name="srvNome" />
                  <pci-input label="Matrícula" [(ngModel)]="novoServidor.matricula" name="srvMatricula" />
                  <pci-input label="CPF" [(ngModel)]="novoServidor.cpf" name="srvCpf" />
                  <pci-input label="Cargo" [(ngModel)]="novoServidor.cargo" name="srvCargo" />
                  <pci-input label="E-mail" [(ngModel)]="novoServidor.email" name="srvEmail" />
                  <label class="field">
                    <span>Setor</span>
                    <select [(ngModel)]="novoServidor.setorId" name="srvSetor">
                      <option value="">Selecione</option>
                      @for (setor of setores(); track setor.id) {
                        <option [value]="setor.id">{{ setor.sigla }} — {{ setor.nome }}</option>
                      }
                    </select>
                  </label>
                  <pci-button
                    variant="secondary"
                    icon="user-plus"
                    [loading]="savingServidor()"
                    (clicked)="createServidor()"
                  >
                    Cadastrar servidor
                  </pci-button>
                </pci-stack>
              }

              <pci-input label="Login" [(ngModel)]="login" name="login" />
              <pci-input label="Senha" type="password" [(ngModel)]="senha" name="senha" />
            } @else {
              <p class="meta"><strong>Login:</strong> {{ login }}</p>
              <p class="meta"><strong>Servidor:</strong> {{ nomeServidor }}</p>

              <pci-checkbox
                label="Bloqueado"
                [checked]="bloqueado"
                (changed)="bloqueado = $event"
              />
              <pci-checkbox label="Ativo" [checked]="ativo" (changed)="ativo = $event" />
            }

            <div class="checks">
              <strong>Perfis</strong>
              @for (perfil of perfis(); track perfil.id) {
                <pci-checkbox
                  [label]="perfil.nome + ' (' + perfil.codigo + ')'"
                  [checked]="selectedPerfilIds().includes(perfil.id)"
                  (changed)="togglePerfil(perfil.id, $event)"
                />
              }
            </div>

            <pci-stack direction="horizontal" gap="3">
              <pci-button
                variant="primary"
                icon="save"
                [loading]="saving()"
                [disabled]="!canSave()"
                (clicked)="save()"
              >
                Salvar
              </pci-button>
              <pci-button variant="ghost" (clicked)="cancel()">Cancelar</pci-button>
            </pci-stack>
          </pci-stack>
        </pci-card-content>
      </pci-card>
    </section>
  `,
  styles: `
    .field {
      display: flex;
      flex-direction: column;
      gap: 0.35rem;
      font-size: 0.875rem;
    }

    .field span {
      color: var(--pci-color-text-secondary, #6b7280);
      font-weight: 500;
    }

    select {
      height: 2.5rem;
      border: 1px solid var(--pci-color-border, #e5e7eb);
      border-radius: var(--pci-radius-md, 0.5rem);
      padding: 0 0.75rem;
      background: #fff;
    }

    .checks {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .meta {
      margin: 0;
      font-size: 0.875rem;
      color: var(--pci-color-text-secondary, #6b7280);
    }
  `,
})
export class UsuarioFormPageComponent implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly isEdit = signal(false);
  readonly saving = signal(false);
  readonly savingServidor = signal(false);
  readonly error = signal<string | null>(null);
  readonly servidores = signal<ServidorListItem[]>([]);
  readonly setores = signal<SetorListItem[]>([]);
  readonly perfis = signal<PerfilListItem[]>([]);
  readonly selectedPerfilIds = signal<string[]>([]);

  private editId: string | null = null;
  servidorId = '';
  login = '';
  senha = '';
  nomeServidor = '';
  bloqueado = false;
  ativo = true;
  novoServidor = {
    nome: '',
    matricula: '',
    cpf: '',
    cargo: '',
    email: '',
    setorId: '',
  };

  ngOnInit(): void {
    this.editId = this.route.snapshot.paramMap.get('id');
    this.isEdit.set(!!this.editId);
    this.loadLookups();

    if (this.editId) {
      this.api.getUsuario(this.editId).subscribe({
        next: (usuario) => {
          this.login = usuario.login;
          this.nomeServidor = usuario.nomeServidor;
          this.bloqueado = usuario.bloqueado;
          this.ativo = usuario.ativo;
          this.selectedPerfilIds.set(usuario.perfilIds ?? []);
        },
        error: () => this.error.set('Não foi possível carregar o usuário.'),
      });
    }
  }

  canSave(): boolean {
    if (this.selectedPerfilIds().length === 0) {
      return false;
    }

    if (this.isEdit()) {
      return true;
    }

    return !!this.servidorId && !!this.login.trim() && this.senha.length >= 8;
  }

  togglePerfil(id: string, checked: boolean): void {
    const current = new Set(this.selectedPerfilIds());
    if (checked) {
      current.add(id);
    } else {
      current.delete(id);
    }
    this.selectedPerfilIds.set([...current]);
  }

  save(): void {
    if (!this.canSave()) {
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    if (this.isEdit() && this.editId) {
      this.api
        .updateUsuario(this.editId, {
          perfilIds: this.selectedPerfilIds(),
          bloqueado: this.bloqueado,
          ativo: this.ativo,
        })
        .subscribe({
          next: () => void this.router.navigateByUrl('/usuarios'),
          error: (err: { error?: { message?: string } }) => {
            this.error.set(err.error?.message ?? 'Falha ao atualizar usuário.');
            this.saving.set(false);
          },
        });
      return;
    }

    this.api
      .createUsuario({
        servidorId: this.servidorId,
        login: this.login.trim(),
        senha: this.senha,
        perfilIds: this.selectedPerfilIds(),
      })
      .subscribe({
        next: () => void this.router.navigateByUrl('/usuarios'),
        error: (err: { error?: { message?: string } }) => {
          this.error.set(err.error?.message ?? 'Falha ao criar usuário.');
          this.saving.set(false);
        },
      });
  }

  createServidor(): void {
    const payload = this.novoServidor;
    if (!payload.nome || !payload.matricula || !payload.cpf || !payload.cargo || !payload.email || !payload.setorId) {
      this.error.set('Preencha todos os campos do servidor.');
      return;
    }

    this.savingServidor.set(true);
    this.api.createServidor(payload).subscribe({
      next: (servidor) => {
        this.savingServidor.set(false);
        this.servidorId = servidor.id;
        this.loadLookups();
      },
      error: (err: { error?: { message?: string } }) => {
        this.error.set(err.error?.message ?? 'Falha ao cadastrar servidor.');
        this.savingServidor.set(false);
      },
    });
  }

  cancel(): void {
    void this.router.navigateByUrl('/usuarios');
  }

  private loadLookups(): void {
    this.api.listServidores(true).subscribe({
      next: (items) => this.servidores.set(items),
    });
    this.api.listSetores().subscribe({
      next: (items) => this.setores.set(items),
    });
    this.api.listPerfis({ page: 1, pageSize: 100 }).subscribe({
      next: (result) => this.perfis.set(result.items.filter((p) => p.ativo)),
    });
  }
}

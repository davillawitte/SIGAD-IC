import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatSelectModule } from '@angular/material/select';
import { ActivatedRoute, Router } from '@angular/router';
import {
  PciAlertComponent,
  PciButtonComponent,
  PciFormPageComponent,
  PciInputComponent,
  PciSelectionListComponent,
  PciStackComponent,
} from '@davillawitte/pci-design-system';
import type { PciSelectOption, PciSelectionListItem } from '@davillawitte/pci-design-system';

import { ADMIN_ROUTE_PAGES } from '../../admin-route-pages';
import { AdminApiService } from '../../services/admin-api.service';
import type { PerfilListItem, PermissaoItem } from '../../models/admin.models';
import { AppFormColDirective, AppFormSectionComponent } from '../../../../shared/form-layout';

const SUPER_ADMIN_CODIGO = 'SUPERADMINISTRADOR';
const AREA_ORDER = [
  'Gestão Institucional',
  'Gestão do Setor',
  'Administração do Sistema',
] as const;

@Component({
  selector: 'app-perfil-form-page',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatSelectModule,
    PciAlertComponent,
    PciButtonComponent,
    PciFormPageComponent,
    PciInputComponent,
    PciSelectionListComponent,
    PciStackComponent,
    AppFormSectionComponent,
    AppFormColDirective,
  ],
  templateUrl: './perfil-form-page.component.html',
  styleUrl: './perfil-form-page.component.scss',
})
export class PerfilFormPageComponent implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly routePages = ADMIN_ROUTE_PAGES;
  readonly isEdit = signal(false);
  readonly isSistema = signal(false);
  readonly isSuperAdmin = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly permissoes = signal<PermissaoItem[]>([]);
  readonly selectedPermissaoIds = signal<string[]>([]);
  readonly currentPath = signal('/perfis/novo');

  readonly desativarConfirmado = signal(false);
  readonly quantidadeUsuarios = signal(0);
  readonly temUsuariosVinculados = signal(false);
  readonly modoDesativacao = signal<'substituir' | 'remover' | null>(null);
  readonly perfilSubstitutoId = signal<string | null>(null);
  readonly perfisAtivos = signal<PerfilListItem[]>([]);

  readonly form = this.fb.nonNullable.group({
    nome: ['', Validators.required],
    descricao: [''],
  });

  readonly permissoesPorArea = computed(() => {
    const groups = new Map<string, PermissaoItem[]>();
    for (const permissao of this.permissoes()) {
      const area = permissao.area || 'Outros';
      const list = groups.get(area) ?? [];
      list.push(permissao);
      groups.set(area, list);
    }

    const ordered: { area: string; items: PciSelectionListItem[] }[] = AREA_ORDER.filter(
      (area) => groups.has(area),
    ).map((area) => ({
      area,
      items: (groups.get(area) ?? []).map(
        (permissao): PciSelectionListItem => ({
          id: permissao.id,
          label: `${permissao.nome} (${permissao.codigo})`,
        }),
      ),
    }));

    for (const [area, items] of groups.entries()) {
      if ((AREA_ORDER as readonly string[]).includes(area)) continue;
      ordered.push({
        area,
        items: items.map((permissao) => ({
          id: permissao.id,
          label: `${permissao.nome} (${permissao.codigo})`,
        })),
      });
    }

    return ordered;
  });

  readonly showDesativarPanel = computed(
    () => this.isEdit() && !this.isSistema() && !this.isSuperAdmin(),
  );

  readonly desativarMensagem = computed(() => {
    const qtd = this.quantidadeUsuarios();
    if (qtd === 0) {
      return 'Nenhuma conta está vinculada a este perfil. Confirme para desativá-lo.';
    }

    const conta = qtd === 1 ? '1 conta está vinculada' : `${qtd} contas estão vinculadas`;
    return `${conta} a este perfil. Você pode substituir por outro perfil ou remover o vínculo (usuários ficam sem perfil e só acessam o Início).`;
  });

  readonly substitutoOptions = computed<PciSelectOption[]>(() =>
    this.perfisAtivos()
      .filter((perfil) => perfil.id !== this.editId)
      .map((perfil) => ({
        value: perfil.id,
        label: perfil.nome,
      })),
  );

  private editId: string | null = null;

  ngOnInit(): void {
    this.editId = this.route.snapshot.paramMap.get('id');
    this.isEdit.set(!!this.editId);
    this.currentPath.set(this.editId ? '/perfis/editar/:id' : '/perfis/novo');

    this.api.listPermissoes({ page: 1, pageSize: 200 }).subscribe({
      next: (result) => this.permissoes.set(result.items.filter((p) => p.ativo)),
    });

    if (this.editId) {
      this.api.getPerfil(this.editId).subscribe({
        next: (perfil) => {
          this.form.patchValue({
            nome: perfil.nome,
            descricao: perfil.descricao ?? '',
          });
          this.isSistema.set(perfil.sistema);
          this.isSuperAdmin.set(perfil.codigo === SUPER_ADMIN_CODIGO);
          this.selectedPermissaoIds.set(perfil.permissaoIds ?? []);
        },
        error: () => this.error.set('Não foi possível carregar o perfil.'),
      });
    }
  }

  onPermissoesChange(ids: string[], areaItemIds: string[]): void {
    if (this.isSuperAdmin()) {
      return;
    }

    const kept = this.selectedPermissaoIds().filter((id) => !areaItemIds.includes(id));
    this.selectedPermissaoIds.set([...kept, ...ids]);
  }

  selectedIdsForArea(itemIds: string[]): string[] {
    const selected = new Set(this.selectedPermissaoIds());
    return itemIds.filter((id) => selected.has(id));
  }

  itemIds(items: PciSelectionListItem[]): string[] {
    return items.map((item) => item.id);
  }

  save(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.error.set('Preencha os campos obrigatórios antes de salvar.');
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    const value = this.form.getRawValue();

    if (!this.isEdit()) {
      this.api
        .createPerfil({
          nome: value.nome.trim(),
          descricao: value.descricao.trim() || null,
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
        nome: value.nome.trim(),
        descricao: value.descricao.trim() || null,
      })
      .subscribe({
        next: () => {
          if (this.isSuperAdmin()) {
            void this.router.navigateByUrl('/perfis');
            return;
          }

          this.api.setPerfilPermissoes(id, this.selectedPermissaoIds()).subscribe({
            next: () => void this.router.navigateByUrl('/perfis'),
            error: (err: { error?: { message?: string } }) => this.fail(err.error?.message),
          });
        },
        error: (err: { error?: { message?: string } }) => this.fail(err.error?.message),
      });
  }

  iniciarDesativacao(): void {
    if (!this.editId) {
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    this.api.getPerfilExclusaoImpacto(this.editId).subscribe({
      next: (impacto) => {
        this.quantidadeUsuarios.set(impacto.quantidadeUsuarios);
        this.temUsuariosVinculados.set(impacto.temUsuariosVinculados);
        this.perfilSubstitutoId.set(null);
        this.modoDesativacao.set(impacto.temUsuariosVinculados ? null : 'remover');
        this.desativarConfirmado.set(true);
        this.saving.set(false);

        if (impacto.temUsuariosVinculados) {
          this.api.listPerfis({ page: 1, pageSize: 100 }).subscribe({
            next: (result) =>
              this.perfisAtivos.set(result.items.filter((perfil) => perfil.ativo)),
          });
        }
      },
      error: (err: { error?: { message?: string } }) => this.fail(err.error?.message),
    });
  }

  cancelarDesativacao(): void {
    this.desativarConfirmado.set(false);
    this.temUsuariosVinculados.set(false);
    this.modoDesativacao.set(null);
    this.perfilSubstitutoId.set(null);
    this.error.set(null);
  }

  confirmarDesativacao(): void {
    if (!this.editId) {
      return;
    }

    if (this.temUsuariosVinculados()) {
      if (this.modoDesativacao() === 'substituir' && !this.perfilSubstitutoId()) {
        this.error.set('Selecione o perfil substituto para as contas vinculadas.');
        return;
      }

      if (!this.modoDesativacao()) {
        this.error.set('Escolha substituir o perfil ou remover os vínculos.');
        return;
      }
    }

    this.saving.set(true);
    this.error.set(null);
    this.api
      .desativarPerfil(this.editId, {
        perfilSubstitutoId:
          this.modoDesativacao() === 'substituir' ? this.perfilSubstitutoId() : null,
        removerVinculosSemSubstituto: this.modoDesativacao() === 'remover',
      })
      .subscribe({
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

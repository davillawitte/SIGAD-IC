import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import {
  FormBuilder,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import {
  PciAlertComponent,
  PciButtonComponent,
  PciCheckboxComponent,
  PciFormPageComponent,
  PciInputComponent,
  PciSelectComponent,
  PciStackComponent,
} from '@davillawitte/pci-design-system';
import type { PciSelectOption } from '@davillawitte/pci-design-system';

import { AuthService } from '../../../../core/auth/auth.service';
import { ADMIN_ROUTE_PAGES } from '../../admin-route-pages';
import { AdminApiService } from '../../services/admin-api.service';
import type {
  AbrangenciaModulo,
  PerfilListItem,
  PermissaoItem,
} from '../../models/admin.models';
import { AppFormColDirective, AppFormSectionComponent } from '../../../../shared/form-layout';

const SUPER_ADMIN_CODIGO = 'SUPERADMINISTRADOR';

type CrudAction = 'listar' | 'criar' | 'editar' | 'excluir';
type AbrangenciaSelect = 'MeusSetores' | 'TodosOsSetores';

interface MatrixExtraDef {
  codigo: string;
  label: string;
}

interface MatrixRowDef {
  key: string;
  label: string;
  modules: string[];
  crud: CrudAction[];
  extras: MatrixExtraDef[];
  hasAbrangencia: boolean;
  adminOnly?: boolean;
}

const CRUD_COLUMNS: { action: CrudAction; label: string }[] = [
  { action: 'listar', label: 'Ver' },
  { action: 'criar', label: 'Criar' },
  { action: 'editar', label: 'Editar' },
  { action: 'excluir', label: 'Excluir' },
];

const MATRIX_ROWS: MatrixRowDef[] = [
  {
    key: 'escalas',
    label: 'Escalas',
    modules: ['escalas'],
    crud: ['listar', 'criar', 'editar', 'excluir'],
    extras: [
      { codigo: 'escalas.publicar', label: 'Publicar' },
      { codigo: 'escalas.finalizar', label: 'Finalizar' },
      { codigo: 'escalas.exportar', label: 'Exportar' },
      { codigo: 'escalas.solicitar_devolucao', label: 'Solicitar devolução' },
      { codigo: 'escalas.devolver', label: 'Devolver' },
    ],
    hasAbrangencia: true,
  },
  {
    key: 'afastamentos',
    label: 'Afastamentos',
    modules: ['afastamentos'],
    crud: ['listar', 'criar', 'editar', 'excluir'],
    extras: [],
    hasAbrangencia: true,
  },
  {
    key: 'servidores',
    label: 'Servidores',
    modules: ['servidores'],
    crud: ['listar', 'criar', 'editar', 'excluir'],
    extras: [],
    hasAbrangencia: true,
  },
  {
    key: 'estrutura',
    label: 'Estrutura Organizacional',
    modules: ['nucleos', 'setores'],
    crud: ['listar', 'criar', 'editar', 'excluir'],
    extras: [],
    hasAbrangencia: true,
  },
  {
    key: 'cargos',
    label: 'Cargos',
    modules: ['cargos'],
    crud: ['listar'],
    extras: [],
    hasAbrangencia: false,
  },
  {
    key: 'usuarios',
    label: 'Usuários',
    modules: ['usuarios'],
    crud: ['listar', 'criar', 'editar'],
    extras: [{ codigo: 'usuarios.bloquear', label: 'Bloquear' }],
    hasAbrangencia: false,
    adminOnly: true,
  },
  {
    key: 'perfis',
    label: 'Perfis',
    modules: ['perfis'],
    crud: ['listar', 'criar', 'editar', 'excluir'],
    extras: [{ codigo: 'perfis.gerenciar_permissoes', label: 'Gerenciar permissões' }],
    hasAbrangencia: false,
    adminOnly: true,
  },
  {
    key: 'permissoes',
    label: 'Permissões',
    modules: ['permissoes'],
    crud: ['listar'],
    extras: [],
    hasAbrangencia: false,
    adminOnly: true,
  },
];

@Component({
  selector: 'app-perfil-form',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    PciAlertComponent,
    PciButtonComponent,
    PciCheckboxComponent,
    PciFormPageComponent,
    PciInputComponent,
    PciSelectComponent,
    PciStackComponent,
    AppFormSectionComponent,
    AppFormColDirective,
  ],
  templateUrl: './perfil-form.html',
  styleUrl: './perfil-form.scss',
})
export class PerfilForm implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly routePages = ADMIN_ROUTE_PAGES;
  readonly crudColumns = CRUD_COLUMNS;
  readonly isEdit = signal(false);
  readonly isSistema = signal(false);
  readonly isSuperAdminProfile = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly permissoes = signal<PermissaoItem[]>([]);
  readonly selectedPermissaoIds = signal<string[]>([]);
  readonly currentPath = signal('/perfis/novo');
  readonly currentUserIsSuperAdmin = this.auth.isSuperAdmin();

  readonly desativarConfirmado = signal(false);
  readonly quantidadeUsuarios = signal(0);
  readonly temUsuariosVinculados = signal(false);
  readonly modoDesativacao = signal<'substituir' | 'remover' | null>(null);
  readonly perfisAtivos = signal<PerfilListItem[]>([]);

  readonly form = this.fb.nonNullable.group({
    nome: ['', Validators.required],
    descricao: [''],
  });

  readonly abrangenciaForm = new FormGroup({
    escalas_ver: new FormControl<AbrangenciaSelect>('MeusSetores', { nonNullable: true }),
    escalas_mutar: new FormControl<AbrangenciaSelect>('MeusSetores', { nonNullable: true }),
    afastamentos_ver: new FormControl<AbrangenciaSelect>('MeusSetores', { nonNullable: true }),
    afastamentos_mutar: new FormControl<AbrangenciaSelect>('MeusSetores', { nonNullable: true }),
    servidores_ver: new FormControl<AbrangenciaSelect>('MeusSetores', { nonNullable: true }),
    servidores_mutar: new FormControl<AbrangenciaSelect>('MeusSetores', { nonNullable: true }),
    estrutura_ver: new FormControl<AbrangenciaSelect>('MeusSetores', { nonNullable: true }),
    estrutura_mutar: new FormControl<AbrangenciaSelect>('MeusSetores', { nonNullable: true }),
  });

  readonly perfilSubstitutoControl = new FormControl<string | null>(null);

  readonly visibleRows = computed(() =>
    MATRIX_ROWS.filter((row) => !row.adminOnly || this.currentUserIsSuperAdmin),
  );

  readonly permissaoByCodigo = computed(() => {
    const map = new Map<string, PermissaoItem>();
    for (const p of this.permissoes()) {
      map.set(p.codigo, p);
    }
    return map;
  });

  readonly showDesativarPanel = computed(
    () => this.isEdit() && !this.isSistema() && !this.isSuperAdminProfile(),
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

  readonly abrangenciaOptions: PciSelectOption[] = [
    { value: 'MeusSetores', label: 'Meus setores' },
    { value: 'TodosOsSetores', label: 'Todos os setores' },
  ];

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
          this.isSuperAdminProfile.set(perfil.codigo === SUPER_ADMIN_CODIGO);
          this.selectedPermissaoIds.set(perfil.permissaoIds ?? []);
          this.applyAbrangenciaFromProfile(perfil.abrangenciaPorPermissao);
        },
        error: () => this.error.set('Não foi possível carregar o perfil.'),
      });
    }
  }

  abrangenciaControl(rowKey: string, tipo: 'ver' | 'mutar'): FormControl<AbrangenciaSelect> {
    return this.abrangenciaForm.get(`${rowKey}_${tipo}`) as unknown as FormControl<AbrangenciaSelect>;
  }

  hasCrud(row: MatrixRowDef, action: CrudAction): boolean {
    return row.crud.includes(action);
  }

  isCrudChecked(row: MatrixRowDef, action: CrudAction): boolean {
    const codes = this.crudCodes(row, action);
    if (!codes.length) {
      return false;
    }
    const selected = this.selectedCodigoSet();
    return codes.some((code) => selected.has(code));
  }

  isExtraChecked(codigo: string): boolean {
    return this.selectedCodigoSet().has(codigo);
  }

  toggleCrud(row: MatrixRowDef, action: CrudAction, checked: boolean): void {
    if (this.isSuperAdminProfile()) {
      return;
    }
    this.applyCodigos(this.crudCodes(row, action), checked);
  }

  toggleExtra(codigo: string, checked: boolean): void {
    if (this.isSuperAdminProfile()) {
      return;
    }
    this.applyCodigos([codigo], checked);
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
    const permissaoIds = this.selectedPermissaoIds();
    const abrangenciaPorPermissao = this.collectAbrangenciaPayload();

    if (!this.isEdit()) {
      this.api
        .createPerfil({
          nome: value.nome.trim(),
          descricao: value.descricao.trim() || null,
          permissaoIds,
          abrangenciaPorPermissao,
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
          if (this.isSuperAdminProfile()) {
            void this.router.navigateByUrl('/perfis');
            return;
          }

          this.api
            .setPerfilPermissoes(id, {
              permissaoIds,
              abrangenciaPorPermissao,
            })
            .subscribe({
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
        this.perfilSubstitutoControl.setValue(null);
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
    this.perfilSubstitutoControl.setValue(null);
    this.error.set(null);
  }

  confirmarDesativacao(): void {
    if (!this.editId) {
      return;
    }

    if (this.temUsuariosVinculados()) {
      if (this.modoDesativacao() === 'substituir' && !this.perfilSubstitutoControl.value) {
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
          this.modoDesativacao() === 'substituir' ? this.perfilSubstitutoControl.value : null,
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

  private crudCodes(row: MatrixRowDef, action: CrudAction): string[] {
    return row.modules.map((modulo) => `${modulo}.${action}`);
  }

  private selectedCodigoSet(): Set<string> {
    const byId = new Map(this.permissoes().map((p) => [p.id, p.codigo]));
    const codes = new Set<string>();
    for (const id of this.selectedPermissaoIds()) {
      const codigo = byId.get(id);
      if (codigo) {
        codes.add(codigo);
      }
    }
    return codes;
  }

  private applyCodigos(codigos: string[], checked: boolean): void {
    const byCodigo = this.permissaoByCodigo();
    const ids = codigos
      .map((codigo) => byCodigo.get(codigo)?.id)
      .filter((id): id is string => !!id);

    if (!ids.length) {
      return;
    }

    const selected = new Set(this.selectedPermissaoIds());
    for (const id of ids) {
      if (checked) {
        selected.add(id);
      } else {
        selected.delete(id);
      }
    }
    this.selectedPermissaoIds.set([...selected]);
  }

  private applyAbrangenciaFromProfile(input?: Record<string, AbrangenciaModulo>): void {
    const map = input ?? {};
    for (const row of MATRIX_ROWS) {
      if (!row.hasAbrangencia) {
        continue;
      }

      const verCodes = this.verCodes(row);
      const mutarCodes = this.mutarCodes(row);
      this.abrangenciaControl(row.key, 'ver').setValue(
        this.hasTodosInMap(map, verCodes) ? 'TodosOsSetores' : 'MeusSetores',
      );
      this.abrangenciaControl(row.key, 'mutar').setValue(
        this.hasTodosInMap(map, mutarCodes) ? 'TodosOsSetores' : 'MeusSetores',
      );
    }
  }

  private collectAbrangenciaPayload(): Record<string, AbrangenciaModulo> {
    const selectedCodes = this.selectedCodigoSet();
    const result: Record<string, AbrangenciaModulo> = {};

    for (const row of this.visibleRows()) {
      if (!row.hasAbrangencia) {
        continue;
      }

      const ver = this.abrangenciaControl(row.key, 'ver').value;
      const mutar = this.abrangenciaControl(row.key, 'mutar').value;

      for (const code of this.verCodes(row)) {
        if (selectedCodes.has(code) && ver === 'TodosOsSetores') {
          result[code] = 'TodosOsSetores';
        }
      }

      for (const code of this.mutarCodes(row)) {
        if (selectedCodes.has(code) && mutar === 'TodosOsSetores') {
          result[code] = 'TodosOsSetores';
        }
      }
    }

    return result;
  }

  /** Listar + ações institucionais (devolver/exportar). */
  private verCodes(row: MatrixRowDef): string[] {
    const codes = this.crudCodes(row, 'listar');
    for (const extra of row.extras) {
      if (extra.codigo.endsWith('.devolver') || extra.codigo.endsWith('.exportar')) {
        codes.push(extra.codigo);
      }
    }
    return codes;
  }

  private mutarCodes(row: MatrixRowDef): string[] {
    const codes = [
      ...this.crudCodes(row, 'criar'),
      ...this.crudCodes(row, 'editar'),
      ...this.crudCodes(row, 'excluir'),
    ];
    for (const extra of row.extras) {
      if (!extra.codigo.endsWith('.devolver') && !extra.codigo.endsWith('.exportar')) {
        codes.push(extra.codigo);
      }
    }
    return codes;
  }

  private hasTodosInMap(
    map: Record<string, AbrangenciaModulo>,
    codes: string[],
  ): boolean {
    return codes.some((code) => {
      const value = map[code];
      return value === 'TodosOsSetores' || value === 2;
    });
  }

  private fail(message?: string): void {
    this.error.set(message ?? 'Operação não concluída.');
    this.saving.set(false);
  }
}

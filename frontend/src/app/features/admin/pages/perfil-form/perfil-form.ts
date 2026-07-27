import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import {
  PciAlertComponent,
  PciButtonComponent,
  PciCheckboxComponent,
  PciFormPageComponent,
  PciInputComponent,
  PciStackComponent,
} from '@davillawitte/pci-design-system';

import { AuthService } from '../../../../core/auth/auth.service';
import { ADMIN_ROUTE_PAGES } from '../../admin-route-pages';
import { AdminApiService } from '../../services/admin-api.service';
import type { PerfilListItem } from '../../models/admin.models';
import { AppFormColDirective, AppFormSectionComponent } from '../../../../shared/form-layout';

const SUPER_ADMIN_CODIGO = 'SUPERADMINISTRADOR';
const AREA_SETOR = 'Gestão do Setor';
const AREA_INSTITUCIONAL = 'Gestão Institucional';

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
  readonly isEdit = signal(false);
  readonly isSistema = signal(false);
  readonly isSuperAdminProfile = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly currentPath = signal('/perfis/novo');
  readonly areaSetor = signal(false);
  readonly areaInstitucional = signal(false);

  readonly desativarConfirmado = signal(false);
  readonly quantidadeUsuarios = signal(0);
  readonly temUsuariosVinculados = signal(false);
  readonly modoDesativacao = signal<'substituir' | 'remover' | null>(null);
  readonly perfisAtivos = signal<PerfilListItem[]>([]);

  readonly form = this.fb.nonNullable.group({
    nome: ['', Validators.required],
    descricao: [''],
  });

  readonly perfilSubstitutoControl = new FormControl<string | null>(null);

  readonly canDesativar = () => this.isEdit() && !this.isSistema() && !this.isSuperAdminProfile();

  private editId: string | null = null;

  /** Exposto ao template (desativação / substituto). */
  get perfilId(): string | null {
    return this.editId;
  }

  ngOnInit(): void {
    this.editId = this.route.snapshot.paramMap.get('id');
    this.isEdit.set(!!this.editId);
    this.currentPath.set(this.editId ? '/perfis/editar/:id' : '/perfis/novo');

    if (!this.editId) {
      return;
    }

    this.api.getPerfil(this.editId).subscribe({
      next: (perfil) => {
        this.form.patchValue({
          nome: perfil.nome,
          descricao: perfil.descricao ?? '',
        });
        this.isSistema.set(perfil.sistema);
        this.isSuperAdminProfile.set(perfil.codigo === SUPER_ADMIN_CODIGO);
        const areas = perfil.areas ?? [];
        this.areaSetor.set(areas.includes(AREA_SETOR));
        this.areaInstitucional.set(areas.includes(AREA_INSTITUCIONAL));
      },
      error: () => this.error.set('Não foi possível carregar o perfil.'),
    });
  }

  toggleAreaSetor(checked: boolean): void {
    if (this.isSuperAdminProfile()) return;
    this.areaSetor.set(checked);
  }

  toggleAreaInstitucional(checked: boolean): void {
    if (this.isSuperAdminProfile()) return;
    this.areaInstitucional.set(checked);
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
    const areas = this.collectAreas();

    if (!this.isEdit()) {
      this.api
        .createPerfil({
          nome: value.nome.trim(),
          descricao: value.descricao.trim() || null,
          areas,
        })
        .subscribe({
          next: () => {
            this.auth.refreshSession().subscribe({
              next: () => void this.router.navigateByUrl('/perfis'),
              error: () => void this.router.navigateByUrl('/perfis'),
            });
          },
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

          this.api.setPerfilPermissoes(id, { areas }).subscribe({
            next: () => {
              this.auth.refreshSession().subscribe({
                next: () => void this.router.navigateByUrl('/perfis'),
                error: () => void this.router.navigateByUrl('/perfis'),
              });
            },
            error: (err: { error?: { message?: string } }) => this.fail(err.error?.message),
          });
        },
        error: (err: { error?: { message?: string } }) => this.fail(err.error?.message),
      });
  }

  iniciarDesativacao(): void {
    if (!this.editId) return;
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
    if (!this.editId) return;
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

  private collectAreas(): string[] {
    const areas: string[] = [];
    if (this.areaSetor()) areas.push(AREA_SETOR);
    if (this.areaInstitucional()) areas.push(AREA_INSTITUCIONAL);
    return areas;
  }

  private fail(message?: string): void {
    this.error.set(message ?? 'Operação não concluída.');
    this.saving.set(false);
  }
}

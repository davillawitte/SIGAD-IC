import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatSelectModule } from '@angular/material/select';
import { ActivatedRoute, Router } from '@angular/router';
import {
  PciAlertComponent,
  PciButtonComponent,
  PciCheckboxComponent,
  PciFeedbackModalService,
  PciFormPageComponent,
  PciInputComponent,
  PciSelectionListComponent,
  PciStackComponent,
} from '@davillawitte/pci-design-system';
import type { PciSelectOption, PciSelectionListItem } from '@davillawitte/pci-design-system';

import { ADMIN_ROUTE_PAGES } from '../../admin-route-pages';
import { AdminApiService } from '../../services/admin-api.service';
import type { PerfilListItem, ServidorListItem } from '../../models/admin.models';
import { AppFormColDirective, AppFormSectionComponent } from '../../../../shared/form-layout';

@Component({
  selector: 'app-usuario-form-page',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatSelectModule,
    PciAlertComponent,
    PciButtonComponent,
    PciCheckboxComponent,
    PciFormPageComponent,
    PciInputComponent,
    PciSelectionListComponent,
    PciStackComponent,
    AppFormSectionComponent,
    AppFormColDirective,
  ],
  templateUrl: './usuario-form-page.component.html',
  styleUrl: './usuario-form-page.component.scss',
})
export class UsuarioFormPageComponent implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly feedback = inject(PciFeedbackModalService);

  readonly routePages = ADMIN_ROUTE_PAGES;
  readonly isEdit = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly createdLogin = signal<string | null>(null);
  readonly createdPassword = signal<string | null>(null);
  readonly servidores = signal<ServidorListItem[]>([]);
  readonly perfis = signal<PerfilListItem[]>([]);
  readonly selectedPerfilIds = signal<string[]>([]);
  readonly currentPath = signal('/usuarios/novo');

  readonly form = this.fb.nonNullable.group({
    servidorId: [''],
    login: [''],
    nomeServidor: [''],
    ativo: [true],
  });

  readonly servidorOptions = computed<PciSelectOption[]>(() =>
    this.servidores().map((s) => ({
      label: `${s.nome} — ${s.matricula} — CPF ${s.cpf}`,
      value: s.id,
    })),
  );

  readonly perfilItems = computed<PciSelectionListItem[]>(() =>
    this.perfis().map((perfil) => ({
      id: perfil.id,
      label: perfil.nome,
    })),
  );

  private editId: string | null = null;

  ngOnInit(): void {
    this.editId = this.route.snapshot.paramMap.get('id');
    this.isEdit.set(!!this.editId);
    this.currentPath.set(this.editId ? '/usuarios/editar/:id' : '/usuarios/novo');
    this.loadLookups();

    if (!this.editId) {
      this.form.controls.servidorId.setValidators([Validators.required]);
    }

    if (this.editId) {
      this.form.controls.login.disable();
      this.form.controls.nomeServidor.disable();
      this.api.getUsuario(this.editId).subscribe({
        next: (usuario) => {
          this.form.patchValue({
            login: usuario.login,
            nomeServidor: usuario.nomeServidor,
            ativo: usuario.ativo,
          });
          this.selectedPerfilIds.set(usuario.perfilIds ?? []);
        },
        error: () => this.error.set('Não foi possível carregar o usuário.'),
      });
    }
  }

  save(): void {
    if (this.createdPassword()) {
      void this.router.navigateByUrl('/usuarios');
      return;
    }

    if (this.selectedPerfilIds().length === 0) {
      this.error.set('Selecione ao menos um perfil.');
      return;
    }

    if (!this.isEdit()) {
      this.form.controls.servidorId.markAsTouched();
      if (this.form.controls.servidorId.invalid) {
        this.error.set('Selecione o servidor antes de salvar.');
        return;
      }
    }

    this.saving.set(true);
    this.error.set(null);
    const value = this.form.getRawValue();

    if (this.isEdit() && this.editId) {
      this.api
        .updateUsuario(this.editId, {
          perfilIds: this.selectedPerfilIds(),
          ativo: value.ativo,
        })
        .subscribe({
          next: () => {
            this.feedback.showSuccess('Usuário atualizado com sucesso.');
            void this.router.navigateByUrl('/usuarios');
          },
          error: (err: { error?: { message?: string } }) => {
            this.error.set(err.error?.message ?? 'Falha ao atualizar usuário.');
            this.saving.set(false);
          },
        });
      return;
    }

    this.api
      .createUsuario({
        servidorId: value.servidorId,
        perfilIds: this.selectedPerfilIds(),
      })
      .subscribe({
        next: (created) => {
          this.createdLogin.set(created.login);
          this.createdPassword.set(created.senhaTemporaria);
          this.saving.set(false);
          this.feedback.showSuccess('Usuário criado com sucesso.');
        },
        error: (err: { error?: { message?: string } }) => {
          this.error.set(err.error?.message ?? 'Falha ao criar usuário.');
          this.saving.set(false);
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
    this.api.listPerfis({ page: 1, pageSize: 100 }).subscribe({
      next: (result) => this.perfis.set(result.items.filter((p) => p.ativo)),
    });
  }
}

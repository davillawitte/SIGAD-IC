import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import {
  PciAlertComponent,
  PciFormPageComponent,
  PciInputComponent,
  PciSelectComponent,
} from '@davillawitte/pci-design-system';
import type { PciSelectOption } from '@davillawitte/pci-design-system';

import { AuthService } from '../../../../core/auth/auth.service';
import { AdminApiService } from '../../../admin/services/admin-api.service';
import { AppFormColDirective, AppFormSectionComponent } from '../../../../shared/form-layout';
import { AFASTAMENTOS_ROUTE_PAGES } from '../../afastamentos.routes.meta';
import { AfastamentosApiService } from '../../services/afastamentos-api.service';

@Component({
  selector: 'app-afastamento-form',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    PciAlertComponent,
    PciFormPageComponent,
    PciInputComponent,
    PciSelectComponent,
    AppFormSectionComponent,
    AppFormColDirective,
  ],
  templateUrl: './afastamento-form.html',
})
export class AfastamentoForm implements OnInit {
  private readonly api = inject(AfastamentosApiService);
  private readonly adminApi = inject(AdminApiService);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly routePages = AFASTAMENTOS_ROUTE_PAGES;
  readonly isEdit = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly servidorOptions = signal<PciSelectOption[]>([]);
  readonly currentPath = signal('/afastamentos/novo');

  /** setorId por servidor, para validar escopo do chefe no save. */
  private readonly servidorSetorById = new Map<string, string>();

  readonly tipoOptions: PciSelectOption[] = [
    { label: 'FR — Férias', value: 'FR' },
    { label: 'LM — Licença Médica', value: 'LM' },
    { label: 'LP — Licença Prêmio', value: 'LP' },
    { label: 'LO — Licença Outros', value: 'LO' },
  ];

  readonly form = this.fb.nonNullable.group({
    servidorId: ['', Validators.required],
    dataInicio: ['', Validators.required],
    dataFim: ['', Validators.required],
    tipoOcorrenciaCodigo: ['FR', Validators.required],
    sei: [''],
    observacao: [''],
  });

  private editId: string | null = null;

  ngOnInit(): void {
    this.editId = this.route.snapshot.paramMap.get('id');
    this.isEdit.set(!!this.editId);
    this.currentPath.set(this.editId ? '/afastamentos/editar/:id' : '/afastamentos/novo');

    this.adminApi.listMeusServidores().subscribe({
      next: (servidores) => {
        const doSetor = servidores.filter((s) =>
          this.auth.canAccess('afastamentos.criar', s.setorId),
        );

        this.servidorSetorById.clear();
        for (const s of doSetor) {
          this.servidorSetorById.set(s.id, s.setorId);
        }

        this.servidorOptions.set(
          doSetor.map((s) => ({
            label: `${s.nome} — ${s.matricula}`,
            value: s.id,
          })),
        );
      },
      error: () => this.error.set('Não foi possível carregar os servidores do seu setor.'),
    });

    if (this.editId) {
      this.form.controls.servidorId.disable();
      this.api.get(this.editId).subscribe({
        next: (item) => {
          if (!this.auth.canAccess('afastamentos.editar', item.setorId)) {
            this.error.set('Sem permissão para alterar afastamento neste setor.');
            this.form.disable();
            return;
          }
          this.form.patchValue({
            servidorId: item.servidorId,
            dataInicio: item.dataInicio.slice(0, 10),
            dataFim: item.dataFim.slice(0, 10),
            tipoOcorrenciaCodigo: item.tipoOcorrenciaCodigo,
            sei: item.sei ?? '',
            observacao: item.observacao ?? '',
          });
        },
        error: () => this.error.set('Não foi possível carregar o afastamento.'),
      });
    }
  }

  save(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.error.set('Preencha os campos obrigatórios.');
      return;
    }

    const value = this.form.getRawValue();
    if (!this.isEdit()) {
      const setorId = this.servidorSetorById.get(value.servidorId);
      if (!setorId || !this.auth.canAccess('afastamentos.criar', setorId)) {
        this.error.set('Só é possível cadastrar afastamento para servidores do seu setor.');
        return;
      }
    }

    this.saving.set(true);
    this.error.set(null);

    if (!this.isEdit()) {
      this.api
        .create({
          servidorId: value.servidorId,
          dataInicio: value.dataInicio,
          dataFim: value.dataFim,
          tipoOcorrenciaCodigo: value.tipoOcorrenciaCodigo,
          observacao: value.observacao.trim() || null,
          sei: value.sei.trim() || null,
        })
        .subscribe({
          next: () => void this.router.navigateByUrl('/afastamentos'),
          error: (err: { error?: { message?: string } }) => {
            this.error.set(err.error?.message ?? 'Não foi possível salvar.');
            this.saving.set(false);
          },
        });
      return;
    }

    this.api
      .update(this.editId!, {
        dataInicio: value.dataInicio,
        dataFim: value.dataFim,
        tipoOcorrenciaCodigo: value.tipoOcorrenciaCodigo,
        observacao: value.observacao.trim() || null,
        sei: value.sei.trim() || null,
      })
      .subscribe({
        next: () => void this.router.navigateByUrl('/afastamentos'),
        error: (err: { error?: { message?: string } }) => {
          this.error.set(err.error?.message ?? 'Não foi possível salvar.');
          this.saving.set(false);
        },
      });
  }

  cancel(): void {
    void this.router.navigateByUrl('/afastamentos');
  }
}

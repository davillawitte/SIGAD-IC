import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatSelectModule } from '@angular/material/select';
import { ActivatedRoute, Router } from '@angular/router';
import {
  PciAlertComponent,
  PciFormPageComponent,
  PciInputComponent,
} from '@davillawitte/pci-design-system';
import type { PciSelectOption } from '@davillawitte/pci-design-system';

import { ADMIN_ROUTE_PAGES } from '../../admin-route-pages';
import { AdminApiService } from '../../services/admin-api.service';
import type { ServidorListItem } from '../../models/admin.models';
import { AppFormColDirective, AppFormSectionComponent } from '../../../../shared/form-layout';

@Component({
  selector: 'app-nucleo-form-page',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatSelectModule,
    PciAlertComponent,
    PciFormPageComponent,
    PciInputComponent,
    AppFormSectionComponent,
    AppFormColDirective,
  ],
  templateUrl: './nucleo-form-page.component.html',
  styleUrl: './nucleo-form-page.component.scss',
})
export class NucleoFormPageComponent implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly routePages = ADMIN_ROUTE_PAGES;
  readonly isEdit = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly servidores = signal<ServidorListItem[]>([]);
  readonly currentPath = signal('/estrutura-organizacional/nucleos/novo');

  readonly form = this.fb.nonNullable.group({
    nome: ['', Validators.required],
    sigla: ['', Validators.required],
    chefeServidorId: [''],
  });

  readonly servidorOptions = computed<PciSelectOption[]>(() =>
    this.servidores()
      .filter((s) => s.status === 'Ativo')
      .map((s) => ({ label: `${s.nome} — ${s.matricula}`, value: s.id })),
  );

  private editId: string | null = null;

  ngOnInit(): void {
    this.editId = this.route.snapshot.paramMap.get('id');
    this.isEdit.set(!!this.editId);
    this.currentPath.set(
      this.editId
        ? '/estrutura-organizacional/nucleos/editar/:id'
        : '/estrutura-organizacional/nucleos/novo',
    );

    this.api.listServidores(false).subscribe({ next: (items) => this.servidores.set(items) });

    if (this.editId) {
      this.api.getNucleo(this.editId).subscribe({
        next: (nucleo) => {
          this.form.patchValue({
            nome: nucleo.nome,
            sigla: nucleo.sigla,
            chefeServidorId: nucleo.chefeServidorId ?? '',
          });
        },
        error: () => this.error.set('Não foi possível carregar o núcleo.'),
      });
    }
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
    const payload = {
      nome: value.nome.trim(),
      sigla: value.sigla.trim(),
      chefeServidorId: value.chefeServidorId || null,
    };

    if (this.isEdit() && this.editId) {
      this.api.updateNucleo(this.editId, payload).subscribe({
        next: () => void this.router.navigateByUrl('/estrutura-organizacional'),
        error: (err: { error?: { message?: string } }) => this.fail(err.error?.message),
      });
      return;
    }

    this.api.createNucleo(payload).subscribe({
      next: () => void this.router.navigateByUrl('/estrutura-organizacional'),
      error: (err: { error?: { message?: string } }) => this.fail(err.error?.message),
    });
  }

  cancel(): void {
    void this.router.navigateByUrl('/estrutura-organizacional');
  }

  private fail(message?: string): void {
    this.error.set(message ?? 'Operação não concluída.');
    this.saving.set(false);
  }
}

import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSelectModule } from '@angular/material/select';
import { ActivatedRoute, Router } from '@angular/router';
import {
  PciAlertComponent,
  PciFeedbackModalService,
  PciFormPageComponent,
  PciInputComponent,
} from '@davillawitte/pci-design-system';
import type { PciSelectOption } from '@davillawitte/pci-design-system';
import { Observable, of } from 'rxjs';
import { switchMap } from 'rxjs/operators';

import { ADMIN_ROUTE_PAGES } from '../../admin-route-pages';
import { AdminApiService } from '../../services/admin-api.service';
import type {
  ChefiaConflito,
  CreateSetorPayload,
  NucleoListItem,
  ServidorListItem,
  SetorChefiaInput,
  TipoChefia,
} from '../../models/admin.models';
import { AppFormColDirective, AppFormSectionComponent } from '../../../../shared/form-layout';
import { openConfirmDialog } from '../../../../shared/dialogs/dialog.helpers';

const DIRECAO_IC_SIGLA = 'Direção IC';
const DIRECAO_IC_NOME = 'Direção do Instituto de Criminalística';

function normalizeSigla(value: string): string {
  return value
    .trim()
    .toLowerCase()
    .normalize('NFD')
    .replace(/\p{M}/gu, '');
}

function isDirecaoSigla(value: string): boolean {
  return normalizeSigla(value) === normalizeSigla(DIRECAO_IC_SIGLA);
}

@Component({
  selector: 'app-setor-form-page',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatSelectModule,
    PciAlertComponent,
    PciFormPageComponent,
    PciInputComponent,
    AppFormSectionComponent,
    AppFormColDirective,
  ],
  templateUrl: './setor-form-page.component.html',
  styleUrl: './setor-form-page.component.scss',
})
export class SetorFormPageComponent implements OnInit {
  private readonly api = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly dialog = inject(MatDialog);
  private readonly feedback = inject(PciFeedbackModalService);

  readonly routePages = ADMIN_ROUTE_PAGES;
  readonly isEdit = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly nucleos = signal<NucleoListItem[]>([]);
  readonly servidores = signal<ServidorListItem[]>([]);
  readonly currentPath = signal('/estrutura-organizacional/setores/novo');
  readonly loadedIsDirecao = signal(false);

  readonly form = this.fb.nonNullable.group({
    nome: ['', Validators.required],
    sigla: ['', Validators.required],
    resumo: [''],
    nucleoId: [''],
    chefiaPrimariaId: ['', Validators.required],
    chefiaSecundariaId: [''],
  });

  readonly isDirecaoIc = computed(() => {
    if (this.loadedIsDirecao()) {
      return true;
    }
    return isDirecaoSigla(this.form.controls.sigla.value || '');
  });

  readonly pageTitle = computed(() => {
    if (this.isDirecaoIc()) {
      return this.isEdit() ? 'Editar Direção' : 'Nova Direção';
    }
    return this.isEdit() ? 'Editar setor' : 'Novo setor';
  });

  readonly pageDescription = computed(() =>
    this.isDirecaoIc()
      ? 'Direção do Instituto de Criminalística da PCI/RN'
      : 'Todo setor deve pertencer a um núcleo e ter chefia imediata.',
  );

  readonly nucleoOptions = computed<PciSelectOption[]>(() =>
    this.nucleos().map((n) => ({ label: n.nome, value: n.id })),
  );

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
        ? '/estrutura-organizacional/setores/editar/:id'
        : '/estrutura-organizacional/setores/novo',
    );

    const nucleoPrefill = this.route.snapshot.queryParamMap.get('nucleoId');
    if (nucleoPrefill) {
      this.form.controls.nucleoId.setValue(nucleoPrefill);
    }

    this.api.listNucleos().subscribe({ next: (items) => this.nucleos.set(items) });
    this.api.listServidores(false).subscribe({ next: (items) => this.servidores.set(items) });

    this.form.controls.sigla.valueChanges.subscribe((sigla) => {
      if (!this.isEdit() && isDirecaoSigla(sigla || '')) {
        this.applyDirecaoDefaults();
      }
    });

    if (this.editId) {
      this.api.getSetor(this.editId).subscribe({
        next: (setor) => {
          this.loadedIsDirecao.set(setor.isDirecaoIc);
          const primaria = this.findChefia(
            setor.chefias,
            setor.isDirecaoIc ? 'Diretor' : 'ChefiaImediata',
          );
          const secundaria = this.findChefia(
            setor.chefias,
            setor.isDirecaoIc ? 'Subcoordenador' : 'ChefiaSubstituta',
          );

          this.form.patchValue({
            nome: setor.nome,
            sigla: setor.sigla,
            resumo: setor.resumo ?? '',
            nucleoId: setor.nucleoId ?? '',
            chefiaPrimariaId: primaria ?? '',
            chefiaSecundariaId: secundaria ?? '',
          });

          if (setor.isDirecaoIc) {
            this.applyDirecaoDefaults();
          }
        },
        error: () => this.error.set('Não foi possível carregar o setor.'),
      });
    }
  }

  save(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.error.set('Preencha os campos obrigatórios antes de salvar.');
      return;
    }

    const value = this.form.getRawValue();
    const isDirecao = this.isDirecaoIc();
    if (!isDirecao && !value.nucleoId) {
      this.error.set('Informe o núcleo do setor.');
      return;
    }

    const chefias = this.buildChefias(isDirecao, value.chefiaPrimariaId, value.chefiaSecundariaId);
    if (!chefias) {
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    const common: CreateSetorPayload = {
      nome: isDirecao ? DIRECAO_IC_NOME : value.nome.trim(),
      sigla: isDirecao ? DIRECAO_IC_SIGLA : value.sigla.trim(),
      resumo: value.resumo.trim() || null,
      nucleoId: isDirecao ? null : value.nucleoId || null,
      chefias,
    };

    this.api
      .previewChefiasConflitos({
        setorId: this.editId,
        chefias,
      })
      .pipe(
        switchMap((conflitos) =>
          this.confirmChefiasConflitos$(conflitos).pipe(
            switchMap((confirmed) => {
              if (!confirmed) {
                this.saving.set(false);
                return of(null);
              }
              const payload = {
                ...common,
                confirmarRemocaoChefiasEmOutrosSetores: conflitos.length > 0,
              };
              if (this.isEdit() && this.editId) {
                return this.api.updateSetor(this.editId, payload);
              }
              return this.api.createSetor(payload);
            }),
          ),
        ),
      )
      .subscribe({
        next: (result) => {
          if (!result) return;
          this.feedback.showSuccess(
            this.isEdit() ? 'Setor atualizado com sucesso.' : 'Setor criado com sucesso.',
          );
          void this.router.navigateByUrl('/estrutura-organizacional');
        },
        error: (err: { error?: { message?: string } }) => this.fail(err.error?.message),
      });
  }

  private confirmChefiasConflitos$(conflitos: ChefiaConflito[]): Observable<boolean> {
    if (!conflitos.length) {
      return of(true);
    }
    const parts = conflitos.map((c) => {
      const tipo = this.labelTipoChefia(c.tipoChefia);
      return `${c.servidorNome} (${tipo} em ${c.setorNome})`;
    });
    return openConfirmDialog(this.dialog, {
      title: 'Conflito de chefia',
      message:
        'Os servidores abaixo já são chefia em outro setor. Ao confirmar, esses vínculos serão removidos:\n\n' +
        parts.join('\n'),
      confirmLabel: 'Remover e continuar',
      danger: true,
    });
  }

  private labelTipoChefia(tipo: TipoChefia): string {
    switch (tipo) {
      case 'ChefiaImediata':
        return 'Chefia imediata';
      case 'ChefiaSubstituta':
        return 'Chefia substituta';
      case 'Diretor':
        return 'Diretor';
      case 'Subcoordenador':
        return 'Subcoordenador';
      default:
        return tipo;
    }
  }

  cancel(): void {
    void this.router.navigateByUrl('/estrutura-organizacional');
  }

  private applyDirecaoDefaults(): void {
    this.form.controls.nome.setValue(DIRECAO_IC_NOME);
    this.form.controls.sigla.setValue(DIRECAO_IC_SIGLA);
    this.form.controls.nucleoId.setValue('');
    this.form.controls.nome.disable({ emitEvent: false });
    this.form.controls.sigla.disable({ emitEvent: false });
  }

  private findChefia(
    chefias: { tipoChefia: TipoChefia; servidorId: string }[] | undefined,
    tipo: TipoChefia,
  ): string | null {
    return (chefias ?? []).find((c) => c.tipoChefia === tipo)?.servidorId ?? null;
  }

  private buildChefias(
    isDirecao: boolean,
    primariaId: string,
    secundariaId: string,
  ): SetorChefiaInput[] | null {
    if (!primariaId) {
      this.error.set(isDirecao ? 'Informe o Diretor.' : 'Informe a chefia imediata.');
      return null;
    }

    if (secundariaId && secundariaId === primariaId) {
      this.error.set('O segundo papel de chefia deve ser um servidor diferente.');
      return null;
    }

    const primariaTipo: TipoChefia = isDirecao ? 'Diretor' : 'ChefiaImediata';
    const secundariaTipo: TipoChefia = isDirecao ? 'Subcoordenador' : 'ChefiaSubstituta';
    const chefias: SetorChefiaInput[] = [{ tipoChefia: primariaTipo, servidorId: primariaId }];
    if (secundariaId) {
      chefias.push({ tipoChefia: secundariaTipo, servidorId: secundariaId });
    }
    return chefias;
  }

  private fail(message?: string): void {
    this.error.set(message ?? 'Operação não concluída.');
    this.saving.set(false);
  }
}

import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import {
  PciAlertComponent,
  PciButtonComponent,
  PciDatepickerComponent,
  PciFormActionsComponent,
  PciFormCardComponent,
  PciIconComponent,
  PciInputComponent,
  PciSelectComponent,
} from '@davillawitte/pci-design-system';
import type { PciSelectOption } from '@davillawitte/pci-design-system';
import { Subscription, forkJoin } from 'rxjs';

import { AuthService } from '../../../../core/auth/auth.service';
import { AdminApiService } from '../../services/admin-api.service';
import type { CargoListItem, NucleoListItem, ServidorListItem, SetorListItem, StatusServidor } from '../../models/admin.models';
import { AppFormColDirective, AppFormSectionComponent } from '../../../../shared/form-layout';
import { AppDialogHeaderComponent } from '../../../../shared/dialogs/dialog-header/dialog-header';
import {
  isCpfComplete,
  isEmailValid,
  isMatriculaValid,
  isTelefoneValid,
  maskCpf,
  maskMatricula,
  maskTelefone,
} from '../../../../shared/input-masks';

const STATUS_OPTIONS: { label: string; value: StatusServidor }[] = [
  { label: 'Ativo', value: 'Ativo' },
  { label: 'Afastado', value: 'Afastado' },
  { label: 'Cedido', value: 'Cedido' },
];

type LotacaoTipo = 'setor' | 'nucleo';

const LOTACAO_OPTIONS: { label: string; value: LotacaoTipo }[] = [
  { label: 'Lotado em um Setor', value: 'setor' },
  { label: 'Lotado diretamente no Núcleo', value: 'nucleo' },
];

function matriculaValidator(control: AbstractControl): ValidationErrors | null {
  const v = (control.value ?? '').toString().trim();
  if (!v) return { required: true };
  return isMatriculaValid(v) ? null : { matricula: true };
}

function cpfValidator(control: AbstractControl): ValidationErrors | null {
  const v = (control.value ?? '').toString();
  if (!v.trim()) return { required: true };
  return isCpfComplete(v) ? null : { cpf: true };
}

function emailFormatValidator(control: AbstractControl): ValidationErrors | null {
  const v = (control.value ?? '').toString().trim();
  if (!v) return null;
  return isEmailValid(v) ? null : { email: true };
}

function telefoneValidator(control: AbstractControl): ValidationErrors | null {
  const v = (control.value ?? '').toString();
  return isTelefoneValid(v) ? null : { telefone: true };
}

function toDateOnlyString(value: string | null | undefined): string | null {
  if (!value?.trim()) return null;
  return value.slice(0, 10);
}

/** Versão só-criação de `servidor-form` embutida em `MatDialog`, pra cadastrar um servidor sem
 * sair da tela que precisa dele (ex.: seleção de servidor em `usuario-form`). Mesmo padrão de
 * `AfastamentoDialog` — sem `Router`, fecha com o item criado ou `false`. */
@Component({
  selector: 'app-servidor-dialog',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    PciAlertComponent,
    PciButtonComponent,
    PciDatepickerComponent,
    PciFormActionsComponent,
    PciFormCardComponent,
    PciIconComponent,
    PciInputComponent,
    PciSelectComponent,
    AppFormSectionComponent,
    AppFormColDirective,
    AppDialogHeaderComponent,
  ],
  templateUrl: './servidor-dialog.html',
})
export class ServidorDialog implements OnInit, OnDestroy {
  private readonly api = inject(AdminApiService);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<ServidorDialog, ServidorListItem | false>);
  private readonly subs = new Subscription();

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly created = signal<ServidorListItem | null>(null);
  readonly cargos = signal<CargoListItem[]>([]);
  readonly setores = signal<SetorListItem[]>([]);
  readonly nucleos = signal<NucleoListItem[]>([]);
  readonly statusOptions = STATUS_OPTIONS;
  readonly lotacaoOptions = LOTACAO_OPTIONS;
  readonly maxNascimento = toDateOnlyString(new Date().toISOString())!;

  readonly form = this.fb.nonNullable.group({
    nome: ['', Validators.required],
    matricula: ['', matriculaValidator],
    cpf: ['', cpfValidator],
    dataNascimento: ['', Validators.required],
    email: ['', emailFormatValidator],
    telefone: ['', telefoneValidator],
    cargoId: ['', Validators.required],
    lotacaoTipo: ['setor' as LotacaoTipo, Validators.required],
    setorId: ['', Validators.required],
    nucleoId: [''],
    status: ['Ativo' as StatusServidor, Validators.required],
  });

  readonly lotacaoTipo = signal<LotacaoTipo>('setor');

  readonly cargoOptions = computed<PciSelectOption[]>(() =>
    this.cargos().map((c) => ({ label: c.nome, value: c.id })),
  );

  readonly nucleoOptions = computed<PciSelectOption[]>(() =>
    this.nucleos().map((n) => ({ label: `${n.sigla} — ${n.nome}`, value: n.id })),
  );

  readonly setorOptions = computed<PciSelectOption[]>(() =>
    this.setores()
      .filter((s) => this.auth.canAccess('servidores.criar', s.id))
      .map((s) => ({ label: `${s.sigla} — ${s.nome}`, value: s.id })),
  );

  ngOnInit(): void {
    this.bindMask('matricula', maskMatricula);
    this.bindMask('cpf', maskCpf);
    this.bindMask('telefone', maskTelefone);
    this.bindLotacaoTipo();

    this.api.listCargos().subscribe({ next: (items) => this.cargos.set(items) });
    this.api.listSetores().subscribe({ next: (items) => this.setores.set(items) });
    this.api.listNucleos().subscribe({ next: (items) => this.nucleos.set(items) });
  }

  /** Alterna obrigatoriedade de setor/núcleo conforme o tipo de lotação escolhido, e limpa o
   * campo que deixou de ser usado (invariante do domínio: um ou outro, não os dois). */
  private bindLotacaoTipo(): void {
    const setorControl = this.form.controls.setorId;
    const nucleoControl = this.form.controls.nucleoId;

    this.subs.add(
      this.form.controls.lotacaoTipo.valueChanges.subscribe((tipo) => {
        this.lotacaoTipo.set(tipo);
        if (tipo === 'setor') {
          setorControl.setValidators(Validators.required);
          nucleoControl.clearValidators();
          nucleoControl.setValue('', { emitEvent: false });
        } else {
          nucleoControl.setValidators(Validators.required);
          setorControl.clearValidators();
          setorControl.setValue('', { emitEvent: false });
        }
        setorControl.updateValueAndValidity({ emitEvent: false });
        nucleoControl.updateValueAndValidity({ emitEvent: false });
      }),
    );
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
  }

  save(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.error.set('Verifique os campos obrigatórios e os formatos (CPF, matrícula e telefone).');
      return;
    }

    const value = this.form.getRawValue();
    const dataNascimento = toDateOnlyString(value.dataNascimento);
    if (!dataNascimento) {
      this.error.set('Informe a data de nascimento.');
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    const emailTrim = value.email.trim();
    const payload = {
      nome: value.nome.trim(),
      matricula: value.matricula.trim(),
      cpf: value.cpf,
      email: emailTrim || null,
      telefone: value.telefone.trim() || null,
      dataNascimento,
      cargoId: value.cargoId,
      setorId: value.lotacaoTipo === 'setor' ? value.setorId : null,
      nucleoId: value.lotacaoTipo === 'nucleo' ? value.nucleoId : null,
      status: value.status,
    };

    this.api.createServidor(payload).subscribe({
      next: (created) => {
        this.saving.set(false);
        this.created.set(created);
      },
      error: (err: { error?: { message?: string } }) => {
        this.error.set(err.error?.message ?? 'Não foi possível salvar o servidor.');
        this.saving.set(false);
      },
    });
  }

  cancel(): void {
    this.dialogRef.close(false);
  }

  concluir(): void {
    this.dialogRef.close(this.created()!);
  }

  close(): void {
    this.dialogRef.close(this.created() ?? false);
  }

  private bindMask(controlName: 'matricula' | 'cpf' | 'telefone', maskFn: (v: string) => string): void {
    const control = this.form.controls[controlName];
    this.subs.add(
      control.valueChanges.subscribe((raw) => {
        const masked = maskFn(raw ?? '');
        if (masked !== raw) {
          control.setValue(masked, { emitEvent: false });
        }
      }),
    );
  }
}

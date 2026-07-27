import { CommonModule } from '@angular/common';
import {
  afterNextRender,
  Component,
  Injector,
  OnDestroy,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { MatSelectModule } from '@angular/material/select';
import { ActivatedRoute, Router } from '@angular/router';
import {
  PciAlertComponent,
  PciDatepickerComponent,
  PciFeedbackModalService,
  PciFormPageComponent,
  PciInputComponent,
} from '@davillawitte/pci-design-system';
import type { PciSelectOption } from '@davillawitte/pci-design-system';
import { Subscription, forkJoin } from 'rxjs';

import { ADMIN_ROUTE_PAGES } from '../../admin-route-pages';
import { AdminApiService } from '../../services/admin-api.service';
import type { CargoListItem, ServidorListItem, SetorListItem, StatusServidor } from '../../models/admin.models';
import { AppFormColDirective, AppFormSectionComponent } from '../../../../shared/form-layout';
import {
  formatCpfDisplay,
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

@Component({
  selector: 'app-servidor-form',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatSelectModule,
    PciAlertComponent,
    PciDatepickerComponent,
    PciFormPageComponent,
    PciInputComponent,
    AppFormSectionComponent,
    AppFormColDirective,
  ],
  templateUrl: './servidor-form.html',
  styleUrl: './servidor-form.scss',
})
export class ServidorForm implements OnInit, OnDestroy {
  private readonly api = inject(AdminApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly feedback = inject(PciFeedbackModalService);
  private readonly injector = inject(Injector);
  private readonly subs = new Subscription();

  readonly routePages = ADMIN_ROUTE_PAGES;
  readonly isEdit = signal(false);
  readonly saving = signal(false);
  readonly loading = signal(false);
  readonly formReady = signal(false);
  readonly error = signal<string | null>(null);
  readonly cargos = signal<CargoListItem[]>([]);
  readonly setores = signal<SetorListItem[]>([]);
  readonly currentPath = signal('/servidores/novo');
  readonly statusOptions = STATUS_OPTIONS;
  readonly maxNascimento = toDateOnlyString(new Date().toISOString())!;

  readonly compareGuid = (a: string, b: string): boolean =>
    (a ?? '').toLowerCase() === (b ?? '').toLowerCase();

  readonly form = this.fb.nonNullable.group({
    nome: ['', Validators.required],
    matricula: ['', matriculaValidator],
    cpf: ['', cpfValidator],
    dataNascimento: ['', Validators.required],
    email: ['', emailFormatValidator],
    telefone: ['', telefoneValidator],
    cargoId: ['', Validators.required],
    setorId: ['', Validators.required],
    status: ['Ativo' as StatusServidor, Validators.required],
  });

  readonly cargoOptions = computed<PciSelectOption[]>(() =>
    this.cargos().map((c) => ({ label: c.nome, value: c.id })),
  );

  readonly setorOptions = computed<PciSelectOption[]>(() =>
    this.setores().map((s) => ({ label: `${s.sigla} — ${s.nome}`, value: s.id })),
  );

  private editId: string | null = null;

  ngOnInit(): void {
    this.editId = this.route.snapshot.paramMap.get('id');
    this.isEdit.set(!!this.editId);
    this.currentPath.set(this.editId ? '/servidores/editar/:id' : '/servidores/novo');

    this.bindMask('matricula', maskMatricula);
    this.bindMask('cpf', maskCpf);
    this.bindMask('telefone', maskTelefone);

    if (this.editId) {
      this.loading.set(true);
      forkJoin({
        cargos: this.api.listCargos(),
        setores: this.api.listSetores(),
        servidor: this.api.getServidor(this.editId),
      }).subscribe({
        next: ({ cargos, setores, servidor }) => {
          this.cargos.set(cargos);
          this.setores.set(setores);
          this.formReady.set(true);
          this.loading.set(false);
          afterNextRender(() => this.patchServidorForm(servidor), { injector: this.injector });
        },
        error: () => {
          this.error.set('Não foi possível carregar o servidor.');
          this.loading.set(false);
        },
      });
      return;
    }

    this.formReady.set(true);
    this.api.listCargos().subscribe({ next: (items) => this.cargos.set(items) });
    this.api.listSetores().subscribe({ next: (items) => this.setores.set(items) });
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
      setorId: value.setorId,
      status: value.status,
    };

    if (this.isEdit() && this.editId) {
      this.api.updateServidor(this.editId, payload).subscribe({
        next: () => {
          this.feedback.showSuccess('Servidor atualizado com sucesso.');
          void this.router.navigateByUrl('/servidores');
        },
        error: (err: { error?: { message?: string } }) => this.fail(err.error?.message),
      });
      return;
    }

    this.api.createServidor(payload).subscribe({
      next: () => {
        this.feedback.showSuccess('Servidor criado com sucesso.');
        void this.router.navigateByUrl('/servidores');
      },
      error: (err: { error?: { message?: string } }) => this.fail(err.error?.message),
    });
  }

  cancel(): void {
    void this.router.navigateByUrl('/servidores');
  }

  private patchServidorForm(servidor: ServidorListItem): void {
    this.form.patchValue(
      {
        nome: servidor.nome,
        matricula: maskMatricula(servidor.matricula),
        cpf: formatCpfDisplay(servidor.cpf),
        dataNascimento: servidor.dataNascimento || '',
        email: servidor.email ?? '',
        telefone: servidor.telefone ? maskTelefone(servidor.telefone) : '',
        cargoId: servidor.cargoId,
        setorId: servidor.setorId,
        status: servidor.status,
      },
      { emitEvent: false },
    );
    // Força CVAs (pci-input / pci-datepicker) a sincronizar após render.
    this.form.setValue(this.form.getRawValue(), { emitEvent: false });
  }

  private bindMask(
    controlName: 'matricula' | 'cpf' | 'telefone',
    maskFn: (v: string) => string,
  ): void {
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

  private fail(message?: string): void {
    this.error.set(message ?? 'Operação não concluída.');
    this.saving.set(false);
  }
}

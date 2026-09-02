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
import { toObservable } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSelectModule } from '@angular/material/select';
import { ActivatedRoute, Router } from '@angular/router';
import {
  PciAlertComponent,
  PciDatepickerComponent,
  PciFeedbackModalService,
  PciFormPageComponent,
  PciInputComponent,
  PciSelectComponent,
} from '@davillawitte/pci-design-system';
import type { PciSelectOption } from '@davillawitte/pci-design-system';
import { Subscription, filter, forkJoin, take } from 'rxjs';

import { AuthService } from '../../../../core/auth/auth.service';
import { openConfirmDialog } from '../../../../shared/dialogs/dialog.helpers';
import { ADMIN_ROUTE_PAGES } from '../../admin-route-pages';
import { AdminApiService } from '../../services/admin-api.service';
import { UsuarioDialog } from '../../components/usuario-dialog/usuario-dialog';
import type {
  CargoListItem,
  NucleoListItem,
  ServidorListItem,
  SetorListItem,
  StatusServidor,
} from '../../models/admin.models';
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

@Component({
  selector: 'app-servidor-form',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatSelectModule,
    PciAlertComponent,
    PciDatepickerComponent,
    PciFormPageComponent,
    PciInputComponent,
    PciSelectComponent,
    AppFormSectionComponent,
    AppFormColDirective,
  ],
  templateUrl: './servidor-form.html',
  styleUrl: './servidor-form.scss',
})
export class ServidorForm implements OnInit, OnDestroy {
  private readonly api = inject(AdminApiService);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly feedback = inject(PciFeedbackModalService);
  private readonly injector = inject(Injector);
  private readonly dialog = inject(MatDialog);
  private readonly subs = new Subscription();
  /** `PciFeedbackModalService.showSuccess()` não devolve um jeito de saber quando o usuário
   * fechou o modal (nem `Observable`/`Promise`) — workaround local até a lib expor isso de
   * verdade (ver docs/design-system-pendencias.md). `close()` da lib só marca `open: false`,
   * nunca volta o `state` pra `null`. */
  private readonly feedbackClosed$ = toObservable(this.feedback.state, { injector: this.injector });

  readonly routePages = ADMIN_ROUTE_PAGES;
  readonly isEdit = signal(false);
  readonly saving = signal(false);
  readonly loading = signal(false);
  readonly formReady = signal(false);
  readonly error = signal<string | null>(null);
  readonly cargos = signal<CargoListItem[]>([]);
  readonly setores = signal<SetorListItem[]>([]);
  readonly nucleos = signal<NucleoListItem[]>([]);
  readonly currentPath = signal('/servidores/novo');
  readonly statusOptions = STATUS_OPTIONS;
  readonly lotacaoOptions = LOTACAO_OPTIONS;
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

  readonly setorOptions = computed<PciSelectOption[]>(() => {
    const permission = this.isEdit() ? 'servidores.editar' : 'servidores.criar';
    return this.setores()
      .filter((s) => this.auth.canAccess(permission, s.id))
      .map((s) => ({ label: `${s.sigla} — ${s.nome}`, value: s.id }));
  });

  private editId: string | null = null;

  ngOnInit(): void {
    this.editId = this.route.snapshot.paramMap.get('id');
    this.isEdit.set(!!this.editId);
    this.currentPath.set(this.editId ? '/servidores/editar/:id' : '/servidores/novo');

    this.bindMask('matricula', maskMatricula);
    this.bindMask('cpf', maskCpf);
    this.bindMask('telefone', maskTelefone);
    this.bindLotacaoTipo();

    if (this.editId) {
      this.loading.set(true);
      forkJoin({
        cargos: this.api.listCargos(),
        setores: this.api.listSetores(),
        nucleos: this.api.listNucleos(),
        servidor: this.api.getServidor(this.editId),
      }).subscribe({
        next: ({ cargos, setores, nucleos, servidor }) => {
          this.cargos.set(cargos);
          this.setores.set(setores);
          this.nucleos.set(nucleos);
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
    this.api.listNucleos().subscribe({ next: (items) => this.nucleos.set(items) });
  }

  /** Alterna obrigatoriedade de setor/núcleo conforme o tipo de lotação escolhido, e
   * limpa o campo que deixou de ser usado (invariante do domínio: um ou outro, não os dois). */
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
      next: (created) => {
        this.saving.set(false);
        this.feedback.showSuccess('Servidor criado com sucesso.');
        if (!this.auth.hasPermission('usuarios.criar')) {
          void this.router.navigateByUrl('/servidores');
          return;
        }
        // Só abre o próximo modal depois que o usuário fechar o de sucesso — evita os dois
        // ficarem visíveis ao mesmo tempo (um por cima do outro).
        this.feedbackClosed$
          .pipe(
            filter((state) => state !== null && state.open === false),
            take(1),
          )
          .subscribe(() => this.perguntarCriarUsuario(created));
      },
      error: (err: { error?: { message?: string } }) => this.fail(err.error?.message),
    });
  }

  cancel(): void {
    void this.router.navigateByUrl('/servidores');
  }

  /** Depois de criar um servidor, oferece cadastrar o usuário dele na hora — evita ter que
   * abrir a listagem de usuários e procurar o servidor de novo. Sempre volta pra listagem de
   * servidores ao final, tenha o admin criado o usuário ou não. */
  private perguntarCriarUsuario(created: ServidorListItem): void {
    openConfirmDialog(this.dialog, {
      title: 'Cadastrar usuário?',
      message: `Deseja cadastrar um usuário de acesso para ${created.nome} agora?`,
      confirmLabel: 'Sim',
      cancelLabel: 'Não',
      icon: 'user-plus',
    }).subscribe((sim) => {
      if (!sim) {
        void this.router.navigateByUrl('/servidores');
        return;
      }
      this.dialog
        .open(UsuarioDialog, {
          data: { servidorId: created.id, servidorLabel: created.nome },
          width: '640px',
          maxWidth: '95vw',
          panelClass: 'pci-app-dialog-panel',
        })
        .afterClosed()
        .subscribe(() => void this.router.navigateByUrl('/servidores'));
    });
  }

  private patchServidorForm(servidor: ServidorListItem): void {
    const lotacaoTipo: LotacaoTipo = servidor.nucleoId ? 'nucleo' : 'setor';
    this.lotacaoTipo.set(lotacaoTipo);
    if (lotacaoTipo === 'setor') {
      this.form.controls.setorId.setValidators(Validators.required);
      this.form.controls.nucleoId.clearValidators();
    } else {
      this.form.controls.nucleoId.setValidators(Validators.required);
      this.form.controls.setorId.clearValidators();
    }

    this.form.patchValue(
      {
        nome: servidor.nome,
        matricula: maskMatricula(servidor.matricula),
        cpf: formatCpfDisplay(servidor.cpf),
        dataNascimento: servidor.dataNascimento || '',
        email: servidor.email ?? '',
        telefone: servidor.telefone ? maskTelefone(servidor.telefone) : '',
        cargoId: servidor.cargoId,
        lotacaoTipo,
        setorId: servidor.setorId ?? '',
        nucleoId: servidor.nucleoId ?? '',
        status: servidor.status,
      },
      { emitEvent: false },
    );
    this.form.controls.setorId.updateValueAndValidity({ emitEvent: false });
    this.form.controls.nucleoId.updateValueAndValidity({ emitEvent: false });
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

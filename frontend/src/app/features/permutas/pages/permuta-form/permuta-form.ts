import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import {
  PciAlertComponent,
  PciDatepickerComponent,
  PciFeedbackModalService,
  PciFormPageComponent,
  PciIconComponent,
  PciInputComponent,
  PciSelectComponent,
} from '@davillawitte/pci-design-system';
import type { PciSelectOption } from '@davillawitte/pci-design-system';

import { maskMatricula } from '../../../../shared/input-masks';
import { PermutaDias } from '../../components/permuta-dias/permuta-dias';
import type { PermutaEscalaServidor, PermutaServidorOpcao } from '../../models/permutas.models';
import { PERMUTAS_ROUTE_PAGES } from '../../permutas.routes.meta';
import { PermutasApiService } from '../../services/permutas-api.service';

type Lado = 'solicitante' | 'permutante';

interface EstadoLado {
  ano: number;
  mes: number;
  escala: PermutaEscalaServidor | null;
  loading: boolean;
}

function mesAtual(): { ano: number; mes: number } {
  const d = new Date();
  return { ano: d.getFullYear(), mes: d.getMonth() + 1 };
}

/** dd/mm, para o resumo da troca. */
function fmtDiaMes(iso: string): string {
  const [, m, d] = iso.slice(0, 10).split('-');
  return `${d}/${m}`;
}

/**
 * Cadastro de permuta: à esquerda a escala do solicitante (clique no dia = ida), à direita a do
 * permutante (clique no dia = volta). As datas também podem ser digitadas — inclusive de mês que
 * ainda não tem escala; a permuta é aplicada quando a escala for criada.
 */
@Component({
  selector: 'app-permuta-form',
  imports: [
    ReactiveFormsModule,
    PciAlertComponent,
    PciDatepickerComponent,
    PciFormPageComponent,
    PciIconComponent,
    PciInputComponent,
    PciSelectComponent,
    PermutaDias,
  ],
  templateUrl: './permuta-form.html',
  styleUrl: './permuta-form.scss',
})
export class PermutaForm implements OnInit {
  private readonly api = inject(PermutasApiService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly feedback = inject(PciFeedbackModalService);
  private readonly destroyRef = inject(DestroyRef);

  readonly routePages = PERMUTAS_ROUTE_PAGES;
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  private readonly solicitantes = signal<PermutaServidorOpcao[]>([]);
  private readonly permutantes = signal<PermutaServidorOpcao[]>([]);

  readonly lados = {
    solicitante: signal<EstadoLado>({ ...mesAtual(), escala: null, loading: false }),
    permutante: signal<EstadoLado>({ ...mesAtual(), escala: null, loading: false }),
  };

  readonly form = this.fb.nonNullable.group({
    solicitanteServidorId: ['', Validators.required],
    permutanteServidorId: ['', Validators.required],
    dataIda: ['', Validators.required],
    dataVolta: ['', Validators.required],
    observacao: [''],
  });

  private readonly valor = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });
  readonly solicitanteId = computed(() => this.valor().solicitanteServidorId ?? '');
  readonly permutanteId = computed(() => this.valor().permutanteServidorId ?? '');
  readonly dataIda = computed(() => (this.valor().dataIda ?? '').slice(0, 10) || null);
  readonly dataVolta = computed(() => (this.valor().dataVolta ?? '').slice(0, 10) || null);

  readonly solicitanteOptions = computed(() => this.toOptions(this.solicitantes()));
  readonly permutanteOptions = computed(() =>
    this.toOptions(this.permutantes().filter((s) => s.id !== this.solicitanteId())),
  );

  readonly resumoIda = computed(() =>
    this.resumo('solicitante', this.solicitanteId(), this.permutanteId(), this.dataIda()),
  );
  readonly resumoVolta = computed(() =>
    this.resumo('permutante', this.permutanteId(), this.solicitanteId(), this.dataVolta()),
  );

  ngOnInit(): void {
    this.api.servidores().subscribe({
      next: (r) => {
        this.solicitantes.set(r.solicitantes);
        this.permutantes.set(r.permutantes);
      },
      error: () => this.error.set('Não foi possível carregar os servidores.'),
    });

    const c = this.form.controls;
    c.solicitanteServidorId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.carregar('solicitante'));
    c.permutanteServidorId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.carregar('permutante'));
    // Data digitada em outro mês: a lista daquele lado acompanha.
    c.dataIda.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((v) => this.irParaMesDaData('solicitante', v));
    c.dataVolta.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((v) => this.irParaMesDaData('permutante', v));
  }

  onDia(lado: Lado, data: string): void {
    const control = lado === 'solicitante' ? this.form.controls.dataIda : this.form.controls.dataVolta;
    control.setValue(data);
    control.markAsTouched();
  }

  onMes(lado: Lado, ref: { ano: number; mes: number }): void {
    this.lados[lado].update((s) => ({ ...s, ...ref }));
    this.carregar(lado);
  }

  save(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.error.set('Informe o solicitante, o permutante, a data da ida e a data da volta.');
      return;
    }

    const v = this.form.getRawValue();
    this.saving.set(true);
    this.error.set(null);
    this.api
      .create({
        solicitanteServidorId: v.solicitanteServidorId,
        permutanteServidorId: v.permutanteServidorId,
        dataIda: v.dataIda.slice(0, 10),
        dataVolta: v.dataVolta.slice(0, 10),
        observacao: v.observacao.trim() || null,
      })
      .subscribe({
        next: () => {
          this.feedback.showSuccess('Permuta registrada com sucesso.');
          void this.router.navigateByUrl('/permutas');
        },
        error: (err: { error?: { message?: string } }) => {
          this.error.set(err.error?.message ?? 'Não foi possível registrar a permuta.');
          this.saving.set(false);
        },
      });
  }

  cancel(): void {
    void this.router.navigateByUrl('/permutas');
  }

  private carregar(lado: Lado): void {
    const servidorId =
      lado === 'solicitante'
        ? this.form.controls.solicitanteServidorId.value
        : this.form.controls.permutanteServidorId.value;
    const estado = this.lados[lado];
    if (!servidorId) {
      estado.update((s) => ({ ...s, escala: null, loading: false }));
      return;
    }

    const { ano, mes } = estado();
    estado.update((s) => ({ ...s, loading: true }));
    this.api.escalaServidor(servidorId, ano, mes).subscribe({
      next: (escala) => {
        // Resposta atrasada de um mês que já mudou não sobrescreve o atual.
        const atual = estado();
        if (atual.ano === ano && atual.mes === mes) {
          estado.set({ ...atual, escala, loading: false });
        }
      },
      error: () => {
        estado.update((s) => ({ ...s, escala: null, loading: false }));
        this.error.set('Não foi possível carregar a escala do servidor.');
      },
    });
  }

  private irParaMesDaData(lado: Lado, valor: string | null | undefined): void {
    const iso = (valor ?? '').slice(0, 10);
    if (!/^\d{4}-\d{2}-\d{2}$/.test(iso)) return;
    const [ano, mes] = iso.split('-').map(Number);
    const atual = this.lados[lado]();
    if (atual.ano !== ano || atual.mes !== mes) {
      this.onMes(lado, { ano, mes });
    }
  }

  /** Ex.: "FULANO (01/09 PD) → BELTRANO". A sigla vem da escala de quem passa o dia; sem
   * escala no mês, sai só a data. */
  private resumo(lado: Lado, deId: string, paraId: string, data: string | null): string {
    if (!deId || !paraId || !data) return '';
    const todos = [...this.solicitantes(), ...this.permutantes()];
    const nome = (id: string) => todos.find((s) => s.id === id)?.nome ?? '';
    const codigo = this.lados[lado]().escala?.dias.find((d) => d.data === data)?.tipoOcorrenciaCodigo;
    const dia = codigo ? `${fmtDiaMes(data)} ${codigo}` : fmtDiaMes(data);
    return `${nome(deId)} (${dia}) → ${nome(paraId)}`;
  }

  private toOptions(servidores: PermutaServidorOpcao[]): PciSelectOption[] {
    return servidores.map((s) => ({
      label: `${s.nome} — ${maskMatricula(s.matricula)}${s.lotacao ? ` (${s.lotacao})` : ''}`,
      value: s.id,
    }));
  }
}

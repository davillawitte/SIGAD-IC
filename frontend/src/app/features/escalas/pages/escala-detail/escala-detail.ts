import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';
import {
  PciAlertComponent,
  PciBadgeComponent,
  PciButtonComponent,
  PciDropdownMenuComponent,
  PciDropdownPanelDirective,
  PciDropdownTriggerDirective,
  PciFeedbackModalService,
  PciPageHeaderComponent,
  PciStackComponent,
  PciTabComponent,
  PciTabsComponent,
  PciToastService,
} from '@davillawitte/pci-design-system';
import { filter, switchMap } from 'rxjs/operators';

import { AuthService } from '../../../../core/auth/auth.service';
import {
  openConfirmDialog,
  openConflitosDialog,
  openPromptDialog,
} from '../../../../shared/dialogs/dialog.helpers';
import { EscalaMatrix, MES_NOMES } from '../../components/escala-matrix/escala-matrix';
import { EscalasApiService } from '../../services/escalas-api.service';
import type { EscalaDetail as EscalaDetailDto, StatusEscala } from '../../models/escalas.models';
import { statusEscalaLabel } from '../../models/escalas.models';

type ViewTab = 'matriz' | 'vertical';

function isDirecaoIcSigla(sigla: string | null | undefined): boolean {
  const n = (sigla ?? '')
    .trim()
    .toLowerCase()
    .normalize('NFD')
    .replace(/\p{M}/gu, '');
  return n === 'direcao ic';
}

@Component({
  selector: 'app-escala-detail',
  imports: [
    CommonModule,
    MatDialogModule,
    PciAlertComponent,
    PciBadgeComponent,
    PciButtonComponent,
    PciDropdownMenuComponent,
    PciDropdownPanelDirective,
    PciDropdownTriggerDirective,
    PciPageHeaderComponent,
    PciStackComponent,
    PciTabComponent,
    PciTabsComponent,
    EscalaMatrix,
  ],
  templateUrl: './escala-detail.html',
  styleUrl: './escala-detail.scss',
})
export class EscalaDetail implements OnInit {
  private readonly api = inject(EscalasApiService);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly feedback = inject(PciFeedbackModalService);
  private readonly toast = inject(PciToastService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly escala = signal<EscalaDetailDto | null>(null);
  readonly working = signal(false);
  readonly pdfMenuOpen = signal(false);
  readonly statusLabel = statusEscalaLabel;
  readonly listBasePath = signal('/escalas');

  private readonly viewTabOrder: ViewTab[] = ['matriz', 'vertical'];
  readonly viewTab = signal<ViewTab>('matriz');
  readonly viewTabIndex = computed(() => Math.max(0, this.viewTabOrder.indexOf(this.viewTab())));

  readonly pageTitle = computed(() => {
    const e = this.escala();
    if (!e) return 'Detalhes da Escala';
    return `Escala — ${MES_NOMES[e.mes] ?? e.mes}/${e.ano}`;
  });

  readonly mesNome = computed(() => {
    const e = this.escala();
    if (!e) return '—';
    return MES_NOMES[e.mes] ?? String(e.mes);
  });

  readonly unidadeLabel = computed(() => {
    const e = this.escala();
    return e?.setorNome ?? e?.nucleoNome ?? '';
  });

  ngOnInit(): void {
    const escopo = (this.route.snapshot.data['escopo'] as string | undefined) ?? 'setor';
    this.listBasePath.set(escopo === 'institucional' ? '/escalas-institucionais' : '/escalas');
    this.reload();
  }

  onViewTabIndexChange(index: number): void {
    const tab = this.viewTabOrder[index];
    if (tab) this.viewTab.set(tab);
  }

  canEdit(): boolean {
    const e = this.escala();
    return this.auth.canAccessEscala('escalas.editar', e?.setorId, e?.nucleoId);
  }

  canPublish(): boolean {
    const e = this.escala();
    return this.auth.canAccessEscala('escalas.publicar', e?.setorId, e?.nucleoId);
  }

  canFinalizar(): boolean {
    const e = this.escala();
    return this.auth.canAccessEscala('escalas.finalizar', e?.setorId, e?.nucleoId);
  }

  canExcluir(): boolean {
    const e = this.escala();
    return this.auth.canAccessEscala('escalas.excluir', e?.setorId, e?.nucleoId);
  }

  canSolicitarDevolucao(): boolean {
    const e = this.escala();
    if (!e || e.status !== 'Publicada') return false;
    if (isDirecaoIcSigla(e.setorSigla)) return false;
    return this.auth.canAccessEscala('escalas.solicitar_devolucao', e.setorId, e.nucleoId);
  }

  canDevolverDireto(): boolean {
    const e = this.escala();
    if (!e || !isDirecaoIcSigla(e.setorSigla)) return false;
    if (e.status !== 'Publicada' && e.status !== 'DevolucaoSolicitada') return false;
    return this.auth.canAccessEscala('escalas.devolver', e.setorId, e.nucleoId);
  }

  canExport(): boolean {
    return (
      this.auth.hasPermission('escalas.exportar') ||
      this.auth.hasPermission('escalas.listar')
    );
  }

  isEditableStatus(status: StatusEscala): boolean {
    return status === 'Rascunho' || status === 'Finalizada';
  }

  statusVariant(status: StatusEscala): 'success' | 'secondary' | 'warning' {
    if (status === 'Publicada') return 'success';
    if (status === 'Finalizada') return 'secondary';
    return 'warning';
  }

  editar(): void {
    const id = this.escala()?.id;
    if (id) void this.router.navigateByUrl(`/escalas/${id}/editar`);
  }

  voltar(): void {
    void this.router.navigateByUrl(this.listBasePath());
  }

  finalizar(): void {
    this.run((id) => this.api.finalizar(id), 'Escala finalizada com sucesso.');
  }

  publicar(): void {
    const id = this.escala()?.id;
    if (!id) return;
    this.working.set(true);
    this.error.set(null);
    this.api.getConflitos(id).subscribe({
      next: (conflitos) => {
        const temCriticos = conflitos.totalCriticos > 0;
        if (!temCriticos) {
          this.run((escalaId) => this.api.publicar(escalaId, false), 'Escala publicada com sucesso.');
          return;
        }
        this.working.set(false);
        openConfirmDialog(this.dialog, {
          title: 'Publicar com conflitos',
          message: `Esta escala possui ${conflitos.totalCriticos} conflito(s) crítico(s). Deseja publicar mesmo assim?`,
          confirmLabel: 'Publicar',
          danger: true,
          onViewDetails: () => {
            openConflitosDialog(this.dialog, { itens: conflitos.itens }).subscribe();
          },
        })
          .pipe(filter(Boolean))
          .subscribe(() =>
            this.run(
              (escalaId) => this.api.publicar(escalaId, true),
              'Escala publicada com sucesso.',
            ),
          );
      },
      error: (err: { error?: { message?: string } }) => {
        this.error.set(err.error?.message ?? 'Não foi possível verificar conflitos da escala.');
        this.working.set(false);
      },
    });
  }

  reabrir(): void {
    this.run((id) => this.api.reabrir(id), 'Escala reaberta como rascunho.');
  }

  solicitarDevolucao(): void {
    openPromptDialog(this.dialog, {
      title: 'Solicitar devolução',
      label: 'Justificativa',
      placeholder: 'Descreva o motivo da devolução',
      confirmLabel: 'Solicitar',
      requiredMessage: 'Informe a justificativa para devolução da escala.',
    })
      .pipe(
        filter((v): v is string => !!v?.trim()),
        switchMap((justificativa) => {
          const id = this.escala()?.id;
          if (!id) throw new Error('Escala inválida');
          this.working.set(true);
          this.error.set(null);
          return this.api.solicitarDevolucao(id, justificativa.trim());
        }),
      )
      .subscribe({
        next: () => {
          this.working.set(false);
          this.feedback.showSuccess('Solicitação de devolução enviada.');
          this.reload();
        },
        error: (err: { error?: { message?: string } }) => {
          this.error.set(err.error?.message ?? 'Não foi possível solicitar a devolução.');
          this.working.set(false);
        },
      });
  }

  devolverDireto(): void {
    openConfirmDialog(this.dialog, {
      title: 'Devolver escala',
      message:
        'Devolver esta escala da Direção do IC para Finalizada? A escala poderá ser editada novamente.',
      confirmLabel: 'Devolver',
      danger: true,
    })
      .pipe(
        filter(Boolean),
        switchMap(() => {
          const id = this.escala()?.id;
          if (!id) throw new Error('Escala inválida');
          this.working.set(true);
          this.error.set(null);
          return this.api.devolver(id);
        }),
      )
      .subscribe({
        next: (escala) => {
          this.escala.set(escala);
          this.working.set(false);
          this.feedback.showSuccess('Escala devolvida para Finalizada.');
        },
        error: (err: { error?: { message?: string } }) => {
          this.error.set(err.error?.message ?? 'Não foi possível devolver a escala.');
          this.working.set(false);
        },
      });
  }

  excluir(): void {
    const id = this.escala()?.id;
    if (!id) return;
    openConfirmDialog(this.dialog, {
      title: 'Excluir escala',
      message: 'Excluir esta escala? Esta ação não pode ser desfeita.',
      confirmLabel: 'Excluir',
      danger: true,
    })
      .pipe(
        filter(Boolean),
        switchMap(() => {
          this.working.set(true);
          this.error.set(null);
          return this.api.delete(id);
        }),
      )
      .subscribe({
        next: () => {
          this.feedback.showSuccess('Escala excluída com sucesso.');
          void this.router.navigateByUrl(this.listBasePath());
        },
        error: (err: { error?: { message?: string } }) => {
          this.error.set(err.error?.message ?? 'Não foi possível excluir a escala.');
          this.working.set(false);
        },
      });
  }

  onExportPdf(layout: 'horizontal' | 'vertical'): void {
    this.pdfMenuOpen.set(false);
    this.downloadPdf(layout);
  }

  downloadPdf(layout: 'horizontal' | 'vertical'): void {
    const id = this.escala()?.id;
    if (!id) return;
    this.api.downloadPdf(id, layout).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `escala-${layout}.pdf`;
        a.click();
        URL.revokeObjectURL(url);
        this.feedback.showSuccess('PDF gerado com sucesso.');
      },
      error: (err: { error?: { message?: string } }) => {
        const msg = err.error?.message ?? 'Falha ao exportar PDF.';
        this.error.set(msg);
        this.toast.showError(msg);
      },
    });
  }

  private run(
    action: (id: string) => ReturnType<EscalasApiService['publicar']>,
    successMessage?: string,
  ): void {
    const id = this.escala()?.id;
    if (!id) return;
    this.working.set(true);
    this.error.set(null);
    action(id).subscribe({
      next: (escala) => {
        this.escala.set(escala);
        this.working.set(false);
        if (successMessage) this.feedback.showSuccess(successMessage);
      },
      error: (err: { error?: { message?: string } }) => {
        this.error.set(err.error?.message ?? 'Operação não concluída.');
        this.working.set(false);
      },
    });
  }

  private reload(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error.set('Escala inválida.');
      this.loading.set(false);
      return;
    }

    this.api.get(id).subscribe({
      next: (escala) => {
        this.escala.set(escala);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Não foi possível carregar a escala.');
        this.loading.set(false);
      },
    });
  }

  fmtDateTime(iso?: string | null): string {
    if (!iso) return '—';
    const date = new Date(iso);
    if (Number.isNaN(date.getTime())) return iso;
    return date.toLocaleString('pt-BR');
  }
}

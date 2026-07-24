import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import type {
  CopiarEscalaPayload,
  CreateEscalaJornadaPayload,
  CreateEscalaPayload,
  EscalaAnteriorInfo,
  EscalaCalendario,
  EscalaCobertura,
  EscalaConflitos,
  EscalaDetail,
  GerarEscalaPayload,
  PadraoEscala,
  PagedEscalas,
  SolicitacaoDevolucaoEscala,
  TipoFuncionamento,
  TipoOcorrencia,
  UpdateEscalaPayload,
  SyncOcorrenciasPayload,
  UpsertOcorrenciaPayload,
  UpsertOcorrenciasLotePayload,
} from '../models/escalas.models';

@Injectable({ providedIn: 'root' })
export class EscalasApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  list(params: {
    setorId?: string;
    mes?: number;
    ano?: number;
    status?: string;
    page?: number;
    pageSize?: number;
    search?: string;
  }): Observable<PagedEscalas> {
    let httpParams = new HttpParams();
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        httpParams = httpParams.set(key, String(value));
      }
    });
    return this.http.get<PagedEscalas>(`${this.base}/api/escalas`, { params: httpParams });
  }

  get(id: string): Observable<EscalaDetail> {
    return this.http.get<EscalaDetail>(`${this.base}/api/escalas/${id}`);
  }

  getCalendario(id: string, servidorId?: string): Observable<EscalaCalendario> {
    let params = new HttpParams();
    if (servidorId) {
      params = params.set('servidorId', servidorId);
    }
    return this.http.get<EscalaCalendario>(`${this.base}/api/escalas/${id}/calendario`, { params });
  }

  getCobertura(id: string): Observable<EscalaCobertura> {
    return this.http.get<EscalaCobertura>(`${this.base}/api/escalas/${id}/cobertura`);
  }

  getConflitos(id: string): Observable<EscalaConflitos> {
    return this.http.get<EscalaConflitos>(`${this.base}/api/escalas/${id}/conflitos`);
  }

  getEscalaAnterior(setorId: string, ano: number, mes: number): Observable<EscalaAnteriorInfo | null> {
    const params = new HttpParams()
      .set('setorId', setorId)
      .set('ano', String(ano))
      .set('mes', String(mes));
    return this.http.get<EscalaAnteriorInfo | null>(`${this.base}/api/escalas/anterior`, { params });
  }

  create(payload: CreateEscalaPayload): Observable<EscalaDetail> {
    return this.http.post<EscalaDetail>(`${this.base}/api/escalas`, payload);
  }

  update(id: string, payload: UpdateEscalaPayload): Observable<EscalaDetail> {
    return this.http.put<EscalaDetail>(`${this.base}/api/escalas/${id}`, payload);
  }

  addServidores(id: string, servidorIds: string[]): Observable<EscalaDetail> {
    return this.http.post<EscalaDetail>(`${this.base}/api/escalas/${id}/servidores`, {
      servidorIds,
    });
  }

  gerar(id: string, payload: GerarEscalaPayload): Observable<EscalaDetail> {
    return this.http.post<EscalaDetail>(`${this.base}/api/escalas/${id}/gerar`, payload);
  }

  aplicarAfastamentos(id: string): Observable<EscalaDetail> {
    return this.http.post<EscalaDetail>(`${this.base}/api/escalas/${id}/aplicar-afastamentos`, {});
  }

  removeServidor(id: string, servidorId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/escalas/${id}/servidores/${servidorId}`);
  }

  addJornada(
    id: string,
    servidorId: string,
    payload: CreateEscalaJornadaPayload,
  ): Observable<EscalaDetail> {
    return this.http.post<EscalaDetail>(
      `${this.base}/api/escalas/${id}/servidores/${servidorId}/jornadas`,
      payload,
    );
  }

  deleteJornada(id: string, jornadaId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/escalas/${id}/jornadas/${jornadaId}`);
  }

  upsertOcorrencia(
    id: string,
    servidorId: string,
    payload: UpsertOcorrenciaPayload,
  ): Observable<EscalaDetail> {
    return this.http.put<EscalaDetail>(
      `${this.base}/api/escalas/${id}/servidores/${servidorId}/ocorrencias`,
      payload,
    );
  }

  upsertOcorrenciasLote(id: string, payload: UpsertOcorrenciasLotePayload): Observable<EscalaDetail> {
    return this.http.post<EscalaDetail>(`${this.base}/api/escalas/${id}/ocorrencias/lote`, payload);
  }

  syncOcorrencias(id: string, payload: SyncOcorrenciasPayload): Observable<EscalaDetail> {
    return this.http.put<EscalaDetail>(`${this.base}/api/escalas/${id}/ocorrencias/sync`, payload);
  }

  deleteOcorrencia(id: string, ocorrenciaId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/escalas/${id}/ocorrencias/${ocorrenciaId}`);
  }

  publicar(id: string, confirmarConflitos = false): Observable<EscalaDetail> {
    return this.http.post<EscalaDetail>(`${this.base}/api/escalas/${id}/publicar`, {
      confirmarConflitos,
    });
  }

  finalizar(id: string): Observable<EscalaDetail> {
    return this.http.post<EscalaDetail>(`${this.base}/api/escalas/${id}/finalizar`, {});
  }

  reabrir(id: string): Observable<EscalaDetail> {
    return this.http.post<EscalaDetail>(`${this.base}/api/escalas/${id}/reabrir`, {});
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/escalas/${id}`);
  }

  solicitarDevolucao(id: string, justificativa: string): Observable<SolicitacaoDevolucaoEscala> {
    return this.http.post<SolicitacaoDevolucaoEscala>(
      `${this.base}/api/escalas/${id}/solicitar-devolucao`,
      { justificativa },
    );
  }

  devolver(id: string): Observable<EscalaDetail> {
    return this.http.post<EscalaDetail>(`${this.base}/api/escalas/${id}/devolver`, {});
  }

  listDevolucoesPendentes(): Observable<SolicitacaoDevolucaoEscala[]> {
    return this.http.get<SolicitacaoDevolucaoEscala[]>(`${this.base}/api/escalas/devolucoes/pendentes`);
  }

  aprovarDevolucao(solicitacaoId: string, observacaoResposta?: string): Observable<SolicitacaoDevolucaoEscala> {
    return this.http.post<SolicitacaoDevolucaoEscala>(
      `${this.base}/api/escalas/devolucoes/${solicitacaoId}/aprovar`,
      { observacaoResposta },
    );
  }

  recusarDevolucao(solicitacaoId: string, observacaoResposta?: string): Observable<SolicitacaoDevolucaoEscala> {
    return this.http.post<SolicitacaoDevolucaoEscala>(
      `${this.base}/api/escalas/devolucoes/${solicitacaoId}/recusar`,
      { observacaoResposta },
    );
  }

  copiar(id: string, payload: CopiarEscalaPayload): Observable<EscalaDetail> {
    return this.http.post<EscalaDetail>(`${this.base}/api/escalas/${id}/copiar`, payload);
  }

  listTiposOcorrencia(): Observable<TipoOcorrencia[]> {
    return this.http.get<TipoOcorrencia[]>(`${this.base}/api/tipos-ocorrencia`);
  }

  listPadroes(tipoFuncionamento?: TipoFuncionamento): Observable<PadraoEscala[]> {
    let params = new HttpParams();
    if (tipoFuncionamento) {
      params = params.set('tipoFuncionamento', tipoFuncionamento);
    }
    return this.http.get<PadraoEscala[]>(`${this.base}/api/padroes-escala`, { params });
  }

  pdfUrl(id: string, layout: 'horizontal' | 'vertical'): string {
    return `${this.base}/api/escalas/${id}/pdf?layout=${layout}`;
  }

  downloadPdf(id: string, layout: 'horizontal' | 'vertical'): Observable<Blob> {
    return this.http.get(this.pdfUrl(id, layout), { responseType: 'blob' });
  }
}

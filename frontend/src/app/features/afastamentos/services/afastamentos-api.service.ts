import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';

/** Servidor lotado em setor traz setorId/setorNome/setorSigla; lotado direto no núcleo traz
 * nucleoId/nucleoNome/nucleoSigla — nunca os dois. */
export interface AfastamentoItem {
  id: string;
  servidorId: string;
  servidorNome: string;
  matricula: string;
  setorId?: string | null;
  setorNome?: string | null;
  setorSigla?: string | null;
  nucleoId?: string | null;
  nucleoNome?: string | null;
  nucleoSigla?: string | null;
  dataInicio: string;
  dataFim: string;
  tipoOcorrenciaCodigo: string;
  tipoOcorrenciaNome: string;
  observacao?: string | null;
  sei?: string | null;
  createdAt: string;
}

export interface CreateAfastamentoPayload {
  servidorId: string;
  dataInicio: string;
  dataFim: string;
  tipoOcorrenciaCodigo: string;
  observacao?: string | null;
  sei?: string | null;
}

export type UpdateAfastamentoPayload = Omit<CreateAfastamentoPayload, 'servidorId'>;

@Injectable({ providedIn: 'root' })
export class AfastamentosApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  list(params: {
    setorId?: string;
    servidorId?: string;
    ano?: number;
    mes?: number;
    tipoOcorrenciaCodigo?: string;
    servidorIds?: string[];
    escopo?: 'setor' | 'institucional';
  } = {}): Observable<AfastamentoItem[]> {
    let httpParams = new HttpParams();
    if (params.setorId) httpParams = httpParams.set('setorId', params.setorId);
    if (params.servidorId) httpParams = httpParams.set('servidorId', params.servidorId);
    if (params.ano) httpParams = httpParams.set('ano', String(params.ano));
    if (params.mes) httpParams = httpParams.set('mes', String(params.mes));
    if (params.tipoOcorrenciaCodigo) {
      httpParams = httpParams.set('tipoOcorrenciaCodigo', params.tipoOcorrenciaCodigo);
    }
    for (const id of params.servidorIds ?? []) {
      httpParams = httpParams.append('servidorIds', id);
    }
    const path =
      params.escopo === 'institucional'
        ? 'api/afastamentos/institucionais'
        : params.escopo === 'setor'
          ? 'api/afastamentos/setor'
          : 'api/afastamentos';
    return this.http.get<AfastamentoItem[]>(`${this.base}/${path}`, { params: httpParams });
  }

  get(id: string): Observable<AfastamentoItem> {
    return this.http.get<AfastamentoItem>(`${this.base}/api/afastamentos/${id}`);
  }

  create(payload: CreateAfastamentoPayload): Observable<AfastamentoItem> {
    return this.http.post<AfastamentoItem>(`${this.base}/api/afastamentos`, payload);
  }

  update(id: string, payload: UpdateAfastamentoPayload): Observable<AfastamentoItem> {
    return this.http.put<AfastamentoItem>(`${this.base}/api/afastamentos/${id}`, payload);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/afastamentos/${id}`);
  }
}

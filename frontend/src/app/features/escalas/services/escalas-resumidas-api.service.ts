import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import { environment } from '../../../../environments/environment';
import type {
  AtualizarEquipePayload,
  ConfigurarEquipePayload,
  ConfigurarRotacaoPayload,
  ConfigurarSetoresPayload,
  CopiarEscalaResumidaPayload,
  CreateEscalaResumidaPayload,
  EscalaResumidaAnteriorInfo,
  EscalaResumidaDetail,
  EscalaResumidaServidorElegivel,
  PagedEscalasResumidas,
  UpdateEscalaResumidaPayload,
  UpsertDiaPayload,
} from '../models/escalas-resumidas.models';

/** O backend omite propriedades `null` do JSON (`DefaultIgnoreCondition.WhenWritingNull`,
 * global) — o grupo "Agentes" chega então SEM a chave `setorId` (não `"setorId": null`), o
 * que o HttpClient desserializa como `undefined`, não `null`. O resto do app compara
 * `setorId` com `null` explicitamente (`Set`, `===`) pra distinguir Agentes de um setor real,
 * então normaliza aqui, uma vez, na borda — depois disso o app inteiro pode confiar que
 * `setorId` é sempre `string` ou `null`, nunca `undefined`. */
function normalizeEscalaResumidaDetail(detail: EscalaResumidaDetail): EscalaResumidaDetail {
  return { ...detail, setores: detail.setores.map((s) => ({ ...s, setorId: s.setorId ?? null })) };
}

@Injectable({ providedIn: 'root' })
export class EscalasResumidasApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  list(params: {
    nucleoId?: string;
    setorId?: string;
    mes?: number;
    ano?: number;
    status?: string;
    page?: number;
    pageSize?: number;
    search?: string;
  }): Observable<PagedEscalasResumidas> {
    let httpParams = new HttpParams();
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        httpParams = httpParams.set(key, String(value));
      }
    });
    return this.http.get<PagedEscalasResumidas>(`${this.base}/api/escalas-resumidas`, {
      params: httpParams,
    });
  }

  get(id: string): Observable<EscalaResumidaDetail> {
    return this.http
      .get<EscalaResumidaDetail>(`${this.base}/api/escalas-resumidas/${id}`)
      .pipe(map(normalizeEscalaResumidaDetail));
  }

  getAnterior(
    container: { nucleoId: string } | { setorId: string },
    ano: number,
    mes: number,
  ): Observable<EscalaResumidaAnteriorInfo | null> {
    let params = new HttpParams().set('ano', String(ano)).set('mes', String(mes));
    params =
      'setorId' in container
        ? params.set('setorId', container.setorId)
        : params.set('nucleoId', container.nucleoId);
    return this.http.get<EscalaResumidaAnteriorInfo | null>(`${this.base}/api/escalas-resumidas/anterior`, {
      params,
    });
  }

  listServidoresElegiveis(
    container: { nucleoId: string } | { setorId: string },
  ): Observable<EscalaResumidaServidorElegivel[]> {
    const params =
      'setorId' in container
        ? new HttpParams().set('setorId', container.setorId)
        : new HttpParams().set('nucleoId', container.nucleoId);
    return this.http.get<EscalaResumidaServidorElegivel[]>(
      `${this.base}/api/escalas-resumidas/servidores-elegiveis`,
      { params },
    );
  }

  create(payload: CreateEscalaResumidaPayload): Observable<EscalaResumidaDetail> {
    return this.http
      .post<EscalaResumidaDetail>(`${this.base}/api/escalas-resumidas`, payload)
      .pipe(map(normalizeEscalaResumidaDetail));
  }

  update(id: string, payload: UpdateEscalaResumidaPayload): Observable<EscalaResumidaDetail> {
    return this.http
      .put<EscalaResumidaDetail>(`${this.base}/api/escalas-resumidas/${id}`, payload)
      .pipe(map(normalizeEscalaResumidaDetail));
  }

  configurarSetores(id: string, payload: ConfigurarSetoresPayload): Observable<EscalaResumidaDetail> {
    return this.http
      .post<EscalaResumidaDetail>(`${this.base}/api/escalas-resumidas/${id}/setores`, payload)
      .pipe(map(normalizeEscalaResumidaDetail));
  }

  configurarEquipe(id: string, payload: ConfigurarEquipePayload): Observable<EscalaResumidaDetail> {
    return this.http
      .post<EscalaResumidaDetail>(`${this.base}/api/escalas-resumidas/${id}/equipes`, payload)
      .pipe(map(normalizeEscalaResumidaDetail));
  }

  atualizarEquipe(
    id: string,
    equipeId: string,
    payload: AtualizarEquipePayload,
  ): Observable<EscalaResumidaDetail> {
    return this.http
      .put<EscalaResumidaDetail>(`${this.base}/api/escalas-resumidas/${id}/equipes/${equipeId}`, payload)
      .pipe(map(normalizeEscalaResumidaDetail));
  }

  removerEquipe(id: string, equipeId: string): Observable<EscalaResumidaDetail> {
    return this.http
      .delete<EscalaResumidaDetail>(`${this.base}/api/escalas-resumidas/${id}/equipes/${equipeId}`)
      .pipe(map(normalizeEscalaResumidaDetail));
  }

  configurarRotacao(
    id: string,
    equipeId: string,
    payload: ConfigurarRotacaoPayload,
  ): Observable<EscalaResumidaDetail> {
    return this.http
      .put<EscalaResumidaDetail>(
        `${this.base}/api/escalas-resumidas/${id}/equipes/${equipeId}/rotacao`,
        payload,
      )
      .pipe(map(normalizeEscalaResumidaDetail));
  }

  upsertDia(id: string, equipeId: string, payload: UpsertDiaPayload): Observable<EscalaResumidaDetail> {
    return this.http
      .put<EscalaResumidaDetail>(
        `${this.base}/api/escalas-resumidas/${id}/equipes/${equipeId}/dias`,
        payload,
      )
      .pipe(map(normalizeEscalaResumidaDetail));
  }

  reverterDia(id: string, equipeId: string, data: string): Observable<EscalaResumidaDetail> {
    return this.http
      .delete<EscalaResumidaDetail>(
        `${this.base}/api/escalas-resumidas/${id}/equipes/${equipeId}/dias/${data}`,
      )
      .pipe(map(normalizeEscalaResumidaDetail));
  }

  copiar(id: string, payload: CopiarEscalaResumidaPayload): Observable<EscalaResumidaDetail> {
    return this.http
      .post<EscalaResumidaDetail>(`${this.base}/api/escalas-resumidas/${id}/copiar`, payload)
      .pipe(map(normalizeEscalaResumidaDetail));
  }

  vincularEscala(id: string, escalaId: string): Observable<EscalaResumidaDetail> {
    return this.http
      .put<EscalaResumidaDetail>(`${this.base}/api/escalas-resumidas/${id}/vincular-escala`, { escalaId })
      .pipe(map(normalizeEscalaResumidaDetail));
  }

  finalizar(id: string): Observable<EscalaResumidaDetail> {
    return this.http
      .post<EscalaResumidaDetail>(`${this.base}/api/escalas-resumidas/${id}/finalizar`, {})
      .pipe(map(normalizeEscalaResumidaDetail));
  }

  reabrir(id: string): Observable<EscalaResumidaDetail> {
    return this.http
      .post<EscalaResumidaDetail>(`${this.base}/api/escalas-resumidas/${id}/reabrir`, {})
      .pipe(map(normalizeEscalaResumidaDetail));
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/escalas-resumidas/${id}`);
  }

  pdfUrl(id: string): string {
    return `${this.base}/api/escalas-resumidas/${id}/pdf`;
  }

  downloadPdf(id: string): Observable<Blob> {
    return this.http.get(this.pdfUrl(id), { responseType: 'blob' });
  }
}

import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';

export type TipoMarcacaoCalendario =
  | 'FeriadoNacional'
  | 'FeriadoEstadual'
  | 'FeriadoMunicipal'
  | 'PontoFacultativo'
  | 'EventoInstitucional';

export type OrigemMarcacao = 'Fixo' | 'Calculado' | 'Manual';
export type StatusCalendarioAno = 'Rascunho' | 'Publicado';

export interface MarcacaoItem {
  id: string;
  calendarioAnoId: string;
  tipo: TipoMarcacaoCalendario;
  origem: OrigemMarcacao;
  data: string;
  dataFim?: string | null;
  nome: string;
  descricao?: string | null;
  local?: string | null;
  publicoAlvo?: string | null;
  createdAt: string;
}

export interface CalendarioAnoDetail {
  id: string;
  ano: number;
  status: StatusCalendarioAno;
  publicadoEm?: string | null;
  publicadoPor?: string | null;
  observacao?: string | null;
  createdAt: string;
  marcacoes: MarcacaoItem[];
}

export interface CalendarioAnoResumo {
  id: string;
  ano: number;
  status: StatusCalendarioAno;
  publicadoEm?: string | null;
  totalFeriados: number;
  totalPontosFacultativos: number;
  totalEventos: number;
}

export interface DiaNaoUtil {
  data: string;
  tipo: TipoMarcacaoCalendario;
  nome: string;
}

export interface CriarMarcacaoPayload {
  calendarioAnoId: string;
  tipo: TipoMarcacaoCalendario;
  data: string;
  dataFim?: string | null;
  nome: string;
  descricao?: string | null;
  local?: string | null;
  publicoAlvo?: string | null;
}

export type AtualizarMarcacaoPayload = Omit<CriarMarcacaoPayload, 'calendarioAnoId'>;

@Injectable({ providedIn: 'root' })
export class CalendarioApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  listarAnos(): Observable<CalendarioAnoResumo[]> {
    return this.http.get<CalendarioAnoResumo[]>(`${this.base}/api/calendario/anos`);
  }

  obterAno(ano: number): Observable<CalendarioAnoDetail> {
    return this.http.get<CalendarioAnoDetail>(`${this.base}/api/calendario/anos/${ano}`);
  }

  gerarAno(ano: number): Observable<CalendarioAnoDetail> {
    return this.http.post<CalendarioAnoDetail>(`${this.base}/api/calendario/anos`, { ano });
  }

  atualizarAno(calendarioAnoId: string, observacao: string | null): Observable<CalendarioAnoDetail> {
    return this.http.put<CalendarioAnoDetail>(`${this.base}/api/calendario/anos/${calendarioAnoId}`, {
      observacao,
    });
  }

  publicar(calendarioAnoId: string): Observable<CalendarioAnoDetail> {
    return this.http.post<CalendarioAnoDetail>(
      `${this.base}/api/calendario/anos/${calendarioAnoId}/publicar`,
      {},
    );
  }

  adicionarMarcacao(payload: CriarMarcacaoPayload): Observable<MarcacaoItem> {
    return this.http.post<MarcacaoItem>(`${this.base}/api/calendario/marcacoes`, payload);
  }

  atualizarMarcacao(id: string, payload: AtualizarMarcacaoPayload): Observable<MarcacaoItem> {
    return this.http.put<MarcacaoItem>(`${this.base}/api/calendario/marcacoes/${id}`, payload);
  }

  excluirMarcacao(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/calendario/marcacoes/${id}`);
  }

  excluirAno(calendarioAnoId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/calendario/anos/${calendarioAnoId}`);
  }

  diasNaoUteis(inicio: string, fim: string): Observable<DiaNaoUtil[]> {
    return this.http.get<DiaNaoUtil[]>(`${this.base}/api/calendario/dias-nao-uteis`, {
      params: { inicio, fim },
    });
  }
}

import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import type {
  CreatePermutaPayload,
  Permuta,
  PermutaEscalaServidor,
  PermutaServidores,
} from '../models/permutas.models';

@Injectable({ providedIn: 'root' })
export class PermutasApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/permutas`;

  list(params: { ano?: number; mes?: number; servidorId?: string } = {}): Observable<Permuta[]> {
    let httpParams = new HttpParams();
    if (params.ano) httpParams = httpParams.set('ano', String(params.ano));
    if (params.mes) httpParams = httpParams.set('mes', String(params.mes));
    if (params.servidorId) httpParams = httpParams.set('servidorId', params.servidorId);
    return this.http.get<Permuta[]>(this.base, { params: httpParams });
  }

  servidores(): Observable<PermutaServidores> {
    return this.http.get<PermutaServidores>(`${this.base}/servidores`);
  }

  escalaServidor(servidorId: string, ano: number, mes: number): Observable<PermutaEscalaServidor> {
    const params = new HttpParams()
      .set('servidorId', servidorId)
      .set('ano', String(ano))
      .set('mes', String(mes));
    return this.http.get<PermutaEscalaServidor>(`${this.base}/escala-servidor`, { params });
  }

  create(payload: CreatePermutaPayload): Observable<Permuta> {
    return this.http.post<Permuta>(this.base, payload);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}

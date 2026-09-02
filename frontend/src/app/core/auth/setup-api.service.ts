import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { LoginResponse } from './models/auth-user.model';

export interface SetupStatus {
  needsSetup: boolean;
}

export interface CompleteSetupPayload {
  token: string;
  cpf: string;
  nome: string;
  matricula: string;
  email?: string | null;
  dataNascimento: string;
  senha: string;
}

@Injectable({ providedIn: 'root' })
export class SetupApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  status(): Observable<SetupStatus> {
    return this.http.get<SetupStatus>(`${this.base}/api/setup/status`);
  }

  complete(payload: CompleteSetupPayload): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.base}/api/setup/complete`, payload);
  }
}

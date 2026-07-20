import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import {
  CreatePermissaoPayload,
  CreatePerfilPayload,
  CreateServidorPayload,
  CreateUsuarioPayload,
  DEFAULT_PAGE_SIZE,
  PagedResult,
  PaginationQuery,
  PermissaoItem,
  PerfilDetail,
  PerfilListItem,
  ServidorListItem,
  SetorListItem,
  UpdatePermissaoPayload,
  UpdatePerfilPayload,
  UpdateUsuarioPayload,
  UsuarioDetail,
  UsuarioListItem,
} from '../models/admin.models';

@Injectable({ providedIn: 'root' })
export class AdminApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiUrl;

  listUsuarios(query: PaginationQuery = {}): Observable<PagedResult<UsuarioListItem>> {
    return this.http.get<PagedResult<UsuarioListItem>>(
      `${this.base}/api/usuarios`,
      { params: this.toParams(query) },
    );
  }

  getUsuario(id: string): Observable<UsuarioDetail> {
    return this.http.get<UsuarioDetail>(`${this.base}/api/usuarios/${id}`);
  }

  createUsuario(payload: CreateUsuarioPayload): Observable<UsuarioListItem> {
    return this.http.post<UsuarioListItem>(`${this.base}/api/usuarios`, payload);
  }

  updateUsuario(id: string, payload: UpdateUsuarioPayload): Observable<UsuarioDetail> {
    return this.http.put<UsuarioDetail>(`${this.base}/api/usuarios/${id}`, payload);
  }

  listPerfis(query: PaginationQuery = {}): Observable<PagedResult<PerfilListItem>> {
    return this.http.get<PagedResult<PerfilListItem>>(
      `${this.base}/api/perfis`,
      { params: this.toParams(query) },
    );
  }

  getPerfil(id: string): Observable<PerfilDetail> {
    return this.http.get<PerfilDetail>(`${this.base}/api/perfis/${id}`);
  }

  createPerfil(payload: CreatePerfilPayload): Observable<PerfilDetail> {
    return this.http.post<PerfilDetail>(`${this.base}/api/perfis`, payload);
  }

  updatePerfil(id: string, payload: UpdatePerfilPayload): Observable<PerfilDetail> {
    return this.http.put<PerfilDetail>(`${this.base}/api/perfis/${id}`, payload);
  }

  deletePerfil(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/perfis/${id}`);
  }

  setPerfilPermissoes(id: string, permissaoIds: string[]): Observable<PerfilDetail> {
    return this.http.put<PerfilDetail>(`${this.base}/api/perfis/${id}/permissoes`, { permissaoIds });
  }

  listPermissoes(query: PaginationQuery = {}): Observable<PagedResult<PermissaoItem>> {
    return this.http.get<PagedResult<PermissaoItem>>(
      `${this.base}/api/permissoes`,
      { params: this.toParams(query) },
    );
  }

  getPermissao(id: string): Observable<PermissaoItem> {
    return this.http.get<PermissaoItem>(`${this.base}/api/permissoes/${id}`);
  }

  createPermissao(payload: CreatePermissaoPayload): Observable<PermissaoItem> {
    return this.http.post<PermissaoItem>(`${this.base}/api/permissoes`, payload);
  }

  updatePermissao(id: string, payload: UpdatePermissaoPayload): Observable<PermissaoItem> {
    return this.http.put<PermissaoItem>(`${this.base}/api/permissoes/${id}`, payload);
  }

  deletePermissao(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/permissoes/${id}`);
  }

  listServidores(semUsuario = false): Observable<ServidorListItem[]> {
    const query = semUsuario ? '?semUsuario=true' : '';
    return this.http.get<ServidorListItem[]>(`${this.base}/api/servidores${query}`);
  }

  createServidor(payload: CreateServidorPayload): Observable<ServidorListItem> {
    return this.http.post<ServidorListItem>(`${this.base}/api/servidores`, payload);
  }

  listSetores(): Observable<SetorListItem[]> {
    return this.http.get<SetorListItem[]>(`${this.base}/api/setores`);
  }

  private toParams(query: PaginationQuery): HttpParams {
    let params = new HttpParams()
      .set('page', String(query.page ?? 1))
      .set('pageSize', String(query.pageSize ?? DEFAULT_PAGE_SIZE));

    if (query.search?.trim()) {
      params = params.set('search', query.search.trim());
    }

    return params;
  }
}

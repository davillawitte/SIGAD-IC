import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../../environments/environment';
import {
  DEFAULT_PAGE_SIZE,
  DesativarPerfilPayload,
  PagedResult,
  PaginationQuery,
  PermissaoItem,
  PerfilDetail,
  PerfilExclusaoImpacto,
  PerfilListItem,
  CreateNucleoPayload,
  CreatePerfilPayload,
  CreateServidorPayload,
  CreateSetorPayload,
  CreateUsuarioPayload,
  CargoListItem,
  EstruturaOrganizacional,
  NucleoDetail,
  NucleoListItem,
  ServidorListItem,
  SetorListItem,
  UpdateNucleoPayload,
  UpdatePerfilPayload,
  UpdateServidorPayload,
  UpdateSetorPayload,
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

  getPerfilExclusaoImpacto(id: string): Observable<PerfilExclusaoImpacto> {
    return this.http.get<PerfilExclusaoImpacto>(`${this.base}/api/perfis/${id}/exclusao-impacto`);
  }

  desativarPerfil(id: string, payload: DesativarPerfilPayload = {}): Observable<void> {
    return this.http.post<void>(`${this.base}/api/perfis/${id}/desativar`, payload);
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

  listServidores(semUsuario = false): Observable<ServidorListItem[]> {
    const query = semUsuario ? '?semUsuario=true' : '';
    return this.http.get<ServidorListItem[]>(`${this.base}/api/servidores${query}`);
  }

  getServidor(id: string): Observable<ServidorListItem> {
    return this.http.get<ServidorListItem>(`${this.base}/api/servidores/${id}`);
  }

  createServidor(payload: CreateServidorPayload): Observable<ServidorListItem> {
    return this.http.post<ServidorListItem>(`${this.base}/api/servidores`, payload);
  }

  updateServidor(id: string, payload: UpdateServidorPayload): Observable<ServidorListItem> {
    return this.http.put<ServidorListItem>(`${this.base}/api/servidores/${id}`, payload);
  }

  listSetores(): Observable<SetorListItem[]> {
    return this.http.get<SetorListItem[]>(`${this.base}/api/setores`);
  }

  getSetor(id: string): Observable<SetorListItem> {
    return this.http.get<SetorListItem>(`${this.base}/api/setores/${id}`);
  }

  getEstruturaOrganizacional(): Observable<EstruturaOrganizacional> {
    return this.http.get<EstruturaOrganizacional>(`${this.base}/api/setores/estrutura`);
  }

  createSetor(payload: CreateSetorPayload): Observable<SetorListItem> {
    return this.http.post<SetorListItem>(`${this.base}/api/setores`, payload);
  }

  updateSetor(id: string, payload: UpdateSetorPayload): Observable<SetorListItem> {
    return this.http.put<SetorListItem>(`${this.base}/api/setores/${id}`, payload);
  }

  deleteSetor(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/setores/${id}`);
  }

  listNucleos(): Observable<NucleoListItem[]> {
    return this.http.get<NucleoListItem[]>(`${this.base}/api/nucleos`);
  }

  getNucleo(id: string): Observable<NucleoDetail> {
    return this.http.get<NucleoDetail>(`${this.base}/api/nucleos/${id}`);
  }

  createNucleo(payload: CreateNucleoPayload): Observable<NucleoDetail> {
    return this.http.post<NucleoDetail>(`${this.base}/api/nucleos`, payload);
  }

  updateNucleo(id: string, payload: UpdateNucleoPayload): Observable<NucleoDetail> {
    return this.http.put<NucleoDetail>(`${this.base}/api/nucleos/${id}`, payload);
  }

  deleteNucleo(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/nucleos/${id}`);
  }

  listCargos(): Observable<CargoListItem[]> {
    return this.http.get<CargoListItem[]>(`${this.base}/api/cargos`);
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

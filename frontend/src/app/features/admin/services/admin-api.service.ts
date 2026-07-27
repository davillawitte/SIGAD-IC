import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import { environment } from '../../../../environments/environment';
import {
  DEFAULT_PAGE_SIZE,
  DesativarPerfilPayload,
  PagedResult,
  PaginationQuery,
  PermissaoItem,
  PerfilDetail,
  PerfilExclusaoImpacto,
  ServidorExclusaoImpacto,
  PerfilListItem,
  CreateNucleoPayload,
  CreatePerfilPayload,
  CreateServidorPayload,
  SetPerfilPermissoesPayload,
  ChefiaConflito,
  CreateSetorPayload,
  CreateUsuarioPayload,
  CargoListItem,
  EstruturaOrganizacional,
  SetorChefiaInput,
  NucleoDetail,
  NucleoListItem,
  ResetSenhaResult,
  ServidorListItem,
  SetorListItem,
  UpdateNucleoPayload,
  UpdatePerfilPayload,
  UpdateServidorPayload,
  UpdateSetorPayload,
  UpdateUsuarioPayload,
  UsuarioComSenha,
  UsuarioDetail,
  UsuarioListItem,
} from '../models/admin.models';
import { mapCargoListItem, mapServidorListItem, mapSetorListItem } from './admin-api.mappers';

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

  createUsuario(payload: CreateUsuarioPayload): Observable<UsuarioComSenha> {
    return this.http.post<UsuarioComSenha>(`${this.base}/api/usuarios`, payload);
  }

  updateUsuario(id: string, payload: UpdateUsuarioPayload): Observable<UsuarioDetail> {
    return this.http.put<UsuarioDetail>(`${this.base}/api/usuarios/${id}`, payload);
  }

  resetUsuarioSenha(id: string): Observable<ResetSenhaResult> {
    return this.http.post<ResetSenhaResult>(`${this.base}/api/usuarios/${id}/reset-senha`, {});
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

  setPerfilPermissoes(
    id: string,
    payload: SetPerfilPermissoesPayload | string[],
  ): Observable<PerfilDetail> {
    const body: SetPerfilPermissoesPayload = Array.isArray(payload)
      ? { permissaoIds: payload }
      : payload;
    return this.http.put<PerfilDetail>(`${this.base}/api/perfis/${id}/permissoes`, body);
  }

  listPermissoes(query: PaginationQuery = {}): Observable<PagedResult<PermissaoItem>> {
    return this.http.get<PagedResult<PermissaoItem>>(
      `${this.base}/api/permissoes`,
      { params: this.toParams(query) },
    );
  }

  listServidores(semUsuario = false): Observable<ServidorListItem[]> {
    const query = semUsuario ? '?semUsuario=true' : '';
    return this.http
      .get<unknown[]>(`${this.base}/api/servidores${query}`)
      .pipe(map((items) => items.map(mapServidorListItem)));
  }

  /** Servidores dos setores gerenciados pelo usuário (ou todos se SuperAdmin). */
  listMeusServidores(semUsuario = false): Observable<ServidorListItem[]> {
    const query = semUsuario ? '?semUsuario=true' : '';
    return this.http
      .get<unknown[]>(`${this.base}/api/servidores/meus${query}`)
      .pipe(map((items) => items.map(mapServidorListItem)));
  }

  getServidor(id: string): Observable<ServidorListItem> {
    return this.http
      .get<unknown>(`${this.base}/api/servidores/${id}`)
      .pipe(map(mapServidorListItem));
  }

  createServidor(payload: CreateServidorPayload): Observable<ServidorListItem> {
    return this.http
      .post<unknown>(`${this.base}/api/servidores`, payload)
      .pipe(map(mapServidorListItem));
  }

  updateServidor(id: string, payload: UpdateServidorPayload): Observable<ServidorListItem> {
    return this.http
      .put<unknown>(`${this.base}/api/servidores/${id}`, payload)
      .pipe(map(mapServidorListItem));
  }

  getServidorExclusaoImpacto(id: string): Observable<ServidorExclusaoImpacto> {
    return this.http.get<ServidorExclusaoImpacto>(`${this.base}/api/servidores/${id}/exclusao-impacto`);
  }

  deleteServidor(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/api/servidores/${id}`);
  }

  listSetores(): Observable<SetorListItem[]> {
    return this.http
      .get<unknown[]>(`${this.base}/api/setores`)
      .pipe(map((items) => items.map(mapSetorListItem)));
  }

  /** Setores acessíveis ao usuário (chefia) ou todos se SuperAdmin. */
  listMeusSetores(): Observable<SetorListItem[]> {
    return this.http.get<SetorListItem[]>(`${this.base}/api/setores/meus`);
  }

  getSetor(id: string): Observable<SetorListItem> {
    return this.http.get<SetorListItem>(`${this.base}/api/setores/${id}`);
  }

  getEstruturaOrganizacional(): Observable<EstruturaOrganizacional> {
    return this.http.get<EstruturaOrganizacional>(`${this.base}/api/setores/estrutura`);
  }

  previewChefiasConflitos(payload: {
    setorId?: string | null;
    chefias: SetorChefiaInput[];
  }): Observable<ChefiaConflito[]> {
    return this.http.post<ChefiaConflito[]>(`${this.base}/api/setores/chefias-conflitos`, payload);
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
    return this.http
      .get<unknown[]>(`${this.base}/api/cargos`)
      .pipe(map((items) => items.map(mapCargoListItem)));
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

import type { CargoListItem, ServidorListItem, SetorListItem, StatusServidor } from '../models/admin.models';

function pick<T>(raw: Record<string, unknown>, camel: string, pascal: string): T | undefined {
  return (raw[camel] ?? raw[pascal]) as T | undefined;
}

function normalizeGuid(value: unknown): string {
  return String(value ?? '').toLowerCase();
}

/** Normaliza resposta da API (camelCase ou PascalCase legado). */
export function mapServidorListItem(raw: unknown): ServidorListItem {
  const r = raw as Record<string, unknown>;
  const dataNascimento = String(pick<string>(r, 'dataNascimento', 'DataNascimento') ?? '').slice(0, 10);
  const email = pick<string>(r, 'email', 'Email');

  return {
    id: normalizeGuid(pick(r, 'id', 'Id')),
    nome: String(pick(r, 'nome', 'Nome') ?? ''),
    matricula: String(pick(r, 'matricula', 'Matricula') ?? ''),
    cpf: String(pick(r, 'cpf', 'Cpf') ?? ''),
    cargoId: normalizeGuid(pick(r, 'cargoId', 'CargoId')),
    cargo: String(pick(r, 'cargo', 'Cargo') ?? ''),
    cargoCodigo: String(pick(r, 'cargoCodigo', 'CargoCodigo') ?? ''),
    email: email ?? '',
    telefone: (pick<string | null>(r, 'telefone', 'Telefone') ?? null) || null,
    dataNascimento,
    setorId: normalizeGuid(pick(r, 'setorId', 'SetorId')),
    setorNome: String(pick(r, 'setorNome', 'SetorNome') ?? ''),
    possuiUsuario: Boolean(pick(r, 'possuiUsuario', 'PossuiUsuario')),
    status: (pick(r, 'status', 'Status') ?? 'Ativo') as StatusServidor,
  };
}

export function mapCargoListItem(raw: unknown): CargoListItem {
  const r = raw as Record<string, unknown>;
  return {
    id: normalizeGuid(pick(r, 'id', 'Id')),
    nome: String(pick(r, 'nome', 'Nome') ?? ''),
    codigo: String(pick(r, 'codigo', 'Codigo') ?? ''),
    ativo: Boolean(pick(r, 'ativo', 'Ativo') ?? true),
  };
}

export function mapSetorListItem(raw: unknown): SetorListItem {
  const r = raw as Record<string, unknown>;
  return {
    id: normalizeGuid(pick(r, 'id', 'Id')),
    nome: String(pick(r, 'nome', 'Nome') ?? ''),
    sigla: String(pick(r, 'sigla', 'Sigla') ?? ''),
    resumo: (pick<string | null>(r, 'resumo', 'Resumo') ?? null) || null,
    nucleoId: pick<string | null>(r, 'nucleoId', 'NucleoId') ?? null,
    nucleoNome: (pick<string | null>(r, 'nucleoNome', 'NucleoNome') ?? null) || null,
    isDirecaoIc: Boolean(pick(r, 'isDirecaoIc', 'IsDirecaoIc')),
    chefias: [],
  };
}

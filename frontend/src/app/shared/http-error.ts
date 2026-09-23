/**
 * Mensagem legível de um erro HTTP da API. Os controllers devolvem `{ message }` nas falhas
 * de regra de negócio, mas o ASP.NET também responde com ProblemDetails (validação de modelo:
 * `errors`/`title`), 401/403 sem corpo e 500 sem corpo em exceção não tratada — sem isto a
 * tela mostrava só "Operação não concluída." sem dizer o porquê.
 */
export function httpErrorMessage(err: unknown, fallback = 'Operação não concluída.'): string {
  const e = (err ?? {}) as {
    status?: number;
    error?: {
      message?: unknown;
      detail?: unknown;
      title?: unknown;
      errors?: Record<string, unknown>;
    } | string | null;
  };
  const body = e.error;

  if (typeof body === 'string' && body.trim() && !body.trimStart().startsWith('<')) {
    return body.trim();
  }
  if (body && typeof body === 'object') {
    if (typeof body.message === 'string' && body.message.trim()) return body.message;
    const validacao = Object.values(body.errors ?? {})
      .flatMap((v) => (Array.isArray(v) ? v : [v]))
      .filter((v): v is string => typeof v === 'string' && !!v.trim());
    if (validacao.length) return validacao.join(' ');
    if (typeof body.detail === 'string' && body.detail.trim()) return body.detail;
  }

  switch (e.status) {
    case 0:
      return 'Não foi possível conectar ao servidor. Verifique a conexão e tente novamente.';
    case 401:
      return 'Sua sessão expirou. Entre novamente.';
    case 403:
      return 'Você não tem permissão para esta operação.';
    case 404:
      return 'Registro não encontrado. Ele pode ter sido excluído.';
  }
  if (e.status && e.status >= 500) {
    return `Erro interno no servidor (HTTP ${e.status}). Tente novamente; se persistir, avise o suporte.`;
  }
  return e.status ? `${fallback} (HTTP ${e.status})` : fallback;
}

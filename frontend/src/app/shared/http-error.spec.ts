import { describe, expect, it } from 'vitest';

import { httpErrorMessage } from './http-error';

describe('httpErrorMessage', () => {
  it('usa a mensagem de regra de negócio da API', () => {
    expect(httpErrorMessage({ status: 400, error: { message: 'Já existe escala publicada.' } })).toBe(
      'Já existe escala publicada.',
    );
  });

  it('junta os erros de validação do ProblemDetails', () => {
    const err = {
      status: 400,
      error: { title: 'One or more validation errors occurred.', errors: { Mes: ['Mês inválido.'], Ano: ['Ano inválido.'] } },
    };
    expect(httpErrorMessage(err)).toBe('Mês inválido. Ano inválido.');
  });

  it('explica erro interno em vez da mensagem genérica', () => {
    expect(httpErrorMessage({ status: 500, error: null })).toContain('Erro interno no servidor (HTTP 500)');
  });

  it('trata falta de conexão e falta de permissão', () => {
    expect(httpErrorMessage({ status: 0, error: {} })).toContain('Não foi possível conectar');
    expect(httpErrorMessage({ status: 403, error: null })).toBe('Você não tem permissão para esta operação.');
  });

  it('cai no fallback informado, com o status HTTP', () => {
    expect(httpErrorMessage({ status: 409, error: {} }, 'Falha ao salvar.')).toBe('Falha ao salvar. (HTTP 409)');
    expect(httpErrorMessage(undefined)).toBe('Operação não concluída.');
  });

  it('ignora corpo HTML (página de erro do proxy)', () => {
    expect(httpErrorMessage({ status: 502, error: '<html>Bad Gateway</html>' })).toContain('HTTP 502');
  });
});

/** `dataIda`: dia do solicitante que passa ao permutante. `dataVolta`: dia do permutante que
 * volta ao solicitante. Datas no formato ISO (`yyyy-mm-dd`). */
export interface Permuta {
  id: string;
  solicitanteServidorId: string;
  solicitanteNome: string;
  solicitanteMatricula: string;
  permutanteServidorId: string;
  permutanteNome: string;
  permutanteMatricula: string;
  dataIda: string;
  dataVolta: string;
  observacao?: string | null;
  createdBy?: string | null;
  createdAt: string;
}

export interface CreatePermutaPayload {
  solicitanteServidorId: string;
  permutanteServidorId: string;
  dataIda: string;
  dataVolta: string;
  observacao?: string | null;
}

export interface PermutaServidorOpcao {
  id: string;
  nome: string;
  matricula: string;
  lotacao?: string | null;
}

/** Solicitante: servidores da chefia. Permutante: qualquer servidor ativo. */
export interface PermutaServidores {
  solicitantes: PermutaServidorOpcao[];
  permutantes: PermutaServidorOpcao[];
}

export interface PermutaDia {
  data: string;
  tipoOcorrenciaCodigo?: string | null;
  tipoOcorrenciaNome?: string | null;
  permuta: boolean;
}

/** Dias do servidor no mês; sem escala no mês, `escalaId` vem nulo. */
export interface PermutaEscalaServidor {
  escalaId?: string | null;
  escalaIdentificacao?: string | null;
  status?: string | number | null;
  dias: PermutaDia[];
}

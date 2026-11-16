export type SituacaoTarefa = 'Pendente' | 'EmAndamento' | 'Concluida';

export const situacoesTarefa: ReadonlyArray<{ valor: SituacaoTarefa; rotulo: string }> = [
  { valor: 'Pendente', rotulo: 'Pendente' },
  { valor: 'EmAndamento', rotulo: 'Em andamento' },
  { valor: 'Concluida', rotulo: 'Concluída' },
];

export interface CriarTarefaRequisicao {
  titulo: string;
  descricao: string | null;
  dataVencimento: string;
}

export interface AtualizarTarefaRequisicao extends CriarTarefaRequisicao {
  situacao: SituacaoTarefa;
}

export interface TarefaResposta extends AtualizarTarefaRequisicao {
  id: string;
}

export interface FiltrosTarefa {
  situacao?: SituacaoTarefa;
  dataVencimento?: string;
}

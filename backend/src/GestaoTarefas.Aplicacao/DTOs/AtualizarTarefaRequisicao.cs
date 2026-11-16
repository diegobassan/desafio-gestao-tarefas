using GestaoTarefas.Dominio.Enums;

namespace GestaoTarefas.Aplicacao.DTOs;

public class AtualizarTarefaRequisicao
{
    public required string Titulo { get; init; }
    public string? Descricao { get; init; }
    public required SituacaoTarefa Situacao { get; init; }
    public required DateOnly DataVencimento { get; init; }
}

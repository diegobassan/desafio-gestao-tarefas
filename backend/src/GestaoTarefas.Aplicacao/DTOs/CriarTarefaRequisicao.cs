namespace GestaoTarefas.Aplicacao.DTOs;

public class CriarTarefaRequisicao
{
    public required string Titulo { get; init; }
    public string? Descricao { get; init; }
    public required DateOnly DataVencimento { get; init; }
}

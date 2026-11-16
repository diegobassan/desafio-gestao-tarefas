using GestaoTarefas.Dominio.Enums;

namespace GestaoTarefas.Aplicacao.DTOs;

public record TarefaResposta(
    Guid Id,
    string Titulo,
    string? Descricao,
    SituacaoTarefa Situacao,
    DateOnly DataVencimento);

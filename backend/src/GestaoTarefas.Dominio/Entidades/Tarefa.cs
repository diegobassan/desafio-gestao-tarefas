using GestaoTarefas.Dominio.Enums;

namespace GestaoTarefas.Dominio.Entidades;

public class Tarefa
{
    public Guid Id { get; private set; }
    public string Titulo { get; private set; } = string.Empty;
    public string? Descricao { get; private set; }
    public SituacaoTarefa Situacao { get; private set; }
    public DateOnly DataVencimento { get; private set; }

    public Tarefa(string titulo, string? descricao, DateOnly dataVencimento)
    {
        Atualizar(titulo, descricao, dataVencimento);
        Id = Guid.NewGuid();
        Situacao = SituacaoTarefa.Pendente;
    }

    public void Atualizar(string titulo, string? descricao, DateOnly dataVencimento)
    {
        if (string.IsNullOrWhiteSpace(titulo))
        {
            throw new ArgumentException("O título é obrigatório.", nameof(titulo));
        }

        if (titulo.Length > 150)
        {
            throw new ArgumentException("O título deve ter no máximo 150 caracteres.", nameof(titulo));
        }

        if (descricao?.Length > 2000)
        {
            throw new ArgumentException("A descrição deve ter no máximo 2000 caracteres.", nameof(descricao));
        }

        Titulo = titulo;
        Descricao = descricao;
        DataVencimento = dataVencimento;
    }

    public void AtualizarSituacao(SituacaoTarefa situacao)
    {
        if (!Enum.IsDefined(situacao))
        {
            throw new ArgumentException("A situação da tarefa é inválida.", nameof(situacao));
        }

        Situacao = situacao;
    }
}

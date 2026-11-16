using GestaoTarefas.Dominio.Entidades;
using GestaoTarefas.Dominio.Enums;

namespace GestaoTarefas.Dominio.Repositorios;

public interface ITarefaRepositorio
{
    Task AdicionarAsync(Tarefa tarefa, CancellationToken cancelamento = default);
    Task<Tarefa?> ConsultarPorIdAsync(Guid id, CancellationToken cancelamento = default);
    Task<IReadOnlyList<Tarefa>> ListarAsync(
        SituacaoTarefa? situacao = null,
        DateOnly? dataVencimento = null,
        CancellationToken cancelamento = default);
    Task AtualizarAsync(Tarefa tarefa, CancellationToken cancelamento = default);
    Task RemoverAsync(Tarefa tarefa, CancellationToken cancelamento = default);
}

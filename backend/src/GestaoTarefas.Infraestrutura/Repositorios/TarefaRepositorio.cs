using GestaoTarefas.Dominio.Entidades;
using GestaoTarefas.Dominio.Enums;
using GestaoTarefas.Dominio.Repositorios;
using GestaoTarefas.Infraestrutura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace GestaoTarefas.Infraestrutura.Repositorios;

public class TarefaRepositorio(GestaoTarefasContexto contexto) : ITarefaRepositorio
{
    public async Task AdicionarAsync(Tarefa tarefa, CancellationToken cancelamento = default)
    {
        await contexto.Tarefas.AddAsync(tarefa, cancelamento);
        await contexto.SaveChangesAsync(cancelamento);
    }

    public Task<Tarefa?> ConsultarPorIdAsync(Guid id, CancellationToken cancelamento = default)
    {
        return contexto.Tarefas.SingleOrDefaultAsync(tarefa => tarefa.Id == id, cancelamento);
    }

    public async Task<IReadOnlyList<Tarefa>> ListarAsync(
        SituacaoTarefa? situacao = null,
        DateOnly? dataVencimento = null,
        CancellationToken cancelamento = default)
    {
        var consulta = contexto.Tarefas.AsNoTracking();

        if (situacao.HasValue)
        {
            consulta = consulta.Where(tarefa => tarefa.Situacao == situacao.Value);
        }

        if (dataVencimento.HasValue)
        {
            consulta = consulta.Where(tarefa => tarefa.DataVencimento == dataVencimento.Value);
        }

        return await consulta.ToListAsync(cancelamento);
    }

    public async Task AtualizarAsync(Tarefa tarefa, CancellationToken cancelamento = default)
    {
        contexto.Tarefas.Update(tarefa);
        await contexto.SaveChangesAsync(cancelamento);
    }

    public async Task RemoverAsync(Tarefa tarefa, CancellationToken cancelamento = default)
    {
        contexto.Tarefas.Remove(tarefa);
        await contexto.SaveChangesAsync(cancelamento);
    }
}

using GestaoTarefas.Aplicacao.DTOs;
using GestaoTarefas.Dominio.Entidades;
using GestaoTarefas.Dominio.Enums;
using GestaoTarefas.Dominio.Repositorios;

namespace GestaoTarefas.Aplicacao.Servicos;

public class TarefaServico(ITarefaRepositorio repositorio)
{
    public async Task<TarefaResposta> CadastrarAsync(
        CriarTarefaRequisicao requisicao, CancellationToken cancelamento = default)
    {
        var tarefa = new Tarefa(requisicao.Titulo, requisicao.Descricao, requisicao.DataVencimento);
        await repositorio.AdicionarAsync(tarefa, cancelamento);
        return CriarResposta(tarefa);
    }

    public async Task<TarefaResposta?> ConsultarPorIdAsync(Guid id, CancellationToken cancelamento = default)
    {
        var tarefa = await repositorio.ConsultarPorIdAsync(id, cancelamento);
        return tarefa is null ? null : CriarResposta(tarefa);
    }

    public async Task<IReadOnlyList<TarefaResposta>> ListarAsync(
        SituacaoTarefa? situacao = null,
        DateOnly? dataVencimento = null,
        CancellationToken cancelamento = default)
    {
        if (situacao.HasValue && !Enum.IsDefined(situacao.Value))
        {
            throw new ArgumentException("A situação do filtro é inválida.", nameof(situacao));
        }

        var tarefas = await repositorio.ListarAsync(situacao, dataVencimento, cancelamento);
        return tarefas.Select(CriarResposta).ToList();
    }

    public async Task<bool> AtualizarAsync(
        Guid id, AtualizarTarefaRequisicao requisicao, CancellationToken cancelamento = default)
    {
        var tarefa = await repositorio.ConsultarPorIdAsync(id, cancelamento);
        if (tarefa is null)
        {
            return false;
        }

        tarefa.Atualizar(requisicao.Titulo, requisicao.Descricao, requisicao.DataVencimento);
        tarefa.AtualizarSituacao(requisicao.Situacao);
        await repositorio.AtualizarAsync(tarefa, cancelamento);
        return true;
    }

    public async Task<bool> ExcluirAsync(Guid id, CancellationToken cancelamento = default)
    {
        var tarefa = await repositorio.ConsultarPorIdAsync(id, cancelamento);
        if (tarefa is null)
        {
            return false;
        }

        await repositorio.RemoverAsync(tarefa, cancelamento);
        return true;
    }

    private static TarefaResposta CriarResposta(Tarefa tarefa)
    {
        return new TarefaResposta(tarefa.Id, tarefa.Titulo, tarefa.Descricao, tarefa.Situacao, tarefa.DataVencimento);
    }
}

using System.ComponentModel;
using GestaoTarefas.Aplicacao.DTOs;
using GestaoTarefas.Aplicacao.Servicos;
using GestaoTarefas.Dominio.Enums;
using Microsoft.AspNetCore.Mvc;

namespace GestaoTarefas.Api.Controllers;

[ApiController]
[Route("api/tarefas")]
public class TarefasController(TarefaServico servico) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Lista tarefas com filtros opcionais e combináveis.")]
    [ProducesResponseType<IReadOnlyList<TarefaResposta>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<TarefaResposta>>> Listar(
        [FromQuery, Description("Situação da tarefa: Pendente, EmAndamento ou Concluida.")] SituacaoTarefa? situacao = null,
        [FromQuery, Description("Data exata de vencimento, no formato AAAA-MM-DD.")] DateOnly? dataVencimento = null,
        CancellationToken cancelamento = default)
    {
        return Ok(await servico.ListarAsync(situacao, dataVencimento, cancelamento));
    }

    [HttpGet("{id}")]
    [EndpointSummary("Consulta uma tarefa pelo identificador.")]
    [ProducesResponseType<TarefaResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TarefaResposta>> ConsultarPorId(Guid id, CancellationToken cancelamento)
    {
        var tarefa = await servico.ConsultarPorIdAsync(id, cancelamento);
        return tarefa is null ? NotFound() : Ok(tarefa);
    }

    [HttpPost]
    [EndpointSummary("Cadastra uma tarefa com situação inicial Pendente.")]
    [ProducesResponseType<TarefaResposta>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TarefaResposta>> Cadastrar(
        CriarTarefaRequisicao requisicao, CancellationToken cancelamento)
    {
        var tarefa = await servico.CadastrarAsync(requisicao, cancelamento);
        return CreatedAtAction(nameof(ConsultarPorId), new { id = tarefa.Id }, tarefa);
    }

    [HttpPut("{id}")]
    [EndpointSummary("Atualiza os dados e a situação de uma tarefa.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(
        Guid id, AtualizarTarefaRequisicao requisicao, CancellationToken cancelamento)
    {
        var atualizada = await servico.AtualizarAsync(id, requisicao, cancelamento);
        return atualizada ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    [EndpointSummary("Exclui uma tarefa pelo identificador.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken cancelamento)
    {
        return await servico.ExcluirAsync(id, cancelamento) ? NoContent() : NotFound();
    }
}

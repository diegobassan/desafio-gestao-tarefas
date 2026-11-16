using GestaoTarefas.Aplicacao.DTOs;
using GestaoTarefas.Aplicacao.Servicos;
using GestaoTarefas.Dominio.Entidades;
using GestaoTarefas.Dominio.Enums;
using GestaoTarefas.Dominio.Repositorios;
using Moq;

namespace GestaoTarefas.Testes.Unitarios;

public class TarefaServicoTestes
{
    private readonly Mock<ITarefaRepositorio> repositorio = new(MockBehavior.Strict);
    private readonly TarefaServico servico;

    public TarefaServicoTestes()
    {
        servico = new TarefaServico(repositorio.Object);
    }

    [Fact]
    public async Task DeveCadastrarTarefaComSituacaoPendente()
    {
        using var fonteCancelamento = new CancellationTokenSource();
        var cancelamento = fonteCancelamento.Token;
        var requisicao = new CriarTarefaRequisicao
        {
            Titulo = "Preparar entrevista",
            Descricao = "Revisar o desafio",
            DataVencimento = new DateOnly(2026, 11, 20)
        };
        Tarefa? cadastrada = null;
        repositorio.Setup(repositorio => repositorio.AdicionarAsync(It.IsAny<Tarefa>(), cancelamento))
            .Callback<Tarefa, CancellationToken>((tarefa, _) => cadastrada = tarefa)
            .Returns(Task.CompletedTask);

        var resposta = await servico.CadastrarAsync(requisicao, cancelamento);

        Assert.NotNull(cadastrada);
        Assert.NotEqual(Guid.Empty, resposta.Id);
        Assert.Equal(cadastrada.Id, resposta.Id);
        Assert.Equal(requisicao.Titulo, resposta.Titulo);
        Assert.Equal(requisicao.Descricao, resposta.Descricao);
        Assert.Equal(requisicao.DataVencimento, resposta.DataVencimento);
        Assert.Equal(SituacaoTarefa.Pendente, resposta.Situacao);
        repositorio.Verify(repositorio => repositorio.AdicionarAsync(cadastrada, cancelamento), Times.Once);
    }

    [Fact]
    public async Task DeveConsultarTarefaExistente()
    {
        var tarefa = new Tarefa("Revisar testes", "Verificar os cenários", new DateOnly(2026, 11, 20));
        tarefa.AtualizarSituacao(SituacaoTarefa.EmAndamento);
        repositorio.Setup(repositorio => repositorio.ConsultarPorIdAsync(tarefa.Id, default)).ReturnsAsync(tarefa);

        var resposta = await servico.ConsultarPorIdAsync(tarefa.Id);

        Assert.NotNull(resposta);
        Assert.Equal(new TarefaResposta(tarefa.Id, tarefa.Titulo, tarefa.Descricao, tarefa.Situacao, tarefa.DataVencimento), resposta);
    }

    [Fact]
    public async Task DeveRetornarNuloQuandoTarefaNaoExistir()
    {
        var id = Guid.NewGuid();
        repositorio.Setup(repositorio => repositorio.ConsultarPorIdAsync(id, default)).ReturnsAsync((Tarefa?)null);

        Assert.Null(await servico.ConsultarPorIdAsync(id));
    }

    [Fact]
    public async Task DeveAtualizarTarefaPreservandoIdentificador()
    {
        var tarefa = new Tarefa("Revisar testes", "Descrição inicial", new DateOnly(2026, 11, 20));
        var id = tarefa.Id;
        var requisicao = new AtualizarTarefaRequisicao
        {
            Titulo = "Finalizar testes", Descricao = null,
            Situacao = SituacaoTarefa.Concluida, DataVencimento = new DateOnly(2026, 11, 21)
        };
        repositorio.Setup(repositorio => repositorio.ConsultarPorIdAsync(id, default)).ReturnsAsync(tarefa);
        repositorio.Setup(repositorio => repositorio.AtualizarAsync(tarefa, default)).Returns(Task.CompletedTask);

        Assert.True(await servico.AtualizarAsync(id, requisicao));

        Assert.Equal(id, tarefa.Id);
        Assert.Equal(requisicao.Titulo, tarefa.Titulo);
        Assert.Null(tarefa.Descricao);
        Assert.Equal(requisicao.DataVencimento, tarefa.DataVencimento);
        Assert.Equal(SituacaoTarefa.Concluida, tarefa.Situacao);
        repositorio.Verify(repositorio => repositorio.AtualizarAsync(tarefa, default), Times.Once);
    }

    [Fact]
    public async Task NaoDeveAtualizarTarefaInexistente()
    {
        var id = Guid.NewGuid();
        repositorio.Setup(repositorio => repositorio.ConsultarPorIdAsync(id, default)).ReturnsAsync((Tarefa?)null);
        var requisicao = new AtualizarTarefaRequisicao
        {
            Titulo = "Revisar testes", Situacao = SituacaoTarefa.Concluida,
            DataVencimento = new DateOnly(2026, 11, 20)
        };

        Assert.False(await servico.AtualizarAsync(id, requisicao));
        repositorio.Verify(repositorio => repositorio.AtualizarAsync(It.IsAny<Tarefa>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeveExcluirTarefaExistente()
    {
        var tarefa = new Tarefa("Revisar testes", null, new DateOnly(2026, 11, 20));
        repositorio.Setup(repositorio => repositorio.ConsultarPorIdAsync(tarefa.Id, default)).ReturnsAsync(tarefa);
        repositorio.Setup(repositorio => repositorio.RemoverAsync(tarefa, default)).Returns(Task.CompletedTask);

        Assert.True(await servico.ExcluirAsync(tarefa.Id));

        repositorio.Verify(repositorio => repositorio.RemoverAsync(tarefa, default), Times.Once);
    }

    [Fact]
    public async Task NaoDeveExcluirTarefaInexistente()
    {
        var id = Guid.NewGuid();
        repositorio.Setup(repositorio => repositorio.ConsultarPorIdAsync(id, default)).ReturnsAsync((Tarefa?)null);

        Assert.False(await servico.ExcluirAsync(id));
        repositorio.Verify(repositorio => repositorio.RemoverAsync(It.IsAny<Tarefa>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task NaoDeveCadastrarTarefaComTituloInvalido(string? titulo)
    {
        var requisicao = new CriarTarefaRequisicao { Titulo = titulo!, DataVencimento = new DateOnly(2026, 11, 20) };

        await Assert.ThrowsAsync<ArgumentException>(() => servico.CadastrarAsync(requisicao));
        repositorio.Verify(repositorio => repositorio.AdicionarAsync(It.IsAny<Tarefa>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(151, 0)]
    [InlineData(1, 2001)]
    public async Task NaoDeveCadastrarTextosAcimaDosLimites(int tamanhoTitulo, int tamanhoDescricao)
    {
        var requisicao = new CriarTarefaRequisicao
        {
            Titulo = new string('a', tamanhoTitulo), Descricao = new string('b', tamanhoDescricao),
            DataVencimento = new DateOnly(2026, 11, 20)
        };

        await Assert.ThrowsAsync<ArgumentException>(() => servico.CadastrarAsync(requisicao));
        repositorio.Verify(repositorio => repositorio.AdicionarAsync(It.IsAny<Tarefa>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("", SituacaoTarefa.Concluida)]
    [InlineData("Revisar testes", (SituacaoTarefa)99)]
    public async Task NaoDeveSalvarAtualizacaoComDadosInvalidos(string titulo, SituacaoTarefa situacao)
    {
        var tarefa = new Tarefa("Revisar testes", null, new DateOnly(2026, 11, 20));
        repositorio.Setup(repositorio => repositorio.ConsultarPorIdAsync(tarefa.Id, default)).ReturnsAsync(tarefa);
        var requisicao = new AtualizarTarefaRequisicao
        {
            Titulo = titulo, Situacao = situacao, DataVencimento = tarefa.DataVencimento
        };

        await Assert.ThrowsAsync<ArgumentException>(() => servico.AtualizarAsync(tarefa.Id, requisicao));
        repositorio.Verify(repositorio => repositorio.AtualizarAsync(It.IsAny<Tarefa>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(SituacaoTarefa.EmAndamento, null)]
    [InlineData(null, 20)]
    [InlineData(SituacaoTarefa.EmAndamento, 20)]
    public async Task DeveEncaminharFiltrosEMapearTarefas(SituacaoTarefa? situacao, int? diaVencimento)
    {
        using var fonteCancelamento = new CancellationTokenSource();
        var cancelamento = fonteCancelamento.Token;
        DateOnly? vencimento = diaVencimento.HasValue ? new DateOnly(2026, 11, diaVencimento.Value) : null;
        var tarefa = new Tarefa("Revisar testes", "Verificar filtros", new DateOnly(2026, 11, 20));
        tarefa.AtualizarSituacao(SituacaoTarefa.EmAndamento);
        repositorio.Setup(repositorio => repositorio.ListarAsync(situacao, vencimento, cancelamento))
            .ReturnsAsync(new List<Tarefa> { tarefa });

        var respostas = await servico.ListarAsync(situacao, vencimento, cancelamento);

        var resposta = Assert.Single(respostas);
        Assert.Equal(new TarefaResposta(tarefa.Id, tarefa.Titulo, tarefa.Descricao, tarefa.Situacao, tarefa.DataVencimento), resposta);
        repositorio.Verify(repositorio => repositorio.ListarAsync(situacao, vencimento, cancelamento), Times.Once);
    }

    [Fact]
    public async Task NaoDeveConsultarRepositorioComSituacaoDeFiltroInvalida()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => servico.ListarAsync((SituacaoTarefa)99));
        repositorio.Verify(repositorio => repositorio.ListarAsync(It.IsAny<SituacaoTarefa?>(), It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

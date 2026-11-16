using GestaoTarefas.Dominio.Entidades;
using GestaoTarefas.Dominio.Enums;

namespace GestaoTarefas.Testes.Unitarios;

public class TarefaTestes
{
    [Fact]
    public void DeveCriarTarefaComDadosValidos()
    {
        var vencimento = new DateOnly(2026, 12, 15);

        var tarefa = new Tarefa("Preparar entrevista", "Revisar o desafio técnico", vencimento);

        Assert.NotEqual(Guid.Empty, tarefa.Id);
        Assert.Equal("Preparar entrevista", tarefa.Titulo);
        Assert.Equal("Revisar o desafio técnico", tarefa.Descricao);
        Assert.Equal(vencimento, tarefa.DataVencimento);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void NaoDevePermitirTituloVazio(string? titulo)
    {
        var erro = Assert.Throws<ArgumentException>(() =>
            new Tarefa(titulo!, null, new DateOnly(2026, 12, 15)));

        Assert.Equal("titulo", erro.ParamName);
    }

    [Fact]
    public void NaoDevePermitirTituloComMaisDe150Caracteres()
    {
        var erro = Assert.Throws<ArgumentException>(() =>
            new Tarefa(new string('a', 151), null, new DateOnly(2026, 12, 15)));

        Assert.Equal("titulo", erro.ParamName);
    }

    [Fact]
    public void NaoDevePermitirDescricaoComMaisDe2000Caracteres()
    {
        var erro = Assert.Throws<ArgumentException>(() =>
            new Tarefa("Preparar entrevista", new string('a', 2001), new DateOnly(2026, 12, 15)));

        Assert.Equal("descricao", erro.ParamName);
    }

    [Fact]
    public void DevePermitirTituloEDescricaoNosLimitesDeTamanho()
    {
        var titulo = new string('a', 150);
        var descricao = new string('b', 2000);

        var tarefa = new Tarefa(titulo, descricao, new DateOnly(2026, 12, 15));

        Assert.Equal(titulo, tarefa.Titulo);
        Assert.Equal(descricao, tarefa.Descricao);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void DevePermitirTarefaSemDescricao(string? descricao)
    {
        var tarefa = new Tarefa("Preparar entrevista", descricao, new DateOnly(2026, 12, 15));

        Assert.Equal(descricao, tarefa.Descricao);
    }

    [Fact]
    public void DeveCriarTarefaComSituacaoPendente()
    {
        var tarefa = new Tarefa("Preparar entrevista", null, new DateOnly(2026, 12, 15));

        Assert.Equal(SituacaoTarefa.Pendente, tarefa.Situacao);
    }

    [Theory]
    [InlineData(SituacaoTarefa.Pendente)]
    [InlineData(SituacaoTarefa.EmAndamento)]
    [InlineData(SituacaoTarefa.Concluida)]
    public void DeveAtualizarSituacaoValida(SituacaoTarefa situacao)
    {
        var tarefa = new Tarefa("Preparar entrevista", null, new DateOnly(2026, 12, 15));
        tarefa.AtualizarSituacao(SituacaoTarefa.EmAndamento);

        tarefa.AtualizarSituacao(situacao);

        Assert.Equal(situacao, tarefa.Situacao);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(4)]
    public void NaoDevePermitirSituacaoInvalida(int situacao)
    {
        var tarefa = new Tarefa("Preparar entrevista", null, new DateOnly(2026, 12, 15));

        var erro = Assert.Throws<ArgumentException>(() => tarefa.AtualizarSituacao((SituacaoTarefa)situacao));

        Assert.Equal("situacao", erro.ParamName);
        Assert.Equal(SituacaoTarefa.Pendente, tarefa.Situacao);
    }

    [Fact]
    public void DeveAtualizarDadosPreservandoIdentificadorESituacao()
    {
        var tarefa = new Tarefa("Preparar entrevista", "Revisar o desafio", new DateOnly(2026, 12, 15));
        var id = tarefa.Id;
        tarefa.AtualizarSituacao(SituacaoTarefa.EmAndamento);
        var vencimento = new DateOnly(2026, 12, 20);

        tarefa.Atualizar("Revisar testes", null, vencimento);

        Assert.Equal("Revisar testes", tarefa.Titulo);
        Assert.Null(tarefa.Descricao);
        Assert.Equal(vencimento, tarefa.DataVencimento);
        Assert.Equal(id, tarefa.Id);
        Assert.Equal(SituacaoTarefa.EmAndamento, tarefa.Situacao);
    }

    [Fact]
    public void NaoDeveAlterarDadosQuandoAtualizacaoForInvalida()
    {
        var vencimento = new DateOnly(2026, 12, 15);
        var tarefa = new Tarefa("Preparar entrevista", "Revisar o desafio", vencimento);

        Assert.Throws<ArgumentException>(() =>
            tarefa.Atualizar("Novo título", new string('a', 2001), vencimento.AddDays(1)));

        Assert.Equal("Preparar entrevista", tarefa.Titulo);
        Assert.Equal("Revisar o desafio", tarefa.Descricao);
        Assert.Equal(vencimento, tarefa.DataVencimento);
    }
}

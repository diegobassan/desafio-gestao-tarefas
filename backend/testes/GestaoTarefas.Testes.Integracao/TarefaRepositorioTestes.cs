using GestaoTarefas.Dominio.Entidades;
using GestaoTarefas.Dominio.Enums;
using GestaoTarefas.Infraestrutura.Persistencia;
using GestaoTarefas.Infraestrutura.Repositorios;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GestaoTarefas.Testes.Integracao;

public class TarefaRepositorioTestes : IAsyncLifetime
{
    private readonly SqliteConnection conexao = new("Data Source=:memory:");

    public async Task InitializeAsync()
    {
        await conexao.OpenAsync();
        await using var contexto = CriarContexto();
        await contexto.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await conexao.DisposeAsync();
    }

    private GestaoTarefasContexto CriarContexto()
    {
        var opcoes = new DbContextOptionsBuilder<GestaoTarefasContexto>()
            .UseSqlite(conexao)
            .Options;
        return new GestaoTarefasContexto(opcoes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Revisar o desafio técnico")]
    public async Task DeveIncluirEConsultarTarefa(string? descricao)
    {
        var vencimento = new DateOnly(2026, 12, 15);
        var tarefa = new Tarefa("Preparar entrevista", descricao, vencimento);
        await using (var contexto = CriarContexto())
        {
            await new TarefaRepositorio(contexto).AdicionarAsync(tarefa);
        }

        await using var consulta = CriarContexto();
        var encontrada = await new TarefaRepositorio(consulta).ConsultarPorIdAsync(tarefa.Id);

        Assert.NotNull(encontrada);
        Assert.Equal(tarefa.Id, encontrada.Id);
        Assert.Equal(tarefa.Titulo, encontrada.Titulo);
        Assert.Equal(descricao, encontrada.Descricao);
        Assert.Equal(SituacaoTarefa.Pendente, encontrada.Situacao);
        Assert.Equal(vencimento, encontrada.DataVencimento);
    }

    [Fact]
    public async Task DeveRetornarNuloParaIdentificadorInexistente()
    {
        await using var contexto = CriarContexto();

        var tarefa = await new TarefaRepositorio(contexto).ConsultarPorIdAsync(Guid.NewGuid());

        Assert.Null(tarefa);
    }

    [Fact]
    public async Task DeveAtualizarTarefa()
    {
        var tarefa = new Tarefa("Preparar entrevista", "Revisar o desafio", new DateOnly(2026, 12, 15));
        await using (var contexto = CriarContexto())
        {
            await new TarefaRepositorio(contexto).AdicionarAsync(tarefa);
        }

        await using (var contexto = CriarContexto())
        {
            var repositorio = new TarefaRepositorio(contexto);
            var encontrada = await repositorio.ConsultarPorIdAsync(tarefa.Id);
            Assert.NotNull(encontrada);
            encontrada.Atualizar("Revisar testes", null, new DateOnly(2026, 12, 20));
            encontrada.AtualizarSituacao(SituacaoTarefa.Concluida);
            await repositorio.AtualizarAsync(encontrada);
        }

        await using var consulta = CriarContexto();
        var atualizada = await new TarefaRepositorio(consulta).ConsultarPorIdAsync(tarefa.Id);
        Assert.NotNull(atualizada);
        Assert.Equal("Revisar testes", atualizada.Titulo);
        Assert.Null(atualizada.Descricao);
        Assert.Equal(new DateOnly(2026, 12, 20), atualizada.DataVencimento);
        Assert.Equal(SituacaoTarefa.Concluida, atualizada.Situacao);
    }

    [Fact]
    public async Task DeveAtualizarTarefaConsultadaNoMesmoContextoDaInclusao()
    {
        var tarefa = new Tarefa("Preparar entrevista", null, new DateOnly(2026, 12, 15));
        await using (var contexto = CriarContexto())
        {
            var repositorio = new TarefaRepositorio(contexto);
            await repositorio.AdicionarAsync(tarefa);
            var encontrada = await repositorio.ConsultarPorIdAsync(tarefa.Id);
            Assert.NotNull(encontrada);
            encontrada.AtualizarSituacao(SituacaoTarefa.EmAndamento);
            await repositorio.AtualizarAsync(encontrada);
        }

        await using var consulta = CriarContexto();
        var atualizada = await new TarefaRepositorio(consulta).ConsultarPorIdAsync(tarefa.Id);
        Assert.NotNull(atualizada);
        Assert.Equal(SituacaoTarefa.EmAndamento, atualizada.Situacao);
    }

    [Fact]
    public async Task DeveRemoverTarefa()
    {
        var tarefa = new Tarefa("Preparar entrevista", null, new DateOnly(2026, 12, 15));
        await using (var contexto = CriarContexto())
        {
            await new TarefaRepositorio(contexto).AdicionarAsync(tarefa);
        }

        await using (var contexto = CriarContexto())
        {
            var repositorio = new TarefaRepositorio(contexto);
            var encontrada = await repositorio.ConsultarPorIdAsync(tarefa.Id);
            Assert.NotNull(encontrada);
            await repositorio.RemoverAsync(encontrada);
        }

        await using var consulta = CriarContexto();
        Assert.Null(await new TarefaRepositorio(consulta).ConsultarPorIdAsync(tarefa.Id));
    }

    [Theory]
    [InlineData(null, null, new[] { 0, 1, 2, 3 })]
    [InlineData(SituacaoTarefa.EmAndamento, null, new[] { 1, 2 })]
    [InlineData(null, 15, new[] { 0, 1 })]
    [InlineData(SituacaoTarefa.EmAndamento, 15, new[] { 1 })]
    [InlineData(SituacaoTarefa.Concluida, 15, new int[] { })]
    public async Task DeveListarTarefasRespeitandoFiltrosOpcionais(
        SituacaoTarefa? situacao, int? diaVencimento, int[] indicesEsperados)
    {
        var vencimento = new DateOnly(2026, 12, 15);
        var tarefas = new[]
        {
            new Tarefa("Revisar código", null, vencimento),
            new Tarefa("Escrever testes", null, vencimento),
            new Tarefa("Atualizar documentação", null, vencimento.AddDays(1)),
            new Tarefa("Preparar demonstração", null, vencimento.AddDays(1))
        };
        tarefas[1].AtualizarSituacao(SituacaoTarefa.EmAndamento);
        tarefas[2].AtualizarSituacao(SituacaoTarefa.EmAndamento);
        tarefas[3].AtualizarSituacao(SituacaoTarefa.Concluida);
        await using (var contexto = CriarContexto())
        {
            var repositorio = new TarefaRepositorio(contexto);
            foreach (var tarefa in tarefas)
            {
                await repositorio.AdicionarAsync(tarefa);
            }
        }

        DateOnly? dataFiltro = diaVencimento.HasValue ? new DateOnly(2026, 12, diaVencimento.Value) : null;
        await using var consulta = CriarContexto();

        var encontradas = await new TarefaRepositorio(consulta).ListarAsync(situacao, dataFiltro);

        var esperadas = indicesEsperados
            .Select(indice => tarefas[indice].Id)
            .OrderBy(id => id);
        Assert.Equal(esperadas, encontradas.Select(tarefa => tarefa.Id).OrderBy(id => id));
    }

    [Fact]
    public async Task DeveAplicarMigrationInicialSemAlteracoesPendentes()
    {
        await using var contexto = CriarContexto();

        Assert.Single(await contexto.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await contexto.Database.GetPendingMigrationsAsync());
        Assert.False(contexto.Database.HasPendingModelChanges());

        await using var comando = conexao.CreateCommand();
        comando.CommandText = "PRAGMA table_info('Tarefas')";
        await using var leitura = await comando.ExecuteReaderAsync();
        var tipos = new Dictionary<string, string>();
        while (await leitura.ReadAsync())
        {
            tipos.Add(leitura.GetString(1), leitura.GetString(2));
        }

        Assert.Equal("INTEGER", tipos["Situacao"]);
        Assert.Equal("TEXT", tipos["DataVencimento"]);
    }

    [Theory]
    [InlineData(151, 0)]
    [InlineData(1, 2001)]
    public async Task NaoDevePersistirTextosAcimaDosLimites(int tamanhoTitulo, int tamanhoDescricao)
    {
        await using var contexto = CriarContexto();
        var id = Guid.NewGuid();
        var titulo = new string('a', tamanhoTitulo);
        var descricao = new string('b', tamanhoDescricao);
        var situacao = (int)SituacaoTarefa.Pendente;
        var vencimento = new DateOnly(2026, 12, 15);

        await Assert.ThrowsAsync<SqliteException>(() => contexto.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO Tarefas (Id, Titulo, Descricao, Situacao, DataVencimento) VALUES ({id}, {titulo}, {descricao}, {situacao}, {vencimento})"));
    }
}

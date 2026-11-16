using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GestaoTarefas.Infraestrutura.Persistencia;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GestaoTarefas.Testes.Integracao;

public class TarefasHttpTestes : IAsyncLifetime
{
    private readonly SqliteConnection conexao = new("Data Source=:memory:");
    private WebApplicationFactory<Program> aplicacao = null!;
    private HttpClient cliente = null!;

    public async Task InitializeAsync()
    {
        await conexao.OpenAsync();
        aplicacao = new WebApplicationFactory<Program>().WithWebHostBuilder(construtor =>
        {
            construtor.UseEnvironment("Development");
            construtor.ConfigureServices(servicos =>
            {
                servicos.RemoveAll<GestaoTarefasContexto>();
                servicos.RemoveAll<DbContextOptions<GestaoTarefasContexto>>();
                servicos.RemoveAll<IDbContextOptionsConfiguration<GestaoTarefasContexto>>();
                servicos.AddDbContext<GestaoTarefasContexto>(opcoes => opcoes.UseSqlite(conexao));
            });
        });

        await using var escopo = aplicacao.Services.CreateAsyncScope();
        await escopo.ServiceProvider.GetRequiredService<GestaoTarefasContexto>().Database.MigrateAsync();
        cliente = aplicacao.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    public async Task DisposeAsync()
    {
        cliente?.Dispose();
        if (aplicacao is not null)
        {
            await aplicacao.DisposeAsync();
        }
        await conexao.DisposeAsync();
    }

    [Fact]
    public async Task DeveCadastrarEConsultarTarefaComContratoJsonELocalizacao()
    {
        using var resposta = await cliente.PostAsJsonAsync("/api/tarefas", new
        {
            titulo = "Preparar entrevista", descricao = "Revisar o desafio técnico", dataVencimento = "2026-11-20"
        });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.Equal("application/json", resposta.Content.Headers.ContentType?.MediaType);
        var criada = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        var id = criada.GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal(new[] { "dataVencimento", "descricao", "id", "situacao", "titulo" },
            criada.EnumerateObject().Select(propriedade => propriedade.Name).OrderBy(nome => nome));
        Assert.Equal("Preparar entrevista", criada.GetProperty("titulo").GetString());
        Assert.Equal("Revisar o desafio técnico", criada.GetProperty("descricao").GetString());
        Assert.Equal("Pendente", criada.GetProperty("situacao").GetString());
        Assert.Equal("2026-11-20", criada.GetProperty("dataVencimento").GetString());
        var localizacao = resposta.Headers.Location;
        Assert.NotNull(localizacao);
        Assert.EndsWith($"/api/tarefas/{id}", localizacao.ToString());

        using var consulta = await cliente.GetAsync(localizacao);
        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
        Assert.True(JsonElement.DeepEquals(criada, await consulta.Content.ReadFromJsonAsync<JsonElement>()));
    }

    [Fact]
    public async Task DeveRetornarListaVaziaQuandoNaoHouverTarefas()
    {
        using var resposta = await cliente.GetAsync("/api/tarefas");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Empty((await resposta.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray());
    }

    [Fact]
    public async Task DeveAtualizarTarefaSemPermitirTrocaDoIdentificador()
    {
        var tarefa = await CadastrarTarefaAsync();
        var id = tarefa.GetProperty("id").GetGuid();
        var outroId = Guid.NewGuid();

        using var resposta = await cliente.PutAsJsonAsync($"/api/tarefas/{id}", new
        {
            id = outroId, titulo = "Finalizar revisão", descricao = (string?)null,
            situacao = "Concluida", dataVencimento = "2026-11-21"
        });

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.Empty(await resposta.Content.ReadAsStringAsync());
        using var consulta = await cliente.GetAsync($"/api/tarefas/{id}");
        Assert.Equal(HttpStatusCode.OK, consulta.StatusCode);
        var atualizada = await consulta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(id, atualizada.GetProperty("id").GetGuid());
        Assert.Equal("Finalizar revisão", atualizada.GetProperty("titulo").GetString());
        Assert.Equal(JsonValueKind.Null, atualizada.GetProperty("descricao").ValueKind);
        Assert.Equal("Concluida", atualizada.GetProperty("situacao").GetString());
        Assert.Equal("2026-11-21", atualizada.GetProperty("dataVencimento").GetString());
        using var consultaOutroId = await cliente.GetAsync($"/api/tarefas/{outroId}");
        Assert.Equal(HttpStatusCode.NotFound, consultaOutroId.StatusCode);
    }

    [Fact]
    public async Task DeveExcluirTarefaERetornarNaoEncontradaNaConsultaSeguinte()
    {
        var tarefa = await CadastrarTarefaAsync();
        var caminho = $"/api/tarefas/{tarefa.GetProperty("id").GetGuid()}";

        using var resposta = await cliente.DeleteAsync(caminho);

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.Empty(await resposta.Content.ReadAsStringAsync());
        using var consulta = await cliente.GetAsync(caminho);
        Assert.Equal(HttpStatusCode.NotFound, consulta.StatusCode);
        using var repeticao = await cliente.DeleteAsync(caminho);
        Assert.Equal(HttpStatusCode.NotFound, repeticao.StatusCode);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task DeveRetornar404ParaTarefaInexistente(string metodo)
    {
        using var requisicao = new HttpRequestMessage(new HttpMethod(metodo), $"/api/tarefas/{Guid.NewGuid()}");
        if (metodo == "PUT")
        {
            requisicao.Content = JsonContent.Create(new
            {
                titulo = "Revisar testes", situacao = "Pendente", dataVencimento = "2026-11-20"
            });
        }

        using var resposta = await cliente.SendAsync(requisicao);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        Assert.Equal(404, (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetInt32());
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task DeveRetornar400ParaIdentificadorMalformado(string metodo)
    {
        using var requisicao = new HttpRequestMessage(new HttpMethod(metodo), "/api/tarefas/identificador-invalido");
        if (metodo == "PUT")
        {
            requisicao.Content = JsonContent.Create(new
            {
                titulo = "Revisar testes", situacao = "Pendente", dataVencimento = "2026-11-20"
            });
        }

        using var resposta = await cliente.SendAsync(requisicao);

        await VerificarErroValidacaoAsync(resposta);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("{invalido}")]
    [InlineData("{\"dataVencimento\":\"2026-11-20\"}")]
    [InlineData("{\"titulo\":null,\"dataVencimento\":\"2026-11-20\"}")]
    [InlineData("{\"titulo\":\"\",\"dataVencimento\":\"2026-11-20\"}")]
    [InlineData("{\"titulo\":\"   \",\"dataVencimento\":\"2026-11-20\"}")]
    [InlineData("{\"titulo\":\"Revisar testes\"}")]
    [InlineData("{\"titulo\":\"Revisar testes\",\"dataVencimento\":null}")]
    [InlineData("{\"titulo\":\"Revisar testes\",\"dataVencimento\":\"2026-02-30\"}")]
    public async Task NaoDeveCadastrarJsonInvalidoOuCamposObrigatoriosAusentes(string corpo)
    {
        using var conteudo = new StringContent(corpo, Encoding.UTF8, "application/json");

        using var resposta = await cliente.PostAsync("/api/tarefas", conteudo);

        await VerificarErroValidacaoAsync(resposta);
        var tarefas = await cliente.GetFromJsonAsync<JsonElement>("/api/tarefas");
        Assert.Empty(tarefas.EnumerateArray());
    }

    [Theory]
    [InlineData(151, 0)]
    [InlineData(1, 2001)]
    public async Task NaoDeveCadastrarTextosAcimaDosLimitesDoDominio(int tamanhoTitulo, int tamanhoDescricao)
    {
        using var resposta = await cliente.PostAsJsonAsync("/api/tarefas", new
        {
            titulo = new string('a', tamanhoTitulo), descricao = new string('b', tamanhoDescricao), dataVencimento = "2026-11-20"
        });

        await VerificarErroValidacaoAsync(resposta);
        Assert.Empty((await cliente.GetFromJsonAsync<JsonElement>("/api/tarefas")).EnumerateArray());
    }

    [Theory]
    [InlineData("{\"titulo\":\"Revisar testes\",\"situacao\":\"Inexistente\",\"dataVencimento\":\"2026-11-20\"}")]
    [InlineData("{\"titulo\":\"Revisar testes\",\"situacao\":2,\"dataVencimento\":\"2026-11-20\"}")]
    [InlineData("{\"titulo\":\"Revisar testes\",\"dataVencimento\":\"2026-11-20\"}")]
    [InlineData("{\"titulo\":\"\",\"situacao\":\"EmAndamento\",\"dataVencimento\":\"2026-11-20\"}")]
    public async Task NaoDeveAtualizarComSituacaoOuDadosInvalidos(string corpo)
    {
        var tarefa = await CadastrarTarefaAsync();
        var caminho = $"/api/tarefas/{tarefa.GetProperty("id").GetGuid()}";
        using var conteudo = new StringContent(corpo, Encoding.UTF8, "application/json");

        using var resposta = await cliente.PutAsync(caminho, conteudo);

        await VerificarErroValidacaoAsync(resposta);
        Assert.True(JsonElement.DeepEquals(tarefa, await cliente.GetFromJsonAsync<JsonElement>(caminho)));
    }

    [Fact]
    public async Task NaoDevePersistirAtualizacaoRejeitadaPeloDominio()
    {
        var tarefa = await CadastrarTarefaAsync();
        var caminho = $"/api/tarefas/{tarefa.GetProperty("id").GetGuid()}";

        using var resposta = await cliente.PutAsJsonAsync(caminho, new
        {
            titulo = "Novo título", descricao = new string('a', 2001),
            situacao = "EmAndamento", dataVencimento = "2026-11-21"
        });

        await VerificarErroValidacaoAsync(resposta);
        Assert.True(JsonElement.DeepEquals(tarefa, await cliente.GetFromJsonAsync<JsonElement>(caminho)));
    }

    [Theory]
    [InlineData("", new[] { 0, 1, 2, 3 })]
    [InlineData("?situacao=EmAndamento", new[] { 1, 2 })]
    [InlineData("?dataVencimento=2026-11-20", new[] { 0, 1 })]
    [InlineData("?situacao=EmAndamento&dataVencimento=2026-11-20", new[] { 1 })]
    [InlineData("?situacao=Concluida&dataVencimento=2026-11-20", new int[] { })]
    public async Task DeveListarTarefasComFiltrosIsoladosECombinados(string filtros, int[] indicesEsperados)
    {
        var tarefas = new[]
        {
            await CadastrarTarefaAsync("Revisar código"),
            await CadastrarTarefaAsync("Escrever testes"),
            await CadastrarTarefaAsync("Atualizar documentação", "2026-11-21"),
            await CadastrarTarefaAsync("Preparar demonstração", "2026-11-21")
        };
        await AtualizarSituacaoAsync(tarefas[1], "EmAndamento");
        await AtualizarSituacaoAsync(tarefas[2], "EmAndamento");
        await AtualizarSituacaoAsync(tarefas[3], "Concluida");

        using var resposta = await cliente.GetAsync("/api/tarefas" + filtros);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var encontradas = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        var esperados = indicesEsperados.Select(indice => tarefas[indice].GetProperty("id").GetGuid()).OrderBy(id => id);
        Assert.Equal(esperados, encontradas.EnumerateArray().Select(tarefa => tarefa.GetProperty("id").GetGuid()).OrderBy(id => id));
    }

    [Theory]
    [InlineData("situacao=Inexistente")]
    [InlineData("situacao=0")]
    [InlineData("situacao=99")]
    [InlineData("dataVencimento=invalida")]
    [InlineData("dataVencimento=2026-02-30")]
    public async Task DeveRetornar400ParaFiltrosInvalidos(string filtros)
    {
        using var resposta = await cliente.GetAsync("/api/tarefas?" + filtros);

        await VerificarErroValidacaoAsync(resposta);
    }

    [Theory]
    [InlineData("http://localhost:4200", true)]
    [InlineData("http://localhost:4300", false)]
    public async Task DeveAplicarCorsAosEndpointsDeTarefas(string origem, bool permitida)
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Get, "/api/tarefas");
        requisicao.Headers.Add("Origin", origem);

        using var resposta = await cliente.SendAsync(requisicao);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(permitida, resposta.Headers.TryGetValues("Access-Control-Allow-Origin", out var origens));
        if (permitida) Assert.Equal(origem, Assert.Single(origens!));
    }

    [Fact]
    public async Task DeveDocumentarContratosRespostasEFiltrosNoOpenApi()
    {
        var documento = await cliente.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        var caminhos = documento.GetProperty("paths");
        var colecao = caminhos.GetProperty("/api/tarefas");
        var recurso = caminhos.GetProperty("/api/tarefas/{id}");
        var parametros = colecao.GetProperty("get").GetProperty("parameters").EnumerateArray().ToArray();
        Assert.Equal(new[] { "dataVencimento", "situacao" }, parametros.Select(parametro => parametro.GetProperty("name").GetString()).OrderBy(nome => nome));
        foreach (var parametro in parametros)
        {
            Assert.Equal("query", parametro.GetProperty("in").GetString());
            Assert.False(string.IsNullOrWhiteSpace(parametro.GetProperty("description").GetString()));
            Assert.False(parametro.TryGetProperty("required", out var obrigatorio) && obrigatorio.GetBoolean());
        }
        var situacaoFiltro = parametros.Single(parametro => parametro.GetProperty("name").GetString() == "situacao");
        VerificarSituacoes(documento, situacaoFiltro.GetProperty("schema"));
        var dataFiltro = parametros.Single(parametro => parametro.GetProperty("name").GetString() == "dataVencimento");
        Assert.Equal("date", ResolverEsquema(documento, dataFiltro.GetProperty("schema")).GetProperty("format").GetString());

        foreach (var (operacao, sucesso) in new[]
        {
            (colecao.GetProperty("get"), "200"), (colecao.GetProperty("post"), "201"),
            (recurso.GetProperty("get"), "200"), (recurso.GetProperty("put"), "204"),
            (recurso.GetProperty("delete"), "204")
        })
        {
            var respostas = operacao.GetProperty("responses");
            Assert.True(respostas.TryGetProperty(sucesso, out _));
            Assert.True(respostas.TryGetProperty("400", out _));
        }
        foreach (var metodo in new[] { "get", "put", "delete" })
        {
            Assert.True(recurso.GetProperty(metodo).GetProperty("responses").TryGetProperty("404", out _));
        }

        var esquemaResposta = ResolverEsquema(documento, colecao.GetProperty("post").GetProperty("responses")
            .GetProperty("201").GetProperty("content").GetProperty("application/json").GetProperty("schema"));
        VerificarSituacoes(documento, esquemaResposta.GetProperty("properties").GetProperty("situacao"));
        foreach (var operacao in new[] { colecao.GetProperty("post"), recurso.GetProperty("put") })
        {
            var esquema = ResolverEsquema(documento, operacao.GetProperty("requestBody").GetProperty("content")
                .GetProperty("application/json").GetProperty("schema"));
            Assert.False(esquema.GetProperty("properties").TryGetProperty("id", out _));
            var obrigatorios = esquema.GetProperty("required").EnumerateArray().Select(valor => valor.GetString()).ToArray();
            Assert.Contains("titulo", obrigatorios);
            Assert.Contains("dataVencimento", obrigatorios);
            if (esquema.GetProperty("properties").TryGetProperty("situacao", out var situacao))
            {
                Assert.Contains("situacao", obrigatorios);
                VerificarSituacoes(documento, situacao);
            }
        }
    }

    private async Task<JsonElement> CadastrarTarefaAsync(string titulo = "Revisar código", string vencimento = "2026-11-20")
    {
        using var resposta = await cliente.PostAsJsonAsync("/api/tarefas", new { titulo, dataVencimento = vencimento });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        return await resposta.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task AtualizarSituacaoAsync(JsonElement tarefa, string situacao)
    {
        using var resposta = await cliente.PutAsJsonAsync($"/api/tarefas/{tarefa.GetProperty("id").GetGuid()}", new
        {
            titulo = tarefa.GetProperty("titulo").GetString(), situacao,
            dataVencimento = tarefa.GetProperty("dataVencimento").GetString()
        });
        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
    }

    private static async Task VerificarErroValidacaoAsync(HttpResponseMessage resposta)
    {
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType?.MediaType);
        var problema = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(400, problema.GetProperty("status").GetInt32());
        Assert.NotEmpty(problema.GetProperty("errors").EnumerateObject());
    }

    private static JsonElement ResolverEsquema(JsonElement documento, JsonElement esquema)
    {
        while (esquema.TryGetProperty("$ref", out var referencia))
        {
            esquema = referencia.GetString()!.Split('/').Skip(1)
                .Aggregate(documento, (atual, nome) => atual.GetProperty(nome));
        }
        return esquema;
    }

    private static void VerificarSituacoes(JsonElement documento, JsonElement esquema)
    {
        var valores = ResolverEsquema(documento, esquema).GetProperty("enum").EnumerateArray()
            .Where(valor => valor.ValueKind == JsonValueKind.String).Select(valor => valor.GetString());
        Assert.Equal(new[] { "Pendente", "EmAndamento", "Concluida" }, valores);
    }
}

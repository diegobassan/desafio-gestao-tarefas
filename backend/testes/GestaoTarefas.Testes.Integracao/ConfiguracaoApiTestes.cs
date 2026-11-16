using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GestaoTarefas.Testes.Integracao;

public class ConfiguracaoApiTestes
{
    [Fact]
    public async Task DeveExporDocumentoOpenApiSemEndpointsDeNegocioEmDesenvolvimento()
    {
        await using var aplicacao = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        using var cliente = aplicacao.CreateClient();

        using var resposta = await cliente.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        using var documento = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.StartsWith("3.", documento.RootElement.GetProperty("openapi").GetString() ?? string.Empty);
        Assert.Empty(documento.RootElement.GetProperty("paths").EnumerateObject());
    }

    [Fact]
    public async Task DeveExporSwaggerUiEmDesenvolvimento()
    {
        await using var aplicacao = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        using var cliente = aplicacao.CreateClient();

        using var resposta = await cliente.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("text/html", resposta.Content.Headers.ContentType?.MediaType);
        Assert.NotEmpty(await resposta.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/swagger/index.html")]
    [InlineData("/openapi/v1.json")]
    public async Task NaoDeveExporDocumentacaoEmProducao(string caminho)
    {
        await using var aplicacao = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var cliente = aplicacao.CreateClient();

        using var resposta = await cliente.GetAsync(caminho);

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Theory]
    [InlineData("http://localhost:4200", true)]
    [InlineData("http://localhost:4300", false)]
    public async Task DevePermitirCorsApenasParaOrigemConfigurada(string origem, bool permitida)
    {
        await using var aplicacao = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        using var cliente = aplicacao.CreateClient();
        using var requisicao = new HttpRequestMessage(HttpMethod.Options, "/openapi/v1.json");
        requisicao.Headers.Add("Origin", origem);
        requisicao.Headers.Add("Access-Control-Request-Method", "GET");

        using var resposta = await cliente.SendAsync(requisicao);

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        var possuiOrigem = resposta.Headers.TryGetValues("Access-Control-Allow-Origin", out var origens);
        Assert.Equal(permitida, possuiOrigem);
        if (permitida)
        {
            Assert.Equal(origem, Assert.Single(origens!));
        }
    }
}

using System.Text.Json.Serialization;
using GestaoTarefas.Api.Filtros;
using GestaoTarefas.Aplicacao.Servicos;
using GestaoTarefas.Dominio.Enums;
using GestaoTarefas.Dominio.Repositorios;
using GestaoTarefas.Infraestrutura.Persistencia;
using GestaoTarefas.Infraestrutura.Repositorios;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(opcoes => opcoes.Filters.Add<ValidacaoFiltro>())
    .AddJsonOptions(opcoes => opcoes.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter<SituacaoTarefa>(allowIntegerValues: false)));

builder.Services.ConfigureHttpJsonOptions(opcoes => opcoes.SerializerOptions.Converters.Add(
    new JsonStringEnumConverter<SituacaoTarefa>(allowIntegerValues: false)));
builder.Services.AddOpenApi();

var conexao = builder.Configuration.GetConnectionString("BancoDeDados")
    ?? throw new InvalidOperationException("A conexão BancoDeDados não foi configurada.");

builder.Services.AddDbContext<GestaoTarefasContexto>(opcoes => opcoes.UseSqlite(conexao));
builder.Services.AddScoped<ITarefaRepositorio, TarefaRepositorio>();
builder.Services.AddScoped<TarefaServico>();

var origensPermitidas = builder.Configuration
    .GetSection("Cors:OrigensPermitidas")
    .Get<string[]>() ?? [];

builder.Services.AddCors(opcoes =>
{
    opcoes.AddDefaultPolicy(politica =>
    {
        politica.WithOrigins(origensPermitidas)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(opcoes =>
    {
        opcoes.SwaggerEndpoint("/openapi/v1.json", "Gestão de Tarefas v1");
    });
}

app.UseCors();
app.MapControllers();

app.Run();

public partial class Program;

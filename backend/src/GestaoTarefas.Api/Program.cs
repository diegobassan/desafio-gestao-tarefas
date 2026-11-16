var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

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

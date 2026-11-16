# Desafio — Gestão de Tarefas

Estrutura inicial do desafio técnico com ASP.NET Core Web API e .NET 10.
Esta etapa prepara a solução e a API com controllers, sem endpoints de negócio.

## Estrutura

- `backend/src/GestaoTarefas.Api`: inicialização, controllers e configuração HTTP.
- `backend/src/GestaoTarefas.Aplicacao`: futuros serviços e DTOs.
- `backend/src/GestaoTarefas.Dominio`: futuras entidades, regras e contratos.
- `backend/src/GestaoTarefas.Infraestrutura`: futura persistência com EF Core 10 e SQLite.
- `backend/testes/GestaoTarefas.Testes.Unitarios`: xUnit e Moq preparados, sem testes de domínio nesta etapa.
- `backend/testes/GestaoTarefas.Testes.Integracao`: testes da configuração da API com xUnit e WebApplicationFactory.
- `frontend/`: reservado para Angular 22 com TypeScript; projeto ainda não criado.

As dependências são: API → Aplicação e Infraestrutura; Aplicação → Domínio;
Infraestrutura → Domínio. O Domínio não referencia outros projetos.
Os testes unitários referenciam Aplicação e Domínio; os testes de integração referenciam a API.

## Executar

Requisitos: SDK .NET 10 e acesso ao NuGet para restaurar os pacotes.
Na pasta `desafio-gestao-tarefas`:

```sh
cd backend
dotnet restore GestaoTarefas.slnx
dotnet build GestaoTarefas.slnx --no-restore
dotnet test GestaoTarefas.slnx --no-build --no-restore
dotnet run --project src/GestaoTarefas.Api --launch-profile http
```

O perfil `http` inicia em Development em `http://localhost:5000`:

- Swagger UI: `http://localhost:5000/swagger`
- Documento OpenAPI: `http://localhost:5000/openapi/v1.json`

Swagger UI e OpenAPI ficam disponíveis somente em Development.
O documento não possui operações nesta etapa. A raiz `/` retorna 404,
pois nenhum endpoint de negócio foi implementado.

## Configuração

`appsettings.json` define `Cors:OrigensPermitidas`, inicialmente com
`http://localhost:4200`, e `ConnectionStrings:BancoDados`, com o caminho
relativo `Data Source=gestao-tarefas.db`. O provedor SQLite está referenciado,
mas ainda não há DbContext, banco ou migrations.

O registro de dependências fica em `Program.cs`, usando o contêiner nativo do
.NET. Os serviços de aplicação e persistência serão registrados quando forem
implementados. Os nomes do domínio e dos testes devem ser escritos em português.

## Validação

Os testes de integração verificam o documento OpenAPI sem operações, o Swagger UI,
a ausência da documentação em Production e o preflight CORS para origens permitida
e não permitida. O projeto unitário ainda não contém casos de teste.

Para conferir manualmente, com a API em execução:

```sh
curl --fail http://localhost:5000/swagger/index.html
curl --fail http://localhost:5000/openapi/v1.json
curl -i -X OPTIONS http://localhost:5000/openapi/v1.json \
  -H 'Origin: http://localhost:4200' \
  -H 'Access-Control-Request-Method: GET'
```

O preflight deve retornar 204 com `Access-Control-Allow-Origin: http://localhost:4200`.

Na preparação inicial, a validação completa ficou bloqueada pelo ambiente:
o acesso a `api.nuget.org:443` foi negado (`NU1301`), houve falha de DNS
e faltaram pacotes no cache local.
`dotnet restore`, `dotnet build --no-restore`, `dotnet test --no-restore` e
`dotnet run --no-restore` foram executados e falharam por essa restrição.
Domínio e Aplicação compilaram, mas não foi possível confirmar a compilação
completa, executar testes ou iniciar a API. As referências foram verificadas
sem ciclos; a configuração Swagger/OpenAPI foi revisada apenas estaticamente.
Execute os comandos acima em um ambiente com acesso ao NuGet antes de considerar
esta etapa validada.

CRUD, Angular, autenticação e Docker Compose não fazem parte desta etapa.

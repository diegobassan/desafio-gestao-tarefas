# Gestão de Tarefas

Aplicação para cadastrar, consultar, editar e excluir tarefas, com API REST em .NET e interface Angular.

## Funcionalidades

- Tarefas com identificador, título, descrição opcional, situação e data de vencimento.
- Cadastro com situação inicial pendente; alteração para em andamento ou concluída pela edição.
- Listagem com filtros opcionais e combináveis por situação e data exata de vencimento.
- Validação de título obrigatório com até 150 caracteres, descrição com até 2000 caracteres e data de vencimento obrigatória.

## Tecnologias e arquitetura

Backend com .NET 10, ASP.NET Core e Entity Framework Core 10 com SQLite. Frontend com Angular 22, TypeScript e formulários reativos. A execução em containers utiliza Docker Compose e Nginx. Os testes usam xUnit, Moq, WebApplicationFactory, SQLite em memória e Vitest.

O backend está organizado em quatro camadas:

| Projeto | Responsabilidade |
| --- | --- |
| `GestaoTarefas.Api` | Controllers, contratos HTTP, configuração e injeção de dependência. |
| `GestaoTarefas.Aplicacao` | Serviços dos casos de uso e DTOs de entrada e saída. |
| `GestaoTarefas.Dominio` | Entidade, situações, regras de negócio e contrato do repositório. |
| `GestaoTarefas.Infraestrutura` | Repositório, contexto, mapeamentos e migrations do EF Core. |

`ITarefaRepositorio` abstrai as operações de persistência usadas pela aplicação. O `GestaoTarefasContexto`, derivado de `DbContext`, exerce o papel de Unit of Work do EF Core: acompanha alterações e as confirma com `SaveChangesAsync`, chamado pelo repositório em cada operação de escrita. Não há uma interface adicional de Unit of Work; esses padrões são usados no contexto do exercício, sem pressupor sua necessidade em toda aplicação com EF Core. Contexto, repositório e serviço de aplicação têm ciclo de vida por requisição (`Scoped`).

## Como executar com Docker

Pré-requisitos: Git e Docker Desktop ou Docker Engine com o plugin Docker Compose.

**Opção A — compilar localmente:**

```bash
git clone https://github.com/diegobassan/desafio-gestao-tarefas.git
cd desafio-gestao-tarefas
docker compose up -d --build
```

**Opção B — executar as imagens publicadas:**

Com o arquivo `compose.hub.yml` em uma pasta local, sem precisar do código-fonte:

```bash
docker compose -f compose.hub.yml up -d
```

Imagens públicas na versão **1.0.0**: [API no Docker Hub](https://hub.docker.com/r/diegobassan/desafio-gestao-tarefas-api) e [frontend no Docker Hub](https://hub.docker.com/r/diegobassan/desafio-gestao-tarefas-frontend).

As duas opções utilizam SQLite com volume persistente e acesso por **http://localhost:8080**. Execute uma alternativa por vez na porta padrão. Na opção B, a variável `PORTA_FRONTEND` permite escolher outra porta; por exemplo, defina `PORTA_FRONTEND=18080` em um arquivo `.env` ao lado do Compose para acessar **http://localhost:18080**.

Para parar a opção B, execute `docker compose -f compose.hub.yml down` na pasta do arquivo.

Após a inicialização, acesse **http://localhost:8080**. O frontend é publicado em `127.0.0.1:8080`; o Nginx encaminha `/api` para a API, que fica em uma rede interna, sem porta publicada no host. Os serviços possuem healthchecks e o frontend aguarda a API ficar saudável.

Para parar a aplicação, na raiz do repositório:

```bash
docker compose down
```

O SQLite fica em `/dados/gestao-tarefas.db` no volume nomeado `dados-tarefas`, preservado ao parar ou recriar containers. **`docker compose down -v` remove os volumes e pode apagar os dados.**

## Como executar para desenvolvimento

Pré-requisitos: Git, .NET SDK 10, Node.js 24 (24.15.0 ou superior na série 24) e npm. Os comandos abaixo partem da raiz do repositório, em dois terminais separados.

Backend:

```bash
cd backend
dotnet restore GestaoTarefas.slnx
dotnet run --project src/GestaoTarefas.Api --launch-profile http -- --BancoDeDados:AplicarMigracoesNaInicializacao=true
```

A API inicia em **http://localhost:5000**, no ambiente `Development`. O argumento habilita a aplicação das migrations existentes, criando o banco na primeira execução. A configuração padrão usa `gestao-tarefas.db` no diretório do projeto da API (`backend/src/GestaoTarefas.Api`).

Frontend, no outro terminal:

```bash
cd frontend
npm ci
npm start
```

Acesse **http://localhost:4200**. A configuração de desenvolvimento consome a API em `http://localhost:5000`, cuja política CORS permite essa origem. No build de produção, as chamadas usam `/api` na mesma origem, pelo proxy Nginx. Use `Ctrl+C` em cada terminal para encerrar.

## Swagger/OpenAPI e endpoints

Com a API iniciada pelo perfil `http` acima:

- Swagger UI interativo: **http://localhost:5000/swagger** (redireciona para `/swagger/index.html`).
- Documento OpenAPI JSON: **http://localhost:5000/openapi/v1.json**.

Ambos ficam disponíveis somente em `Development`. O Compose executa a API em `Production`, sem expor a documentação interativa ou o documento JSON. Para explorá-los, execute o backend localmente com o comando de desenvolvimento.

| Método | Rota | Resposta de sucesso |
| --- | --- | --- |
| GET | `/api/tarefas` | `200`, lista de tarefas |
| GET | `/api/tarefas/{id}` | `200`, tarefa encontrada |
| POST | `/api/tarefas` | `201`, tarefa criada e cabeçalho `Location` |
| PUT | `/api/tarefas/{id}` | `204`, atualização concluída |
| DELETE | `/api/tarefas/{id}` | `204`, exclusão concluída |

Consultas, atualizações e exclusões de identificadores inexistentes retornam `404`. Entradas inválidas retornam `400`. Os identificadores são UUIDs. A API usa `situacao` para o campo de status, com os valores JSON `Pendente`, `EmAndamento` e `Concluida`. Datas usam o formato `AAAA-MM-DD`.

Exemplo de filtro combinado:

```text
/api/tarefas?situacao=EmAndamento&dataVencimento=2030-12-31
```

Corpo para cadastro:

```json
{
  "titulo": "Revisar o desafio",
  "descricao": "Conferir os testes e a documentação",
  "dataVencimento": "2030-12-31"
}
```

Na atualização, envie os mesmos campos e acrescente `"situacao": "Concluida"`. O identificador permanece na rota.

## Testes

Backend, a partir da raiz:

```bash
cd backend
dotnet restore GestaoTarefas.slnx
dotnet build GestaoTarefas.slnx --configuration Release --no-restore
dotnet test GestaoTarefas.slnx --configuration Release --no-build --no-restore
```

A solução inclui testes unitários do domínio e dos serviços, além de testes de integração HTTP, CORS, Swagger/OpenAPI, repositório e migrations com SQLite em memória, sem depender do banco local.

Frontend, a partir da raiz, em outro terminal:

```bash
cd frontend
npm ci
npm test
npm run build
npm audit --audit-level=high
```

`npm test` executa o Vitest sem watch. Os testes verificam formulários, consumo HTTP, filtros, operações e tratamento de erros. `npm run build` gera a versão de produção.

Para validar a configuração dos containers, na raiz:

```bash
docker compose config --quiet
docker compose ps
```

## Decisões técnicas

- **SQLite:** persistência relacional sem exigir um servidor de banco separado, suficiente para o escopo do desafio.
- **Camadas:** mantêm regras de domínio, casos de uso, HTTP e persistência com responsabilidades distintas.
- **Docker Compose:** reúne API, frontend e rede em um comando, incluindo healthchecks e volume persistente.
- **Migrations:** versionadas na Infraestrutura e aplicadas com `MigrateAsync` quando `BancoDeDados:AplicarMigracoesNaInicializacao` está habilitado. O Compose já habilita essa opção; o comando de desenvolvimento a passa explicitamente. A inicialização aplica apenas migrations pendentes, sem recriar o banco existente.
- **Persistência:** o volume do Compose mantém o SQLite fora do ciclo de vida do container. Na execução local, o arquivo permanece no diretório da API.
- **Testes automatizados:** verificam regras, contratos HTTP e integração com o ORM, além dos fluxos e validações da interface.

## Integração contínua

O [workflow de integração contínua](.github/workflows/integracao-continua.yml) executa build e testes do backend em Linux e Windows, testes e build do frontend, auditoria de dependências e smoke tests com Docker Compose. A verificação dos containers cobre healthchecks, acesso pelo Nginx, cadastro e persistência após recriar a API.

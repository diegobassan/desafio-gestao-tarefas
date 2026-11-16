FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS compilacao
WORKDIR /codigo

COPY src/GestaoTarefas.Api/GestaoTarefas.Api.csproj src/GestaoTarefas.Api/
COPY src/GestaoTarefas.Aplicacao/GestaoTarefas.Aplicacao.csproj src/GestaoTarefas.Aplicacao/
COPY src/GestaoTarefas.Dominio/GestaoTarefas.Dominio.csproj src/GestaoTarefas.Dominio/
COPY src/GestaoTarefas.Infraestrutura/GestaoTarefas.Infraestrutura.csproj src/GestaoTarefas.Infraestrutura/
RUN dotnet restore src/GestaoTarefas.Api/GestaoTarefas.Api.csproj

COPY src/ src/
RUN dotnet publish src/GestaoTarefas.Api/GestaoTarefas.Api.csproj \
    --configuration Release --no-restore --output /publicacao /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS execucao
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /dados \
    && chown app:app /dados

COPY --from=compilacao /publicacao/ ./
ENV ASPNETCORE_HTTP_PORTS=8080
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "GestaoTarefas.Api.dll"]

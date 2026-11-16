using Microsoft.EntityFrameworkCore.Migrations;

namespace GestaoTarefas.Infraestrutura.Persistencia.Migrations;

public partial class Inicial : Migration
{
    protected override void Up(MigrationBuilder migracao)
    {
        migracao.CreateTable(
            name: "Tarefas",
            columns: tabela => new
            {
                Id = tabela.Column<Guid>(type: "TEXT", nullable: false),
                Titulo = tabela.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                Descricao = tabela.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                Situacao = tabela.Column<int>(type: "INTEGER", nullable: false),
                DataVencimento = tabela.Column<DateOnly>(type: "TEXT", nullable: false)
            },
            constraints: tabela =>
            {
                tabela.PrimaryKey("PK_Tarefas", tarefa => tarefa.Id);
                tabela.CheckConstraint("CK_Tarefas_Titulo_Tamanho", "length(\"Titulo\") BETWEEN 1 AND 150");
                tabela.CheckConstraint("CK_Tarefas_Descricao_Tamanho", "\"Descricao\" IS NULL OR length(\"Descricao\") <= 2000");
            });
    }

    protected override void Down(MigrationBuilder migracao)
    {
        migracao.DropTable(name: "Tarefas");
    }
}

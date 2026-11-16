using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GestaoTarefas.Infraestrutura.Persistencia.Migrations;

[DbContext(typeof(GestaoTarefasContexto))]
[Migration("20261116104610_Inicial")]
public partial class Inicial
{
    protected override void BuildTargetModel(ModelBuilder modelo)
    {
        modelo.HasAnnotation("ProductVersion", "10.0.15");

        modelo.Entity("GestaoTarefas.Dominio.Entidades.Tarefa", tarefa =>
        {
            tarefa.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("TEXT");
            tarefa.Property<DateOnly>("DataVencimento").HasColumnType("TEXT");
            tarefa.Property<string>("Descricao").HasMaxLength(2000).HasColumnType("TEXT");
            tarefa.Property<int>("Situacao").HasColumnType("INTEGER");
            tarefa.Property<string>("Titulo").IsRequired().HasMaxLength(150).HasColumnType("TEXT");

            tarefa.HasKey("Id");
            tarefa.ToTable("Tarefas", tabela =>
            {
                tabela.HasCheckConstraint("CK_Tarefas_Titulo_Tamanho", "length(\"Titulo\") BETWEEN 1 AND 150");
                tabela.HasCheckConstraint("CK_Tarefas_Descricao_Tamanho", "\"Descricao\" IS NULL OR length(\"Descricao\") <= 2000");
            });
        });
    }
}

using GestaoTarefas.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestaoTarefas.Infraestrutura.Persistencia.Configuracoes;

public class TarefaConfiguracao : IEntityTypeConfiguration<Tarefa>
{
    public void Configure(EntityTypeBuilder<Tarefa> tarefa)
    {
        tarefa.ToTable("Tarefas", tabela =>
        {
            tabela.HasCheckConstraint("CK_Tarefas_Titulo_Tamanho", "length(\"Titulo\") BETWEEN 1 AND 150");
            tabela.HasCheckConstraint("CK_Tarefas_Descricao_Tamanho", "\"Descricao\" IS NULL OR length(\"Descricao\") <= 2000");
        });

        tarefa.HasKey(tarefa => tarefa.Id);
        tarefa.Property(tarefa => tarefa.Id).ValueGeneratedNever();
        tarefa.Property(tarefa => tarefa.Titulo).IsRequired().HasMaxLength(150);
        tarefa.Property(tarefa => tarefa.Descricao).HasMaxLength(2000);
        tarefa.Property(tarefa => tarefa.Situacao).IsRequired().HasConversion<int>();
        tarefa.Property(tarefa => tarefa.DataVencimento).IsRequired().HasColumnType("TEXT");
    }
}

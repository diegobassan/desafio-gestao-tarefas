using GestaoTarefas.Dominio.Entidades;
using GestaoTarefas.Infraestrutura.Persistencia.Configuracoes;
using Microsoft.EntityFrameworkCore;

namespace GestaoTarefas.Infraestrutura.Persistencia;

public class GestaoTarefasContexto(DbContextOptions<GestaoTarefasContexto> opcoes) : DbContext(opcoes)
{
    public DbSet<Tarefa> Tarefas => Set<Tarefa>();

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        modelo.ApplyConfiguration(new TarefaConfiguracao());
    }
}

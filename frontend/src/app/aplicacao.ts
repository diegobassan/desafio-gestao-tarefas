import { Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, Observable } from 'rxjs';
import { AtualizarTarefaRequisicao, FiltrosTarefa, SituacaoTarefa, situacoesTarefa, TarefaResposta } from './tarefas/tarefa';
import { TarefaServico } from './tarefas/tarefa-servico';
import { TarefaFormulario } from './tarefas/tarefa-formulario';

@Component({
  selector: 'app-raiz',
  standalone: true,
  imports: [ReactiveFormsModule, TarefaFormulario],
  templateUrl: './aplicacao.html',
  styleUrl: './aplicacao.css',
})
export class Aplicacao implements OnInit {
  private readonly servico = inject(TarefaServico);
  private readonly destruidor = inject(DestroyRef);
  private readonly construtor = inject(FormBuilder);
  private filtrosAplicados: FiltrosTarefa = {};
  readonly situacoes = situacoesTarefa;
  readonly tarefas = signal<TarefaResposta[]>([]);
  readonly carregando = signal(false);
  readonly carregandoEdicao = signal(false);
  readonly salvando = signal(false);
  readonly excluindo = signal<string | null>(null);
  readonly formularioAberto = signal(false);
  readonly tarefaEmEdicao = signal<TarefaResposta | null>(null);
  readonly mensagemSucesso = signal('');
  readonly erroLista = signal('');
  readonly erroOperacao = signal('');
  readonly erroFormulario = signal('');
  readonly ocupado = computed(() => this.carregando() || this.carregandoEdicao() || this.salvando() || this.excluindo() !== null);
  readonly filtros = this.construtor.nonNullable.group({
    situacao: this.construtor.nonNullable.control<SituacaoTarefa | ''>(''),
    dataVencimento: '',
  });

  ngOnInit(): void {
    this.carregarTarefas();
  }

  carregarTarefas(): void {
    if (this.carregando()) return;
    this.carregando.set(true);
    this.erroLista.set('');
    this.servico.listar(this.filtrosAplicados).pipe(
      takeUntilDestroyed(this.destruidor),
      finalize(() => this.carregando.set(false)),
    ).subscribe({
      next: (tarefas) => this.tarefas.set(tarefas),
      error: (erro: unknown) => this.erroLista.set(this.obterMensagemErro(erro, 'Não foi possível carregar as tarefas. Tente novamente.')),
    });
  }

  aplicarFiltros(): void {
    if (this.ocupado() || this.formularioAberto()) return;
    const valores = this.filtros.getRawValue();
    this.filtrosAplicados = {
      ...(valores.situacao ? { situacao: valores.situacao } : {}),
      ...(valores.dataVencimento ? { dataVencimento: valores.dataVencimento } : {}),
    };
    this.mensagemSucesso.set('');
    this.carregarTarefas();
  }

  limparFiltros(): void {
    if (this.ocupado() || this.formularioAberto()) return;
    this.filtros.reset();
    this.aplicarFiltros();
  }

  novaTarefa(): void {
    if (this.ocupado() || this.formularioAberto()) return;
    this.tarefaEmEdicao.set(null);
    this.erroFormulario.set('');
    this.erroOperacao.set('');
    this.mensagemSucesso.set('');
    this.formularioAberto.set(true);
  }

  editar(tarefa: TarefaResposta): void {
    if (this.ocupado() || this.formularioAberto()) return;
    this.carregandoEdicao.set(true);
    this.erroOperacao.set('');
    this.mensagemSucesso.set('');
    this.servico.consultar(tarefa.id).pipe(
      takeUntilDestroyed(this.destruidor),
      finalize(() => this.carregandoEdicao.set(false)),
    ).subscribe({
      next: (encontrada) => {
        this.tarefaEmEdicao.set(encontrada);
        this.erroFormulario.set('');
        this.formularioAberto.set(true);
      },
      error: (erro: unknown) => this.erroOperacao.set(this.obterMensagemErro(erro, 'Não foi possível abrir a tarefa. Tente novamente.')),
    });
  }

  cancelarEdicao(): void {
    if (this.salvando()) return;
    this.formularioAberto.set(false);
    this.tarefaEmEdicao.set(null);
    this.erroFormulario.set('');
  }

  salvar(dados: AtualizarTarefaRequisicao): void {
    if (this.ocupado()) return;
    this.salvando.set(true);
    this.erroFormulario.set('');
    const tarefa = this.tarefaEmEdicao();
    const operacao: Observable<unknown> = tarefa
      ? this.servico.atualizar(tarefa.id, dados)
      : this.servico.cadastrar({ titulo: dados.titulo, descricao: dados.descricao, dataVencimento: dados.dataVencimento });

    operacao.pipe(
      takeUntilDestroyed(this.destruidor),
      finalize(() => this.salvando.set(false)),
    ).subscribe({
      next: () => {
        this.formularioAberto.set(false);
        this.tarefaEmEdicao.set(null);
        this.mensagemSucesso.set(tarefa ? 'Tarefa atualizada com sucesso.' : 'Tarefa cadastrada com sucesso.');
        this.carregarTarefas();
      },
      error: (erro: unknown) => this.erroFormulario.set(this.obterMensagemErro(erro, 'Não foi possível salvar a tarefa. Tente novamente.')),
    });
  }

  excluir(tarefa: TarefaResposta): void {
    if (this.ocupado() || this.formularioAberto()) return;
    if (!window.confirm(`Excluir a tarefa "${tarefa.titulo}"? Esta ação não pode ser desfeita.`)) return;
    this.excluindo.set(tarefa.id);
    this.erroOperacao.set('');
    this.mensagemSucesso.set('');
    this.servico.excluir(tarefa.id).pipe(
      takeUntilDestroyed(this.destruidor),
      finalize(() => this.excluindo.set(null)),
    ).subscribe({
      next: () => {
        this.tarefas.update((tarefas) => tarefas.filter((existente) => existente.id !== tarefa.id));
        this.mensagemSucesso.set('Tarefa excluída com sucesso.');
        this.carregarTarefas();
      },
      error: (erro: unknown) => this.erroOperacao.set(this.obterMensagemErro(erro, 'Não foi possível excluir a tarefa. Tente novamente.')),
    });
  }

  rotuloSituacao(situacao: SituacaoTarefa): string {
    return this.situacoes.find((opcao) => opcao.valor === situacao)?.rotulo ?? situacao;
  }

  formatarData(data: string): string {
    const [ano, mes, dia] = data.split('-');
    return `${dia}/${mes}/${ano}`;
  }

  private obterMensagemErro(erro: unknown, padrao: string): string {
    if (erro instanceof HttpErrorResponse) {
      if (erro.status === 0) return 'Não foi possível conectar ao servidor. Verifique a conexão e tente novamente.';
      if (erro.status === 400) return 'Confira os dados informados e tente novamente.';
      if (erro.status === 404) return 'A tarefa não foi encontrada. Ela pode ter sido excluída.';
    }
    return padrao;
  }
}

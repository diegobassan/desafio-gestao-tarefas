import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { of, Subject, throwError } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { Aplicacao } from './aplicacao';
import { TarefaServico } from './tarefas/tarefa-servico';
import { AtualizarTarefaRequisicao, TarefaResposta } from './tarefas/tarefa';

describe('Aplicacao', () => {
  let tela: ComponentFixture<Aplicacao>;
  let componente: Aplicacao;
  const tarefa: TarefaResposta = {
    id: 'df616ee5-dd9d-4b55-ac8a-b33938e89a4b', titulo: 'Revisar testes', descricao: null,
    situacao: 'Pendente', dataVencimento: '2026-11-20',
  };
  const dados: AtualizarTarefaRequisicao = {
    titulo: tarefa.titulo, descricao: null, situacao: 'Pendente', dataVencimento: tarefa.dataVencimento,
  };
  const servico = {
    listar: vi.fn<TarefaServico['listar']>(), consultar: vi.fn<TarefaServico['consultar']>(),
    cadastrar: vi.fn<TarefaServico['cadastrar']>(), atualizar: vi.fn<TarefaServico['atualizar']>(),
    excluir: vi.fn<TarefaServico['excluir']>(),
  };

  beforeEach(async () => {
    vi.resetAllMocks();
    servico.listar.mockReturnValue(of([tarefa]));
    servico.consultar.mockReturnValue(of(tarefa));
    servico.cadastrar.mockReturnValue(of(tarefa));
    servico.atualizar.mockReturnValue(of(undefined));
    servico.excluir.mockReturnValue(of(undefined));
    await TestBed.configureTestingModule({ imports: [Aplicacao], providers: [{ provide: TarefaServico, useValue: servico }] }).compileComponents();
    tela = TestBed.createComponent(Aplicacao);
    componente = tela.componentInstance;
    tela.detectChanges();
    await tela.whenStable();
  });

  afterEach(() => vi.restoreAllMocks());

  it('deve carregar os dados da API e exibir a data sem deslocamento de fuso', () => {
    const elemento = tela.nativeElement as HTMLElement;
    expect(servico.listar).toHaveBeenCalledWith({});
    expect(elemento.textContent).toContain('Revisar testes');
    expect(elemento.querySelector('time')?.textContent).toContain('20/11/2026');
  });

  it('deve aplicar os dois filtros e permitir limpá-los', () => {
    componente.filtros.setValue({ situacao: 'EmAndamento', dataVencimento: '2026-11-20' });
    componente.aplicarFiltros();
    expect(servico.listar).toHaveBeenLastCalledWith({ situacao: 'EmAndamento', dataVencimento: '2026-11-20' });
    componente.limparFiltros();
    expect(servico.listar).toHaveBeenLastCalledWith({});
    expect(componente.filtros.getRawValue()).toEqual({ situacao: '', dataVencimento: '' });
  });

  it('deve cadastrar sem enviar situação e recarregar a lista com os filtros aplicados', () => {
    componente.filtros.controls.situacao.setValue('Pendente');
    componente.aplicarFiltros();
    componente.novaTarefa();
    componente.salvar(dados);
    expect(servico.cadastrar).toHaveBeenCalledWith({ titulo: tarefa.titulo, descricao: null, dataVencimento: '2026-11-20' });
    expect(servico.listar).toHaveBeenLastCalledWith({ situacao: 'Pendente' });
    expect(componente.formularioAberto()).toBe(false);
    expect(componente.mensagemSucesso()).toBe('Tarefa cadastrada com sucesso.');
  });

  it('deve consultar os dados atuais antes de editar e atualizar pela rota original', () => {
    componente.editar(tarefa);
    expect(servico.consultar).toHaveBeenCalledWith(tarefa.id);
    expect(componente.tarefaEmEdicao()).toEqual(tarefa);
    componente.salvar({ ...dados, situacao: 'Concluida' });
    expect(servico.atualizar).toHaveBeenCalledWith(tarefa.id, { ...dados, situacao: 'Concluida' });
    expect(componente.mensagemSucesso()).toBe('Tarefa atualizada com sucesso.');
  });

  it('não deve excluir quando a confirmação for cancelada', () => {
    const confirmar = vi.spyOn(window, 'confirm').mockReturnValue(false);
    componente.excluir(tarefa);
    expect(confirmar).toHaveBeenCalledOnce();
    expect(servico.excluir).not.toHaveBeenCalled();
  });

  it('deve excluir somente após confirmação e atualizar a lista', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    servico.listar.mockReturnValue(of([]));
    componente.excluir(tarefa);
    expect(servico.excluir).toHaveBeenCalledWith(tarefa.id);
    expect(componente.tarefas()).toEqual([]);
    expect(componente.mensagemSucesso()).toBe('Tarefa excluída com sucesso.');
  });

  it('deve exibir erro de comunicação sem apresentar uma lista vazia como sucesso', () => {
    servico.listar.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 0 })));
    componente.carregarTarefas();
    tela.detectChanges();
    const elemento = tela.nativeElement as HTMLElement;
    expect(elemento.textContent).toContain('Não foi possível conectar ao servidor.');
    expect(elemento.textContent).not.toContain('Nenhuma tarefa encontrada');
    expect(componente.carregando()).toBe(false);
  });

  it('deve manter o formulário aberto quando a API rejeitar os dados', () => {
    servico.cadastrar.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 400 })));
    componente.novaTarefa();
    componente.salvar(dados);
    expect(componente.formularioAberto()).toBe(true);
    expect(componente.erroFormulario()).toBe('Confira os dados informados e tente novamente.');
    expect(componente.salvando()).toBe(false);
    expect(componente.mensagemSucesso()).toBe('');
  });

  it('deve impedir envio duplicado enquanto aguarda a API', () => {
    const resposta = new Subject<TarefaResposta>();
    servico.cadastrar.mockReturnValue(resposta);
    componente.novaTarefa();
    componente.salvar(dados);
    componente.salvar(dados);
    expect(componente.salvando()).toBe(true);
    expect(servico.cadastrar).toHaveBeenCalledOnce();
    resposta.next(tarefa);
    resposta.complete();
    expect(componente.salvando()).toBe(false);
  });
});

import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { ambiente } from '../../environments/environment';
import { TarefaServico } from './tarefa-servico';
import { AtualizarTarefaRequisicao, CriarTarefaRequisicao, FiltrosTarefa, TarefaResposta } from './tarefa';

describe('TarefaServico', () => {
  let servico: TarefaServico;
  let requisicoes: HttpTestingController;
  const endereco = `${ambiente.urlApi}/api/tarefas`;
  const tarefa: TarefaResposta = {
    id: 'df616ee5-dd9d-4b55-ac8a-b33938e89a4b', titulo: 'Revisar testes', descricao: null,
    situacao: 'Pendente', dataVencimento: '2026-11-20',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    servico = TestBed.inject(TarefaServico);
    requisicoes = TestBed.inject(HttpTestingController);
  });

  afterEach(() => requisicoes.verify());

  it('deve consultar a lista sem enviar filtros vazios', () => {
    let recebidas: TarefaResposta[] = [];
    servico.listar().subscribe((tarefas) => recebidas = tarefas);
    const requisicao = requisicoes.expectOne(endereco);
    expect(requisicao.request.method).toBe('GET');
    expect(requisicao.request.params.keys()).toEqual([]);
    requisicao.flush([tarefa]);
    expect(recebidas).toEqual([tarefa]);
  });

  it.each<{ filtros: FiltrosTarefa; parametros: Record<string, string> }>([
    { filtros: { situacao: 'Pendente' }, parametros: { situacao: 'Pendente' } },
    { filtros: { dataVencimento: '2026-11-20' }, parametros: { dataVencimento: '2026-11-20' } },
    { filtros: { situacao: 'EmAndamento', dataVencimento: '2026-11-20' }, parametros: { situacao: 'EmAndamento', dataVencimento: '2026-11-20' } },
  ])('deve montar os filtros opcionais e combinados: $parametros', ({ filtros, parametros }) => {
    let recebidas: TarefaResposta[] | undefined;
    servico.listar(filtros).subscribe((tarefas) => recebidas = tarefas);
    const requisicao = requisicoes.expectOne((requisicao) => requisicao.url === endereco);
    expect(requisicao.request.method).toBe('GET');
    expect(requisicao.request.params.keys().sort()).toEqual(Object.keys(parametros).sort());
    for (const [nome, valor] of Object.entries(parametros)) expect(requisicao.request.params.get(nome)).toBe(valor);
    requisicao.flush([]);
    expect(recebidas).toEqual([]);
  });

  it('deve consultar uma tarefa preservando os valores de situação e data', () => {
    let recebida: TarefaResposta | undefined;
    servico.consultar(tarefa.id).subscribe((resposta) => recebida = resposta);
    const requisicao = requisicoes.expectOne(`${endereco}/${tarefa.id}`);
    expect(requisicao.request.method).toBe('GET');
    requisicao.flush(tarefa);
    expect(recebida).toEqual(tarefa);
  });

  it('deve cadastrar com os campos aceitos pela API', () => {
    const dados: CriarTarefaRequisicao = { titulo: tarefa.titulo, descricao: null, dataVencimento: tarefa.dataVencimento };
    let criada: TarefaResposta | undefined;
    servico.cadastrar(dados).subscribe((resposta) => criada = resposta);
    const requisicao = requisicoes.expectOne(endereco);
    expect(requisicao.request.method).toBe('POST');
    expect(requisicao.request.body).toEqual(dados);
    expect(requisicao.request.body).not.toHaveProperty('situacao');
    expect(requisicao.request.body).not.toHaveProperty('id');
    requisicao.flush(tarefa, { status: 201, statusText: 'Created' });
    expect(criada).toEqual(tarefa);
  });

  it('deve atualizar usando o identificador na rota e aceitar resposta sem conteúdo', () => {
    const dados: AtualizarTarefaRequisicao = {
      titulo: 'Revisão concluída', descricao: null, situacao: 'Concluida', dataVencimento: '2026-11-21',
    };
    let concluida = false;
    servico.atualizar(tarefa.id, dados).subscribe(() => concluida = true);
    const requisicao = requisicoes.expectOne(`${endereco}/${tarefa.id}`);
    expect(requisicao.request.method).toBe('PUT');
    expect(requisicao.request.body).toEqual(dados);
    expect(requisicao.request.body).not.toHaveProperty('id');
    requisicao.flush(null, { status: 204, statusText: 'No Content' });
    expect(concluida).toBe(true);
  });

  it('deve excluir e aceitar resposta sem conteúdo', () => {
    let concluida = false;
    servico.excluir(tarefa.id).subscribe(() => concluida = true);
    const requisicao = requisicoes.expectOne(`${endereco}/${tarefa.id}`);
    expect(requisicao.request.method).toBe('DELETE');
    requisicao.flush(null, { status: 204, statusText: 'No Content' });
    expect(concluida).toBe(true);
  });

  it('deve propagar erros HTTP sem transformá-los em uma lista vazia', () => {
    let erroRecebido: HttpErrorResponse | undefined;
    servico.listar().subscribe({ error: (erro: HttpErrorResponse) => erroRecebido = erro });
    requisicoes.expectOne(endereco).flush({ title: 'Erro no servidor' }, { status: 500, statusText: 'Server Error' });
    expect(erroRecebido?.status).toBe(500);
  });
});

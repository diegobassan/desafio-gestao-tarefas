import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ambiente } from '../../environments/environment';
import { AtualizarTarefaRequisicao, CriarTarefaRequisicao, FiltrosTarefa, TarefaResposta } from './tarefa';

@Injectable({ providedIn: 'root' })
export class TarefaServico {
  private readonly http = inject(HttpClient);
  private readonly endereco = `${ambiente.urlApi}/api/tarefas`;

  listar(filtros: FiltrosTarefa = {}): Observable<TarefaResposta[]> {
    let parametros = new HttpParams();
    if (filtros.situacao) parametros = parametros.set('situacao', filtros.situacao);
    if (filtros.dataVencimento) parametros = parametros.set('dataVencimento', filtros.dataVencimento);
    return this.http.get<TarefaResposta[]>(this.endereco, { params: parametros });
  }

  consultar(id: string): Observable<TarefaResposta> {
    return this.http.get<TarefaResposta>(`${this.endereco}/${encodeURIComponent(id)}`);
  }

  cadastrar(requisicao: CriarTarefaRequisicao): Observable<TarefaResposta> {
    return this.http.post<TarefaResposta>(this.endereco, requisicao);
  }

  atualizar(id: string, requisicao: AtualizarTarefaRequisicao): Observable<void> {
    return this.http.put<void>(`${this.endereco}/${encodeURIComponent(id)}`, requisicao);
  }

  excluir(id: string): Observable<void> {
    return this.http.delete<void>(`${this.endereco}/${encodeURIComponent(id)}`);
  }
}

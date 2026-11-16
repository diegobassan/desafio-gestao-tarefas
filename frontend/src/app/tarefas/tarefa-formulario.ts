import { afterNextRender, Component, effect, ElementRef, inject, input, output, viewChild } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AtualizarTarefaRequisicao, SituacaoTarefa, situacoesTarefa, TarefaResposta } from './tarefa';

@Component({
  selector: 'app-tarefa-formulario',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './tarefa-formulario.html',
  styleUrl: './tarefa-formulario.css',
})
export class TarefaFormulario {
  readonly tarefa = input<TarefaResposta | null>(null);
  readonly salvando = input(false);
  readonly erro = input('');
  readonly salvar = output<AtualizarTarefaRequisicao>();
  readonly cancelar = output<void>();
  readonly situacoes = situacoesTarefa;
  private readonly construtor = inject(FormBuilder);
  private readonly campoTitulo = viewChild<ElementRef<HTMLInputElement>>('campoTitulo');

  readonly formulario = this.construtor.nonNullable.group({
    titulo: ['', [Validators.required, Validators.pattern(/\S/), Validators.maxLength(150)]],
    descricao: ['', Validators.maxLength(2000)],
    situacao: this.construtor.nonNullable.control<SituacaoTarefa>('Pendente', Validators.required),
    dataVencimento: ['', Validators.required],
  });

  constructor() {
    effect(() => {
      const tarefa = this.tarefa();
      this.formulario.reset({
        titulo: tarefa?.titulo ?? '',
        descricao: tarefa?.descricao ?? '',
        situacao: tarefa?.situacao ?? 'Pendente',
        dataVencimento: tarefa?.dataVencimento ?? '',
      });
      if (tarefa) this.formulario.controls.situacao.enable({ emitEvent: false });
      else this.formulario.controls.situacao.disable({ emitEvent: false });
    });
    afterNextRender(() => this.campoTitulo()?.nativeElement.focus());
  }

  confirmar(): void {
    if (this.salvando()) return;
    this.formulario.markAllAsTouched();
    if (this.formulario.invalid) return;
    const dados = this.formulario.getRawValue();
    this.salvar.emit({ ...dados, descricao: dados.descricao || null });
  }
}

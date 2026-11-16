import { ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { TarefaFormulario } from './tarefa-formulario';
import { TarefaResposta } from './tarefa';

describe('TarefaFormulario', () => {
  let tela: ComponentFixture<TarefaFormulario>;
  let componente: TarefaFormulario;
  const tarefa: TarefaResposta = {
    id: 'df616ee5-dd9d-4b55-ac8a-b33938e89a4b', titulo: 'Revisar testes', descricao: 'Verificar cenários',
    situacao: 'EmAndamento', dataVencimento: '2026-11-20',
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [TarefaFormulario] }).compileComponents();
    tela = TestBed.createComponent(TarefaFormulario);
    componente = tela.componentInstance;
    tela.detectChanges();
    await tela.whenStable();
  });

  it.each(['', '   ', 'a'.repeat(151)])('deve rejeitar título inválido: %s', (titulo) => {
    componente.formulario.patchValue({ titulo, dataVencimento: '2026-11-20' });
    expect(componente.formulario.controls.titulo.invalid).toBe(true);
  });

  it('deve exigir a data de vencimento', () => {
    componente.formulario.patchValue({ titulo: 'Revisar testes' });
    expect(componente.formulario.controls.dataVencimento.hasError('required')).toBe(true);
  });

  it('deve rejeitar descrição com mais de 2000 caracteres', () => {
    componente.formulario.controls.descricao.setValue('a'.repeat(2001));
    expect(componente.formulario.controls.descricao.hasError('maxlength')).toBe(true);
  });

  it('deve aceitar os limites de tamanho e emitir a data sem conversão de fuso', () => {
    const emitir = vi.spyOn(componente.salvar, 'emit');
    const titulo = 'a'.repeat(150);
    const descricao = 'b'.repeat(2000);
    componente.formulario.patchValue({ titulo, descricao, dataVencimento: '2026-11-20' });
    componente.confirmar();
    expect(emitir).toHaveBeenCalledWith({ titulo, descricao, situacao: 'Pendente', dataVencimento: '2026-11-20' });
  });

  it('deve permitir descrição vazia e enviá-la como nula', () => {
    const emitir = vi.spyOn(componente.salvar, 'emit');
    componente.formulario.patchValue({ titulo: 'Revisar testes', dataVencimento: '2026-11-20' });
    componente.confirmar();
    expect(emitir).toHaveBeenCalledWith({ titulo: 'Revisar testes', descricao: null, situacao: 'Pendente', dataVencimento: '2026-11-20' });
  });

  it('deve iniciar o cadastro como pendente sem permitir alterar a situação', () => {
    expect(componente.formulario.controls.situacao.value).toBe('Pendente');
    expect(componente.formulario.controls.situacao.disabled).toBe(true);
  });

  it('deve carregar os dados da edição sem expor o identificador no formulário', async () => {
    tela.componentRef.setInput('tarefa', tarefa);
    tela.detectChanges();
    await tela.whenStable();
    expect(componente.formulario.getRawValue()).toEqual({
      titulo: tarefa.titulo, descricao: tarefa.descricao, situacao: tarefa.situacao, dataVencimento: tarefa.dataVencimento,
    });
    expect(componente.formulario.controls.situacao.enabled).toBe(true);
    const emitir = vi.spyOn(componente.salvar, 'emit');
    componente.formulario.controls.situacao.setValue('Concluida');
    componente.confirmar();
    expect(emitir).toHaveBeenCalledWith({
      titulo: tarefa.titulo, descricao: tarefa.descricao, situacao: 'Concluida', dataVencimento: tarefa.dataVencimento,
    });
  });

  it('deve impedir envio inválido e apresentar mensagens de validação', () => {
    const emitir = vi.spyOn(componente.salvar, 'emit');
    componente.confirmar();
    tela.detectChanges();
    expect(emitir).not.toHaveBeenCalled();
    const elemento = tela.nativeElement as HTMLElement;
    expect(elemento.textContent).toContain('Informe o título da tarefa.');
    expect(elemento.textContent).toContain('Informe a data de vencimento.');
  });

  it('deve impedir envio duplicado enquanto salva', () => {
    componente.formulario.patchValue({ titulo: 'Revisar testes', dataVencimento: '2026-11-20' });
    tela.componentRef.setInput('salvando', true);
    tela.detectChanges();
    const emitir = vi.spyOn(componente.salvar, 'emit');
    componente.confirmar();
    expect(emitir).not.toHaveBeenCalled();
    expect((tela.nativeElement as HTMLElement).querySelector('fieldset')?.disabled).toBe(true);
  });
});

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GestaoTarefas.Api.Filtros;

public class ValidacaoFiltro : IExceptionFilter
{
    public void OnException(ExceptionContext contexto)
    {
        if (contexto.Exception is not ArgumentException erro)
        {
            return;
        }

        var detalhes = new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            [erro.ParamName ?? "dados"] = [erro.Message]
        })
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Dados inválidos."
        };

        contexto.Result = new BadRequestObjectResult(detalhes);
        contexto.ExceptionHandled = true;
    }
}

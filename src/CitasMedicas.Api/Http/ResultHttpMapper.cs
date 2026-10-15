using CitasMedicas.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace CitasMedicas.Api.Http;

public static class ResultHttpMapper
{
    public static IResult ToProblemDetails(Error error)
    {
        var statusCode = error.Kind switch
        {
            ErrorKind.Validation => StatusCodes.Status400BadRequest,
            ErrorKind.NotFound => StatusCodes.Status404NotFound,
            ErrorKind.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        var title = error.Kind switch
        {
            ErrorKind.Validation => "Error de validación",
            ErrorKind.NotFound => "Recurso no encontrado",
            ErrorKind.Conflict => "Conflicto de negocio",
            _ => "Error interno"
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = error.Message,
            Type = $"https://httpstatuses.io/{statusCode}",
            Extensions =
            {
                ["code"] = error.Code
            }
        };

        return Results.Problem(problemDetails);
    }

    public static IResult ToHttpResult(this Result result)
    {
        if (result.IsSuccess)
            return Results.Ok();

        return ToProblemDetails(result.Error);
    }

    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult>? onSuccess = null)
    {
        if (result.IsSuccess)
        {
            return onSuccess is not null
                ? onSuccess(result.Value)
                : Results.Ok(result.Value);
        }

        return ToProblemDetails(result.Error);
    }
}

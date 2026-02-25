using C4.Shared.Kernel;
using FluentValidation;
using MediatR;

namespace C4.Shared.Infrastructure.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(error => error is not null)
            .ToList();

        if (failures.Count == 0)
        {
            return await next();
        }

        if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var error = new Error("validation.failed", string.Join("; ", failures.Select(f => f.ErrorMessage)));
            var responseType = typeof(TResponse).GetGenericArguments()[0];
            var resultType = typeof(Result<>).MakeGenericType(responseType);
            var failureMethod = resultType.GetMethod(nameof(Result<object>.Failure), [typeof(Error)]);
            if (failureMethod is not null)
            {
                var result = failureMethod.Invoke(null, [error]);
                if (result is TResponse typed)
                {
                    return typed;
                }
            }
        }

        throw new ValidationException(failures);
    }
}

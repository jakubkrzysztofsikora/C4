namespace C4.Shared.Kernel;

public readonly record struct Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
}

public sealed class Result<T>
{
    private Result(T? value, bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new ArgumentException("Successful result cannot contain an error.", nameof(error));
        }

        if (!isSuccess && error == Error.None)
        {
            throw new ArgumentException("Failed result must contain an error.", nameof(error));
        }
        IsSuccess = isSuccess;
        ValueOrDefault = value;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess
        ? ValueOrDefault!
        : throw new InvalidOperationException("Failed result does not have a value.");

    public T? ValueOrDefault { get; }

    public Error Error { get; }

    public static Result<T> Success(T value) => new(value, true, Error.None);

    public static Result<T> Failure(Error error) => new(default, false, error);

    public Result<TNext> Map<TNext>(Func<T, TNext> map) =>
        IsSuccess ? Result<TNext>.Success(map(Value)) : Result<TNext>.Failure(Error);

    public Result<TNext> Bind<TNext>(Func<T, Result<TNext>> bind) =>
        IsSuccess ? bind(Value) : Result<TNext>.Failure(Error);
}

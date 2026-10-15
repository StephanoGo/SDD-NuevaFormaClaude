namespace CitasMedicas.Domain.Common;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error is not null)
            throw new InvalidOperationException("Success result cannot have an error.");
        if (!isSuccess && error is null)
            throw new InvalidOperationException("Failure result must have an error.");

        IsSuccess = isSuccess;
        Error = error!;
    }

    public static Result Success() => new(true, default!);
    public static Result Failure(Error error) => new(false, error);

    public static implicit operator Result(Error error) => Failure(error);
}

public class Result<T> : Result
{
    private readonly T? _value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot access Value of a failure result. Error: {Error?.Code}");

    protected Result(T value) : base(true, default!)
    {
        _value = value;
    }

    protected Result(Error error) : base(false, error)
    {
        _value = default;
    }

    public static Result<T> Success(T value) => new(value);
    public new static Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}

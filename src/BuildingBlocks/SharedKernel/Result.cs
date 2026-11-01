using System.Diagnostics.CodeAnalysis;

namespace SharedKernel;

/// <summary>A rule violation, identified by a stable code such as <c>period.invalid</c>.</summary>
public sealed record Error(string Code, string Message);

/// <summary>Outcome of an operation that can break a business rule, without using exceptions.</summary>
public class Result
{
    protected Result(Error? error)
    {
        Error = error;
    }

    public Error? Error { get; }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    [MemberNotNullWhen(true, nameof(Error))]
    public bool IsFailure => Error is not null;

    public static Result Success() => new(null);

    public static Result Failure(Error error) => new(error);

    public static Result<TValue> Success<TValue>(TValue value) => Result<TValue>.Success(value);

    public static Result<TValue> Failure<TValue>(Error error) => Result<TValue>.Failure(error);

    public static implicit operator Result(Error error) => Failure(error);
}

public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    private Result(TValue? value, Error? error)
        : base(error)
    {
        _value = value;
    }

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot read the value of a failed result ({Error.Code}).");

    public static Result<TValue> Success(TValue value) => new(value, null);

    public static new Result<TValue> Failure(Error error) => new(default, error);

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public static implicit operator Result<TValue>(Error error) => Failure(error);
}

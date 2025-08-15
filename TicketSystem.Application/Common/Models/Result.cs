using static System.Net.Mime.MediaTypeNames;

namespace TicketSystem.Application.Common.Models;

public class Result
{
    public bool IsSuccess { get; private set; }
    public string[] Errors { get; private set; } = Array.Empty<string>();

    protected Result(bool isSuccess, string[] errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public static Result Success() => new(true, Array.Empty<string>());
    public static Result Failure(params string[] errors) => new(false, errors);
    public static Result Failure(IEnumerable<string> errors) => new(false, errors.ToArray());
}

public class Result<T> : Result
{
    public T? Data { get; private set; }

    private Result(bool isSuccess, T? data, string[] errors) : base(isSuccess, errors)
    {
        Data = data;
    }

    public static Result<T> Success(T data) => new(true, data, Array.Empty<string>());
    public static new Result<T> Failure(params string[] errors) => new(false, default, errors);
    public static new Result<T> Failure(IEnumerable<string> errors) => new(false, default, errors.ToArray());
}
namespace ProductCacheApi.Features.Products;

public enum ResultError
{
    None,
    NotFound,
    Validation,
    Conflict
}

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Data { get; }
    public string? Error { get; }
    public ResultError ErrorType { get; }

    private Result(bool isSuccess, T? data, string? error, ResultError errorType)
    {
        IsSuccess = isSuccess;
        Data = data;
        Error = error;
        ErrorType = errorType;
    }

    public static Result<T> Success(T data) => new(true, data, null, ResultError.None);

    public static Result<T> Failure(string error, ResultError errorType) =>
        new(false, default, error, errorType);
}

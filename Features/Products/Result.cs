namespace ProductCacheApi.Features.Products;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T Data { get; }
    public string Error { get; }

    private Result(bool success, T data, string error)
    {
        IsSuccess = success;
        Data = data;
        Error = error;
    }

    public static Result<T> Success(T data) => new(true, data, null);
    public static Result<T?> Failure(string error) => new(false, default, error);
}

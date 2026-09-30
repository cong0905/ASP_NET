namespace ShopNet.Application.Common.Models;

public class Result<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public string[] Errors { get; set; } = Array.Empty<string>();
    public int StatusCode { get; set; } = 200;

    public static Result<T> Success(T data, string message = "", int statusCode = 200)
    {
        return new Result<T>
        {
            IsSuccess = true,
            Data = data,
            Message = message,
            StatusCode = statusCode
        };
    }

    public static Result<T> Failure(string message, int statusCode = 400, params string[] errors)
    {
        return new Result<T>
        {
            IsSuccess = false,
            Data = default,
            Message = message,
            StatusCode = statusCode,
            Errors = errors.Length > 0 ? errors : new[] { message }
        };
    }
}

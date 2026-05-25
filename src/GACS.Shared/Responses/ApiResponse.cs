namespace GACS.Shared.Responses;

public sealed record ApiResponse<T>
{
    public T? Data { get; init; }
    public bool Success { get; init; }
    public string? Message { get; init; }
    public IEnumerable<string>? Errors { get; init; }

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Data = data, Success = true, Message = message };

    public static ApiResponse<T> Fail(IEnumerable<string> errors, string? message = null) =>
        new() { Success = false, Message = message, Errors = errors };

    public static ApiResponse<T> Fail(string error) =>
        Fail([error]);
}

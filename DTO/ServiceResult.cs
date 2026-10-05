namespace SankalpAPI.DTO
{
    public class ServiceResult<T>
    {
        public int StatusCode { get; set; }
        public T Data { get; set; } = default!;

        public static ServiceResult<T> Ok(int statusCode, T data) => new() { StatusCode = statusCode, Data = data };
    }
}

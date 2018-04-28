namespace Badoucai.WebApi.Models
{
    public class ResponseModels
    {
        public string Code { get; set; } = "10000";

        public string Message { get; set; } = "Success";
    }

    public class ResponseModels<T> : ResponseModels where T : class
    {
        public ResponseModels(T data)
        {
            Data = data;
        }

        public T Data { get; set; }
    }
}
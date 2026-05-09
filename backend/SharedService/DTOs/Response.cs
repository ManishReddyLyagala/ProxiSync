using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedService.DTOs
{
    public class Response<T>
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        public static Response<T> Ok(T data, string? message = null)
            => new Response<T> { Success = true, Data = data, Message = message };

        public static Response<T> Fail(string message)
            => new Response<T> { Success = false, Message = message };
    }
}

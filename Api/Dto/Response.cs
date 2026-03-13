using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Api.Dto;

public class Response<TData>
{
    public Response(
        TData? data,
        string? message = null)
    {
        Data = data;
        Message = message;
    }
    
    //public Response(string message, List<string> errors)
    //{
    //    Data = default; 
    //    Message = message;
    //    Errors = errors;
    //}

    public TData? Data { get; set; }
    public string? Message { get; set; }
    
    //public List<string>? Errors { get; set; }
}
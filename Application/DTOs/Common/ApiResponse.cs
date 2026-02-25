using System.ComponentModel.DataAnnotations;

namespace GoogleClass.DTOs.Common;

public class ApiResponse<T>
{
    [Required]
    public ApiResponseType Type { get; set; }
    
    public string? Message { get; set; }
    
    public T? Data { get; set; }
}
using System.ComponentModel.DataAnnotations;

namespace GoogleClass.DTOs.Common;

public class PagedResponse<T>
{
    [Required]
    public ICollection<T> Records { get; set; } = new List<T>();
    
    [Required]
    public int TotalRecords { get; set; }
}
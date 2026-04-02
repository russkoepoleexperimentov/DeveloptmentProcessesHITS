using GoogleClass.DTOs.Common;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Post
{
    public class CreateUpdatePostDto
    {
        [Required]
        public PostType Type { get; set; }

        [Required]
        public string Title { get; set; } = null!;

        public string Text { get; set; } = null!;

        public DateTime? Deadline { get; set; } = null;

        public int? MaxScore { get; set; } = null;

        public TaskType? TaskType { get; set; } = null;

        public bool? SolvableAfterDeadline { get; set; } = null;

        public List<Guid>? Files { get; set; } = null;
        
        public int? MinTeamSize { get; set; } = null;
        public int? MaxTeamSize { get; set; } = null;
    }
}

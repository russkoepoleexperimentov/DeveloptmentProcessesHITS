using GoogleClass.DTOs.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}

using GoogleClass.DTOs.Course;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Post
{
    public class FeedResponseDto
    {
        public List<CourseFeedItemDto> Records { get; set; } = new();
        public int TotalRecords { get; set; }
    }
}

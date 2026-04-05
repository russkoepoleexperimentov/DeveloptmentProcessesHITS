using GoogleClass.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    namespace Domain.Models
    {
        public class CaptainVote : BaseEntityWithId
        {
            public Guid TeamId { get; set; }
            public virtual Team Team { get; set; } = null!;
            public Guid VoterId { get; set; }
            public virtual User Voter { get; set; } = null!;
            public Guid CandidateId { get; set; }
            public virtual User Candidate { get; set; } = null!;
            public DateTime VotedAt { get; set; } = DateTime.UtcNow;
        }
    }
}

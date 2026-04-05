using Application.Services.Abstractions;
using Domain.Models;
using GoogleClass.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Implementations.CaptainVoting
{
    public class TeacherFixedStrategy : ICaptainAssignmentStrategy
    {
        public Task<Guid?> DetermineCaptainAsync(Team team, TeamAssignment assignment)
        {
            if (team.FixedCaptainId.HasValue && team.Members.Any(m => m.UserId == team.FixedCaptainId.Value))
                return Task.FromResult(team.FixedCaptainId);
            return Task.FromResult<Guid?>(null);
        }
        public bool CanStudentTransfer => false;
    }
}

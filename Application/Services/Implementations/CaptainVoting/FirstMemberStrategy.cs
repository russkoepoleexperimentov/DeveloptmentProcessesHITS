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
    public class FirstMemberStrategy : ICaptainAssignmentStrategy
    {
        public Task<Guid?> DetermineCaptainAsync(Team team, TeamAssignment assignment)
        {
            var first = team.Members.OrderBy(m => m.JoinedAt).FirstOrDefault();
            return Task.FromResult(first?.UserId);
        }
        public bool CanStudentTransfer => true;
    }
}

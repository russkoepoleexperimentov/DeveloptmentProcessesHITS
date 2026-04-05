using Application.Services.Abstractions;
using Domain.Models;
using GoogleClass.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Implementations.CaptainVoting
{
    public class VotingAndLotteryStrategy : ICaptainAssignmentStrategy
    {
        private readonly GcDbContext _context;
        public VotingAndLotteryStrategy(GcDbContext context) => _context = context;

        public async Task<Guid?> DetermineCaptainAsync(Team team, TeamAssignment assignment)
        {
            var votes = await _context.CaptainVotes.Where(v => v.TeamId == team.Id).ToListAsync();
            if (!votes.Any()) return null;

            var grouped = votes.GroupBy(v => v.CandidateId)
                .Select(g => new { Candidate = g.Key, Count = g.Count() })
                .ToList();
            int maxVotes = grouped.Max(g => g.Count);
            var winners = grouped.Where(g => g.Count == maxVotes).Select(g => g.Candidate).ToList();

            return winners.Count == 1 ? winners.First() : winners.OrderBy(x => Guid.NewGuid()).First();
        }
        public bool CanStudentTransfer => false;
    }
}

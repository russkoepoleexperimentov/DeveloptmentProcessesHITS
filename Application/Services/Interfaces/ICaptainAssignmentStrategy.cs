using Domain.Models;
using GoogleClass.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Abstractions
{
    public interface ICaptainAssignmentStrategy
    {
        Task<Guid?> DetermineCaptainAsync(Team team, TeamAssignment assignment);
        bool CanStudentTransfer { get; }
    }
}
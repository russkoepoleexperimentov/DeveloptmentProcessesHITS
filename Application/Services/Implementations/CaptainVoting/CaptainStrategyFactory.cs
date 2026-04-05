using Application.Services.Abstractions;
using Application.Services.Interfaces;
using GoogleClass.DTOs.Common;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Implementations.CaptainVoting
{
    public class CaptainStrategyFactory : ICaptainStrategyFactory
    {
        private readonly IServiceProvider _serviceProvider;
        public CaptainStrategyFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public ICaptainAssignmentStrategy GetStrategy(CaptainSelectionMode mode)
        {
            return mode switch
            {
                CaptainSelectionMode.FirstMember => _serviceProvider.GetRequiredService<FirstMemberStrategy>(),
                CaptainSelectionMode.TeacherFixed => _serviceProvider.GetRequiredService<TeacherFixedStrategy>(),
                CaptainSelectionMode.VotingAndLottery => _serviceProvider.GetRequiredService<VotingAndLotteryStrategy>(),
                _ => _serviceProvider.GetRequiredService<FirstMemberStrategy>()
            };
        }
    }
}

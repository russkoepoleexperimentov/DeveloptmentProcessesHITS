using Application.Services.Abstractions;
using GoogleClass.DTOs.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Interfaces
{
    public interface ICaptainStrategyFactory
    {
        ICaptainAssignmentStrategy GetStrategy(CaptainSelectionMode mode);
    }
}

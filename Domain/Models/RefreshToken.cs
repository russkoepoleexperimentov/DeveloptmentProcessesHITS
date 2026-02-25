using GoogleClass.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class RefreshToken : BaseEntityWithId
    {
        public Guid UserId { get; set; }
        public string Token { get; set; } = null!;
        public DateTime Expiration { get; set; }
        public bool IsRevoked { get; set; } = false;
    }
}

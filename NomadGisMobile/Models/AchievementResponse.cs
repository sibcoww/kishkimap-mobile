using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NomadGisMobile.Models
{
    public class AchievementResponse
    {
        public string? Id { get; set; }
        public string? Code { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int RewardPoints { get; set; }
        public string? BadgeImageUrl { get; set; }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NomadGisMobile.Models
{
    public class ProfileMeDto
    {
        public string? Id { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }

        // как в API: уровень и опыт
        public int Level { get; set; }
        public int Experience { get; set; }

        public string? AvatarUrl { get; set; }
    }
}


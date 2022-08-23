using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllianceDiscordController.Models
{
    public class Config
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Hostname { get; set; } = "localhost";
        public int Port { get; set; }

    }
}

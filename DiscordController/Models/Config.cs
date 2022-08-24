using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllianceDiscordController.Models
{
    public class Config
    {
        public string Username { get; set; } = "Username";
        public string Password { get; set; } = "Password";
        public string Hostname { get; set; } = "localhost";
        public int Port { get; set; } = 0;

    }
}

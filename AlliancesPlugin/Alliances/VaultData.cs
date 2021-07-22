using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;

namespace AlliancesPlugin.Alliances
{
    public class VaultData
    {
        [BsonId]
        public string Id { get; set; }
      
        public int count { get; set; }

    }
}

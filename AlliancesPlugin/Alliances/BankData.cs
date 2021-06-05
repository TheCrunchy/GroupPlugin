using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlliancesPlugin.Alliances
{
    public class BankData
    {
        [BsonId]
        public Guid Id { get; set; }

        public long balance { get; set; }

    }

}

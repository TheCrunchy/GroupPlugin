using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrunchGroup.Handlers;
using CrunchGroup.Territories.Interfaces;

namespace CrunchGroup.Territories.PointOwners
{
    public class GroupPointOwner : IPointOwner
    {
        public Guid GroupId;
        public object GetOwner()
        {
            return GroupHandler.GetGroupById(GroupId) ?? null;
        }
    }
}

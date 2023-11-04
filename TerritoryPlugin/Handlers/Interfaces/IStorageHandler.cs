using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Territory.Models;

namespace Territory.Handlers
{
    public interface IStorageHandler
    {
        public void Save(Group group);
        public void Delete(Group group);
        public void Load(Guid groupId);
        public void LoadAll();
    }
}

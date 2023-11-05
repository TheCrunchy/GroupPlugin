using System;
using CrunchGroup.Models;

namespace CrunchGroup.Handlers.Interfaces
{
    public interface IStorageHandler
    {
        public void Save(Group group);
        public void Delete(Group group);
        public void Load(Guid groupId);
        public void LoadAll();
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Territory.Models;

namespace Territory.Handlers
{
    public class JsonStorageHandler : IStorageHandler
    {
        private FileUtils utils = new FileUtils();

        public JsonStorageHandler()
        {
            Directory.CreateDirectory($"{TerritoryPlugin.path}/GroupData/");
            Directory.CreateDirectory($"{TerritoryPlugin.path}/Archive/");
            LoadAll();
        }

        private static readonly string groupBase = $"{TerritoryPlugin.path}/GroupData/";
        private static readonly string groupArchive = $"{TerritoryPlugin.path}/Archive/";

        public void Save(Group group)
        {
            utils.WriteToJsonFile($"{groupBase}/{group.GroupId}.json", group);
        }

        public void Delete(Group group)
        {
            if (File.Exists($"{groupBase}/{group.GroupId}.json"))
            {
                File.Move($"{groupBase}/{group.GroupId}.json", $"{groupArchive}/{group.GroupId}.json");
            }
        }

        public void Load(Guid groupId)
        {
            var path = $"{groupBase}/{groupId}.json";
            if (!File.Exists(path)) return;
            try
            {
                var group = utils.ReadFromJsonFile<Group>(path);
                GroupHandler.AddGroup(group);
            }
            catch (Exception e)
            {
                TerritoryPlugin.Log.Error($"Error loading file {path} {e}");
            }
        }
        public void Load(string path)
        {
            if (!File.Exists(path)) return;
            try
            {
                var group = utils.ReadFromJsonFile<Group>(path);
                GroupHandler.AddGroup(group);
            }
            catch (Exception e)
            {
                TerritoryPlugin.Log.Error($"Error loading file {path} {e}");
            }
        }

        public void LoadAll()
        {
            foreach (var path in Directory.GetFiles(groupBase))
            {
                Load(path);
            }
        }
    }
}

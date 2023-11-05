using System;
using System.IO;
using CrunchGroup.Handlers.Interfaces;
using CrunchGroup.Models;

namespace CrunchGroup.Handlers
{
    public class JsonStorageHandler : IStorageHandler
    {
        private FileUtils utils = new FileUtils();

        public JsonStorageHandler()
        {
            Directory.CreateDirectory($"{GroupPlugin.path}/GroupData/");
            Directory.CreateDirectory($"{GroupPlugin.path}/Archive/");
            LoadAll();
        }

        private static readonly string groupBase = $"{GroupPlugin.path}/GroupData/";
        private static readonly string groupArchive = $"{GroupPlugin.path}/Archive/";

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
                GroupPlugin.Log.Error($"Error loading file {path} {e}");
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
                GroupPlugin.Log.Error($"Error loading file {path} {e}");
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

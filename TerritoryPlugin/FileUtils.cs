using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CrunchGroup
{
    public class FileUtils
    {

        public void WriteToJsonFile<T>(string filePath, T objectToWrite, bool append = false) where T : new()
        {
            TextWriter writer = null;
            try
            {
                var contentsToWriteToFile = JsonConvert.SerializeObject(objectToWrite, new JsonSerializerSettings()
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    Binder = new MySerializationBinder(),
                    Formatting = Newtonsoft.Json.Formatting.Indented
                });
                writer = new StreamWriter(filePath, append);
                writer.Write(contentsToWriteToFile);
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }

        public T ReadFromJsonFile<T>(string filePath) where T : new()
        {
            try
            {
                using (var fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                using (var reader = new StreamReader(fs))
                {
                    var fileContents = reader.ReadToEnd();
                    return JsonConvert.DeserializeObject<T>(fileContents, new JsonSerializerSettings()
                    {
                        TypeNameHandling = TypeNameHandling.Auto,
                        Binder = new MySerializationBinder(),
                        Formatting = Newtonsoft.Json.Formatting.Indented
                    });
                }
            }
            catch (Exception e)
            {
                Core.Log.Error($"Error reading file, moved to backups");
                Core.Log.Error($"Error reading file {filePath} {e}");

                Directory.CreateDirectory($"{Core.path}/ErroredFileBackups/");
                File.Move(filePath, $"{Core.path}/ErroredFileBackups/{Path.GetFileNameWithoutExtension(filePath)}-{DateTime.Now:HH-mm-ss-dd-MM-yyyy}.json");

                return new T();
            }

        }


        public void WriteToXmlFile<T>(string filePath, T objectToWrite, bool append = false) where T : new()
        {
            TextWriter writer = null;
            try
            {
                var serializer = new XmlSerializer(typeof(T));
                writer = new StreamWriter(filePath, append);
                serializer.Serialize(writer, objectToWrite);
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }

        public T ReadFromXmlFile<T>(string filePath) where T : new()
        {
            TextReader reader = null;
            try
            {
                var serializer = new XmlSerializer(typeof(T));
                reader = new StreamReader(filePath);
                return (T)serializer.Deserialize(reader);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
        }
    }
    class MySerializationBinder : DefaultSerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            var t = Core.myAssemblies.Select(x => x)
                .SelectMany(x => x.GetTypes()).FirstOrDefault(x => x.FullName == typeName);

            return t;
        }
    }
}


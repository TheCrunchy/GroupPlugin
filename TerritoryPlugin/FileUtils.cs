using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis;
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
   
            // Check if the type is already loaded in the current AppDomain
            Type existingType = Type.GetType(typeName);

            if (existingType != null)
            {
                // Type is already loaded, return it
             ///   Core.Log.Info($"Type '{typeName}' is already loaded in the current project.");
                return existingType;
            }

            // Type is not loaded, try to load it from specified assemblies
            var t = Core.myAssemblies
                .SelectMany(x => x.GetTypes())
                .FirstOrDefault(x => x.FullName == typeName);

            if (t == null)
            {
                Core.Log.Info($"Cannot resolve type {typeName}");
            }

            return t;
        }
    }
}


using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;

public static class XMLSerializer<T> where T : class, new()
{
    public static void WriteToFile(string filePath, List<T> items)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            using (var writer = new StreamWriter(filePath))
            {
                var serializer = new XmlSerializer(typeof(List<T>));
                serializer.Serialize(writer, items);
            }
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to write XML file at {filePath}", ex);
        }
    }

    public static List<T> ReadFromFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return new List<T>();

            using (var reader = new StreamReader(filePath))
            {
                var serializer = new XmlSerializer(typeof(List<T>));
                var result = serializer.Deserialize(reader) as List<T>;
                return result ?? new List<T>();
            }
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to read XML file from {filePath}", ex);
        }
    }

    public static Task WriteToFileAsync(string filePath, List<T> items) => Task.Run(() => WriteToFile(filePath, items));
    public static Task<List<T>> ReadFromFileAsync(string filePath) => Task.Run(() => ReadFromFile(filePath));
}
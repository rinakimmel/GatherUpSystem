using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml;

public static class XMLDocManager
{
    public static XDocument CreateDocument(string rootName)
    {
        return new XDocument(new XElement(rootName));
    }

    public static XElement? FindElementById(XDocument doc, string elementName, int id)
    {
        return doc.Root?.Elements(elementName)
            .FirstOrDefault(e => (int?)e.Attribute("Id") == id);
    }

    public static void AddElement(XDocument doc, XElement element)
    {
        doc.Root?.Add(element);
    }

    public static void RemoveElement(XDocument doc, XElement element)
    {
        element.Remove();
    }


    public static List<XElement> GetAllElements(XDocument doc, string elementName)
    {
        return doc.Root?.Elements(elementName).ToList() ?? new List<XElement>();
    }

    public static void SaveDocument(XDocument doc, string filePath)
    {
        var settings = new XmlWriterSettings { Indent = true, IndentChars = "  " };
        using (var writer = XmlWriter.Create(filePath, settings))
        {
            doc.Save(writer);
        }
    }

    public static XDocument LoadDocument(string filePath)
    {
        if (!File.Exists(filePath))
            return CreateDocument("root");
        return XDocument.Load(filePath);
    }
}
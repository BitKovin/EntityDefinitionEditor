using EntityEditor.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace EntityEditor.Services;

public static class EntParser
{
    // ─── Import ───────────────────────────────────────────────────────────

    public static ProjectData ImportFromEnt(string path)
    {
        var xml = File.ReadAllText(path);

        // Normalize start (remove BOM, whitespace, etc.)
        xml = xml.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');

        // Remove XML declaration if present (must be first node)
        if (xml.StartsWith("<?xml", StringComparison.Ordinal))
        {
            int end = xml.IndexOf("?>", StringComparison.Ordinal);
            if (end >= 0)
                xml = xml.Substring(end + 2).TrimStart();
        }

        XDocument doc;

        try
        {
            // Try parsing directly (preferred, since your file already has <classes>)
            doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
        }
        catch
        {
            // Fallback: wrap in root if something is malformed
            var wrapped = $"<root>{xml}</root>";
            doc = XDocument.Parse(wrapped, LoadOptions.PreserveWhitespace);
        }

        var project = new ProjectData();

        var root = doc.Root;
        if (root == null)
            return project;

        foreach (var el in root.Elements())
        {
            var tag = el.Name.LocalName.ToLower();
            if (tag != "point" && tag != "group") continue;

            var entity = new EntityData
            {
                Name = el.Attribute("name")?.Value ?? "unnamed",
                EntityType = tag,
                Description = GetTextContent(el),
            };

            // Color
            var colorStr = el.Attribute("color")?.Value;
            if (colorStr != null)
                entity.Color = ParseDoubles(colorStr, 3) ?? entity.Color;

            // Box
            var boxStr = el.Attribute("box")?.Value;
            if (boxStr != null)
                entity.Box = ParseDoubles(boxStr, 6);

            // Properties
            foreach (var prop in el.Elements())
            {
                var pd = ParseProperty(prop);
                if (pd != null)
                    entity.Properties.Add(pd);
            }

            project.Entities.Add(entity);
        }

        return project;
    }

    private static PropertyData? ParseProperty(XElement el)
    {
        var tag = el.Name.LocalName.ToLower();
        var key = el.Attribute("key")?.Value;
        if (string.IsNullOrEmpty(key)) return null;

        var pd = new PropertyData
        {
            Key = key,
            DisplayName = el.Attribute("name")?.Value ?? key,
            DefaultValue = el.Attribute("value")?.Value ?? "",
            Description = GetTextContent(el),
        };

        pd.Type = tag switch
        {
            "string"  => PropertyType.String,
            "integer" => PropertyType.Integer,
            "real"    => PropertyType.Real,
            "real3"   => PropertyType.Real3,
            "angle"   => PropertyType.Angle,
            "angles"  => PropertyType.Angles,
            "color"   => PropertyType.Color,
            "model"   => PropertyType.Model,
            "texture" => PropertyType.Texture,
            "boolean" => PropertyType.Boolean,
            "target"  => PropertyType.Target,
            "sound"   => PropertyType.Sound,
            "array"   => PropertyType.Array,
            "flag"    => PropertyType.Flag,
            _         => PropertyType.String,
        };

        if (pd.Type == PropertyType.Flag)
        {
            if (int.TryParse(el.Attribute("bit")?.Value, out int bit))
                pd.BitIndex = bit;
        }

        return pd;
    }

    private static string GetTextContent(XElement el)
    {
        var sb = new StringBuilder();
        foreach (var node in el.Nodes())
        {
            if (node is XText txt)
            {
                var t = txt.Value.Trim();
                if (!string.IsNullOrEmpty(t)) sb.Append(t).Append(' ');
            }
        }
        return sb.ToString().Trim();
    }

    private static double[]? ParseDoubles(string s, int count)
    {
        var parts = s.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < count) return null;
        var result = new double[count];
        for (int i = 0; i < count; i++)
            if (!double.TryParse(parts[i], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out result[i]))
                return null;
        return result;
    }

    // ─── Export ───────────────────────────────────────────────────────────

    public static void ExportToEnt(string path, ProjectData data)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "    ",
            OmitXmlDeclaration = false,
            Encoding = new UTF8Encoding(false),
        };

        using var writer = XmlWriter.Create(path, settings);
        writer.WriteStartDocument();
        writer.WriteStartElement("classes");

        foreach (var entity in data.Entities)
        {
            if (entity.EntityType == "abstract") continue; // skip abstract base classes

            var tag = entity.EntityType == "group" ? "group" : "point";

            var colorStr = entity.Color != null
                ? string.Join(" ", entity.Color.Select(v => v.ToString("G", System.Globalization.CultureInfo.InvariantCulture)))
                : "0.5 0.5 0.5";

            writer.WriteStartElement(tag);
            writer.WriteAttributeString("name", entity.Name);
            writer.WriteAttributeString("color", colorStr);

            if (tag == "point" && entity.Box is { Length: 6 })
            {
                var boxStr = string.Join(" ", entity.Box.Select(v => v.ToString("G", System.Globalization.CultureInfo.InvariantCulture)));
                writer.WriteAttributeString("box", boxStr);
            }

            if (!string.IsNullOrWhiteSpace(entity.Description))
                writer.WriteString("\n" + entity.Description + "\n");


            List<PropertyData> allProperties = new List<PropertyData>();

            AddParentProperties(entity, data.Entities.ToDictionary(e => e.Name), allProperties, new HashSet<string>());

            // Write all own properties (not inherited)
            foreach (var prop in allProperties)
            {
                WriteProperty(writer, prop);
            }


            writer.WriteEndElement();
        }

        writer.WriteEndElement();
        writer.WriteEndDocument();
    }

    private static void AddParentProperties(EntityData entity, Dictionary<string, EntityData> entityMap, List<PropertyData> result, HashSet<string> visited)
    {
        if (visited.Contains(entity.Name))
            return; // prevent cycles
        visited.Add(entity.Name);

        foreach (var prop in entity.Properties)
        {
            if (!result.Any(p => p.Key == prop.Key))
                result.Add(prop);
        }

        //auomatically add classname property if doesn't exist
        bool hasClassname = result.Any(p => p.Key == "classname");
        if(hasClassname == false)
        {
            result.Add(new PropertyData
            {
                Key = "classname",
                DisplayName = "Classname",
                Type = PropertyType.String,
                DefaultValue = entity.Name,
                Description = "The classname of the entity.",
                IsBuiltIn = true
            });
        }


        foreach (var parentName in entity.Inherits)
        {
            if (entityMap.TryGetValue(parentName, out var parent))
            {
                AddParentProperties(parent, entityMap, result, visited);
            }
        }
    }

    private static void WriteProperty(XmlWriter writer, PropertyData prop)
    {
        var tag = prop.Type switch
        {
            PropertyType.String  => "string",
            PropertyType.Integer => "integer",
            PropertyType.Real    => "real",
            PropertyType.Real3   => "real3",
            PropertyType.Angle   => "angle",
            PropertyType.Angles  => "angles",
            PropertyType.Color   => "color",
            PropertyType.Model   => "model",
            PropertyType.Texture => "texture",
            PropertyType.Boolean => "boolean",
            PropertyType.Target  => "target",
            PropertyType.Sound   => "sound",
            PropertyType.Array   => "array",
            PropertyType.Flag    => "flag",
            _                   => "string",
        };

        writer.WriteStartElement(tag);
        writer.WriteAttributeString("key", prop.Key);
        if (!string.IsNullOrEmpty(prop.DisplayName))
            writer.WriteAttributeString("name", prop.DisplayName);
        if (!string.IsNullOrEmpty(prop.DefaultValue))
            writer.WriteAttributeString("value", prop.DefaultValue);
        if (prop.Type == PropertyType.Flag)
            writer.WriteAttributeString("bit", prop.BitIndex.ToString());
        if (!string.IsNullOrWhiteSpace(prop.Description))
            writer.WriteString(prop.Description);
        writer.WriteEndElement();
    }
}

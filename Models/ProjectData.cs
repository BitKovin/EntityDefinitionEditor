using System.Collections.Generic;

namespace EntityEditor.Models;

public class ProjectData
{
    public string Version { get; set; } = "1.0";
    public List<EntityData> Entities { get; set; } = new();
}

public class EntityData
{
    public string Name { get; set; } = "new_entity";
    public string EntityType { get; set; } = "point"; // point | group | abstract
    public double[] Color { get; set; } = [0.5, 0.5, 0.5];
    public double[]? Box { get; set; } = [-16, -16, -16, 16, 16, 16];
    public string Description { get; set; } = "";
    public List<string> Inherits { get; set; } = new();
    public List<PropertyData> Properties { get; set; } = new();
}

public class PropertyData
{
    public string Key { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public PropertyType Type { get; set; } = PropertyType.String;
    public string DefaultValue { get; set; } = "";
    public string Description { get; set; } = "";
    public int BitIndex { get; set; } = 0; // for Flag type
    public bool IsBuiltIn { get; set; } = false;

    public override string ToString()
    {
        return DisplayName;
    }

}

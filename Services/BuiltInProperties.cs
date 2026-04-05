using EntityEditor.Models;
using System.Collections.Generic;

namespace EntityEditor.Services;

public static class BuiltInProperties
{
    public static readonly IReadOnlyList<PropertyData> All = new List<PropertyData>
    {
        new() { Key = "targetname", DisplayName = "Name", Type = PropertyType.String,
                DefaultValue = "", Description = "Logical name of this entity. Used by other entities to target it.", IsBuiltIn = true },
        new() { Key = "target", DisplayName = "Target", Type = PropertyType.Target,
                DefaultValue = "", Description = "Targetname of entity to activate/target.", IsBuiltIn = true },
        new() { Key = "angle", DisplayName = "Yaw Angle", Type = PropertyType.Angle,
                DefaultValue = "0", Description = "Yaw rotation in degrees. 0 = East, 90 = South, etc.", IsBuiltIn = true },
        new() { Key = "angles", DisplayName = "Pitch Yaw Roll", Type = PropertyType.Angles,
                DefaultValue = "0 0 0", Description = "Full rotation: Pitch Yaw Roll in degrees.", IsBuiltIn = true },
        new() { Key = "model", DisplayName = "Model Path", Type = PropertyType.Model,
                DefaultValue = "", Description = "Path to the model file (e.g. models/props/crate.obj).", IsBuiltIn = true },
        new() { Key = "origin", DisplayName = "Origin", Type = PropertyType.Real3,
                DefaultValue = "0 0 0", Description = "World-space XYZ position of the entity.", IsBuiltIn = true },
        new() { Key = "_color", DisplayName = "Light Color", Type = PropertyType.Color,
                DefaultValue = "1 1 1", Description = "Weighted RGB color value (default 1 1 1 = white).", IsBuiltIn = true },
        new() { Key = "classname", DisplayName = "Class Name", Type = PropertyType.String,
                DefaultValue = "", Description = "Entity class name (set automatically by the engine).", IsBuiltIn = true },
        new() { Key = "spawnflags", DisplayName = "Spawnflags", Type = PropertyType.Integer,
                DefaultValue = "0", Description = "Bitmask of spawning flags.", IsBuiltIn = true },
    };

    public static readonly IReadOnlyDictionary<string, string> TypeDescriptions = new Dictionary<string, string>
    {
        [nameof(PropertyType.String)]  = "Arbitrary text value.",
        [nameof(PropertyType.Integer)] = "Whole number (e.g. 0, 1, -5).",
        [nameof(PropertyType.Real)]    = "Floating-point number (e.g. 1.5, -0.3).",
        [nameof(PropertyType.Real3)]   = "Three floats separated by spaces (e.g. 0 0 0).",
        [nameof(PropertyType.Angle)]   = "Single yaw angle in degrees.",
        [nameof(PropertyType.Angles)]  = "Pitch Yaw Roll in degrees (e.g. 0 90 0).",
        [nameof(PropertyType.Color)]   = "RGB color, each channel 0.0–1.0 (e.g. 1 0.5 0).",
        [nameof(PropertyType.Model)]   = "Path to a model file.",
        [nameof(PropertyType.Texture)] = "Path to a texture/shader (omit 'textures/' prefix in some cases).",
        [nameof(PropertyType.Boolean)] = "True/false value (0 or 1).",
        [nameof(PropertyType.Target)]  = "Targetname of another entity.",
        [nameof(PropertyType.Sound)]   = "Path to a sound/music file.",
        [nameof(PropertyType.Array)]   = "Array of values (space-separated).",
        [nameof(PropertyType.Flag)]    = "Spawnflag bit (individual bit in the spawnflags bitmask).",
    };
}

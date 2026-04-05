using EntityEditor.Commands;
using EntityEditor.Models;
using EntityEditor.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace EntityEditor.ViewModels;

public class EntityViewModel : ViewModelBase
{
    private readonly UndoRedoService _undo;

    // ── Backing fields ──────────────────────────────────────────────────
    private string _name        = "new_entity";
    private string _entityType  = "point";
    private string _description = "";
    private double _colorR = 0.5, _colorG = 0.5, _colorB = 0.5;
    private double _boxMinX = -16, _boxMinY = -16, _boxMinZ = -16;
    private double _boxMaxX =  16, _boxMaxY =  16, _boxMaxZ =  16;
    private bool _hasBox = true;

    private PropertyViewModel? _selectedProperty;

    public bool SuppressUndo { get; set; }

    public EntityViewModel(UndoRedoService undo)
    {
        _undo = undo;
        OwnProperties = new ObservableCollection<PropertyViewModel>();
        Inherits = new ObservableCollection<string>();
        Inherits.CollectionChanged += (_, _) => RefreshAllProperties();
        OwnProperties.CollectionChanged += (_, _) => RefreshAllProperties();
    }

    // ── Tracked properties ──────────────────────────────────────────────

    public string Name
    {
        get => _name;
        set
        {
            if (_name == value) return;
            if (!SuppressUndo)
            {
                var old = _name; _undo.Track(new PropertyChangeCommand<string>(
                    "Rename entity", v => { _name = v; OnPropertyChanged(nameof(Name)); }, old, value));
            }
            _name = value; OnPropertyChanged();
        }
    }

    public string EntityType
    {
        get => _entityType;
        set
        {
            if (_entityType == value) return;
            if (!SuppressUndo)
            {
                var old = _entityType; _undo.Track(new PropertyChangeCommand<string>(
                    "Change entity type", v => { _entityType = v; OnPropertyChanged(nameof(EntityType)); OnPropertyChanged(nameof(HasBox)); }, old, value));
            }
            _entityType = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasBox));
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            if (_description == value) return;
            if (!SuppressUndo) {
                var old = _description; _undo.Track(new PropertyChangeCommand<string>(
                    "Edit description", v => { _description = v; OnPropertyChanged(nameof(Description)); }, old, value));
            }
            _description = value; OnPropertyChanged();
        }
    }

    // Color channels
    public double ColorR { get => _colorR; set => SetColorChannel(ref _colorR, value, nameof(ColorR)); }
    public double ColorG { get => _colorG; set => SetColorChannel(ref _colorG, value, nameof(ColorG)); }
    public double ColorB { get => _colorB; set => SetColorChannel(ref _colorB, value, nameof(ColorB)); }

    private void SetColorChannel(ref double field, double value, string name)
    {
        value = Math.Clamp(value, 0, 1);
        if (Math.Abs(field - value) < 1e-9) return;
        if (!SuppressUndo)
        {
            var old = field; var n = name;
            _undo.Track(new PropertyChangeCommand<double>($"Change color", v => {
                if (n == nameof(ColorR)) _colorR = v;
                else if (n == nameof(ColorG)) _colorG = v;
                else _colorB = v;
                OnPropertyChanged(n);
            }, old, value));
        }
        field = value; OnPropertyChanged(name);
    }

    // Box
    public bool HasBox  { get => _hasBox && _entityType == "point"; set { SetField(ref _hasBox, value); OnPropertyChanged(); } }
    public double BoxMinX { get => _boxMinX; set => SetBox(ref _boxMinX, value, nameof(BoxMinX)); }
    public double BoxMinY { get => _boxMinY; set => SetBox(ref _boxMinY, value, nameof(BoxMinY)); }
    public double BoxMinZ { get => _boxMinZ; set => SetBox(ref _boxMinZ, value, nameof(BoxMinZ)); }
    public double BoxMaxX { get => _boxMaxX; set => SetBox(ref _boxMaxX, value, nameof(BoxMaxX)); }
    public double BoxMaxY { get => _boxMaxY; set => SetBox(ref _boxMaxY, value, nameof(BoxMaxY)); }
    public double BoxMaxZ { get => _boxMaxZ; set => SetBox(ref _boxMaxZ, value, nameof(BoxMaxZ)); }

    private void SetBox(ref double field, double value, string name)
    {
        if (Math.Abs(field - value) < 1e-9) return;
        if (!SuppressUndo)
        {
            var old = field; var n = name;
            _undo.Track(new PropertyChangeCommand<double>($"Change box", v => {
                switch (n) {
                    case nameof(BoxMinX): _boxMinX = v; break; case nameof(BoxMinY): _boxMinY = v; break;
                    case nameof(BoxMinZ): _boxMinZ = v; break; case nameof(BoxMaxX): _boxMaxX = v; break;
                    case nameof(BoxMaxY): _boxMaxY = v; break; case nameof(BoxMaxZ): _boxMaxZ = v; break;
                }
                OnPropertyChanged(n);
            }, old, value));
        }
        field = value; OnPropertyChanged(name);
    }

    // ── Collections ──────────────────────────────────────────────────────

    public ObservableCollection<PropertyViewModel> OwnProperties { get; }
    public ObservableCollection<string> Inherits { get; }
    public ObservableCollection<PropertyViewModel> AllProperties { get; } = new();

    public PropertyViewModel? SelectedProperty
    {
        get => _selectedProperty;
        set => SetField(ref _selectedProperty, value);
    }

    // Called externally when the entity list changes, so inherited props can be resolved
    public Func<string, EntityViewModel?>? EntityResolver { get; set; }

    public void RefreshAllProperties()
    {
        AllProperties.Clear();

        // Inherited (depth-first, avoiding cycles)
        var visited = new HashSet<string>();
        CollectInherited(this, visited, AllProperties);

        // Own (may override inherited)
        foreach (var p in OwnProperties)
        {
            p.IsInherited = false;
            p.InheritedFrom = "";
            AllProperties.Add(p);
        }
    }

    private void CollectInherited(EntityViewModel entity, HashSet<string> visited,
                                   ObservableCollection<PropertyViewModel> target)
    {
        foreach (var parentName in entity.Inherits)
        {
            if (visited.Contains(parentName)) continue;
            visited.Add(parentName);

            var parent = EntityResolver?.Invoke(parentName);
            if (parent == null) continue;

            CollectInherited(parent, visited, target);

            foreach (var prop in parent.OwnProperties)
            {
                // Don't add if entity already has own property with same key
                bool alreadyOwn = OwnProperties.Any(p => p.Key == prop.Key);
                bool alreadyInherited = target.Any(p => p.Key == prop.Key && p.IsInherited);
                if (!alreadyOwn && !alreadyInherited)
                {
                    var inherited = new PropertyViewModel(_undo) { SuppressUndo = true };
                    CopyPropertyValues(prop, inherited);
                    inherited.IsInherited = true;
                    inherited.InheritedFrom = parentName;
                    inherited.SuppressUndo = false;
                    target.Add(inherited);
                }
            }
        }
    }

    private static void CopyPropertyValues(PropertyViewModel src, PropertyViewModel dst)
    {
        dst._key = src.Key; dst._displayName = src.DisplayName;
        dst._type = src.Type; dst._defaultValue = src.DefaultValue;
        dst._description = src.Description; dst._bitIndex = src.BitIndex;
        dst._isBuiltIn = src.IsBuiltIn;
    }

    // ── Serialization ─────────────────────────────────────────────────

    public EntityData ToData() => new()
    {
        Name        = _name,
        EntityType  = _entityType,
        Description = _description,
        Color       = [_colorR, _colorG, _colorB],
        Box         = _hasBox ? [_boxMinX, _boxMinY, _boxMinZ, _boxMaxX, _boxMaxY, _boxMaxZ] : null,
        Inherits    = new List<string>(Inherits),
        Properties  = OwnProperties.Select(p => p.ToData()).ToList(),
    };

    public static EntityViewModel FromData(EntityData d, UndoRedoService undo)
    {
        var vm = new EntityViewModel(undo) { SuppressUndo = true };
        vm._name        = d.Name;
        vm._entityType  = d.EntityType;
        vm._description = d.Description;

        if (d.Color is { Length: >= 3 })
        { vm._colorR = d.Color[0]; vm._colorG = d.Color[1]; vm._colorB = d.Color[2]; }

        if (d.Box is { Length: 6 })
        {
            vm._hasBox = true;
            vm._boxMinX = d.Box[0]; vm._boxMinY = d.Box[1]; vm._boxMinZ = d.Box[2];
            vm._boxMaxX = d.Box[3]; vm._boxMaxY = d.Box[4]; vm._boxMaxZ = d.Box[5];
        }
        else { vm._hasBox = false; }

        foreach (var s in d.Inherits) vm.Inherits.Add(s);
        foreach (var p in d.Properties) vm.OwnProperties.Add(PropertyViewModel.FromData(p, undo));

        vm.SuppressUndo = false;
        return vm;
    }
}

using EntityEditor.Commands;
using EntityEditor.Models;

namespace EntityEditor.ViewModels;

public class PropertyViewModel : ViewModelBase
{
    private readonly UndoRedoService _undo;

    // ── Raw backing fields ──────────────────────────────────────────────
    internal string _key         = "";
    internal string _displayName = "";
    internal PropertyType _type  = PropertyType.String;
    internal string _defaultValue= "";
    internal string _description = "";
    internal int    _bitIndex    = 0;
    internal bool   _isBuiltIn   = false;
    private bool   _isInherited = false;
    private string _inheritedFrom = "";

    // Internal flag to suppress undo recording during load
    public bool SuppressUndo { get; set; }

    public PropertyViewModel(UndoRedoService undo) => _undo = undo;

    // ── Tracked properties ──────────────────────────────────────────────

    public string Key
    {
        get => _key;
        set
        {
            if (_key == value) return;
            if (!SuppressUndo)
            {
                var old = _key;
                _undo.Track(new PropertyChangeCommand<string>($"Change key", v => { _key = v; OnPropertyChanged(nameof(Key)); }, old, value));
            }
            _key = value;
            OnPropertyChanged();
        }
    }

    public string DisplayName
    {
        get => _displayName;
        set
        {
            if (_displayName == value) return;
            if (!SuppressUndo)
            {
                var old = _displayName;
                _undo.Track(new PropertyChangeCommand<string>($"Rename property", v => { _displayName = v; OnPropertyChanged(nameof(DisplayName)); }, old, value));
            }
            _displayName = value;
            OnPropertyChanged();
        }
    }

    public PropertyType Type
    {
        get => _type;
        set
        {
            if (_type == value) return;
            if (!SuppressUndo)
            {
                var old = _type;
                _undo.Track(new PropertyChangeCommand<PropertyType>($"Change type", v => { _type = v; OnPropertyChanged(nameof(Type)); OnPropertyChanged(nameof(IsFlag)); }, old, value));
            }
            _type = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsFlag));
        }
    }

    public string DefaultValue
    {
        get => _defaultValue;
        set
        {
            if (_defaultValue == value) return;
            if (!SuppressUndo)
            {
                var old = _defaultValue;
                _undo.Track(new PropertyChangeCommand<string>($"Change default value", v => { _defaultValue = v; OnPropertyChanged(nameof(DefaultValue)); }, old, value));
            }
            _defaultValue = value;
            OnPropertyChanged();
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            if (_description == value) return;
            if (!SuppressUndo)
            {
                var old = _description;
                _undo.Track(new PropertyChangeCommand<string>($"Edit description", v => { _description = v; OnPropertyChanged(nameof(Description)); }, old, value));
            }
            _description = value;
            OnPropertyChanged();
        }
    }

    public int BitIndex
    {
        get => _bitIndex;
        set
        {
            if (_bitIndex == value) return;
            if (!SuppressUndo)
            {
                var old = _bitIndex;
                _undo.Track(new PropertyChangeCommand<int>($"Change bit index", v => { _bitIndex = v; OnPropertyChanged(nameof(BitIndex)); }, old, value));
            }
            _bitIndex = value;
            OnPropertyChanged();
        }
    }

    // These don't need undo
    public bool IsBuiltIn   { get => _isBuiltIn;   set => SetField(ref _isBuiltIn,   value); }
    public bool IsInherited { get => _isInherited;  set => SetField(ref _isInherited, value); }
    public string InheritedFrom { get => _inheritedFrom; set => SetField(ref _inheritedFrom, value); }

    public bool IsFlag => Type == PropertyType.Flag;

    // ── Serialization helpers ──────────────────────────────────────────

    public PropertyData ToData() => new()
    {
        Key          = _key,
        DisplayName  = _displayName,
        Type         = _type,
        DefaultValue = _defaultValue,
        Description  = _description,
        BitIndex     = _bitIndex,
        IsBuiltIn    = _isBuiltIn,
    };

    public static PropertyViewModel FromData(PropertyData d, UndoRedoService undo)
    {
        var vm = new PropertyViewModel(undo) { SuppressUndo = true };
        vm._key          = d.Key;
        vm._displayName  = d.DisplayName;
        vm._type         = d.Type;
        vm._defaultValue = d.DefaultValue;
        vm._description  = d.Description;
        vm._bitIndex     = d.BitIndex;
        vm._isBuiltIn    = d.IsBuiltIn;
        vm.SuppressUndo  = false;
        return vm;
    }

    public override string ToString()
    {
        return Key;
    }

}

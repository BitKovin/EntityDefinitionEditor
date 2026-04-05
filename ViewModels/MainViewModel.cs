using EntityEditor.Commands;
using EntityEditor.Models;
using EntityEditor.Services;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace EntityEditor.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly UndoRedoService _undo = new();

    private EntityViewModel? _selectedEntity;
    private string _searchText = "";
    private string? _currentFilePath;
    private bool _isDirty;
    private string _statusMessage = "Ready";

    public MainViewModel()
    {
        _undo.HistoryChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(UndoLabel));
            OnPropertyChanged(nameof(RedoLabel));
            IsDirty = true;
        };

        // Commands
        NewProjectCommand    = new RelayCommand(NewProject);
        OpenProjectCommand   = new RelayCommand(OpenProject);
        SaveProjectCommand   = new RelayCommand(SaveProject);
        SaveAsProjectCommand = new RelayCommand(SaveAsProject);
        ImportEntCommand     = new RelayCommand(ImportEnt);
        ExportEntCommand     = new RelayCommand(ExportEnt);

        UndoCommand = new RelayCommand(_undo.Undo, () => _undo.CanUndo);
        RedoCommand = new RelayCommand(_undo.Redo, () => _undo.CanRedo);

        AddEntityCommand     = new RelayCommand(AddEntity);
        DuplicateEntityCommand = new RelayCommand(DuplicateEntity, () => SelectedEntity != null);
        DeleteEntityCommand  = new RelayCommand(DeleteEntity, () => SelectedEntity != null);
        MoveEntityUpCommand  = new RelayCommand(MoveEntityUp,   () => SelectedEntity != null && Entities.IndexOf(SelectedEntity) > 0);
        MoveEntityDownCommand= new RelayCommand(MoveEntityDown, () => SelectedEntity != null && Entities.IndexOf(SelectedEntity) < Entities.Count - 1);

        AddPropertyCommand      = new RelayCommand(AddProperty,    () => SelectedEntity != null);
        DeletePropertyCommand   = new RelayCommand(DeleteProperty,  () => SelectedEntity?.SelectedProperty != null && SelectedEntity.SelectedProperty.IsInherited == false);
        AddInheritCommand       = new RelayCommand(AddInherit,      () => SelectedEntity != null);
        RemoveInheritCommand    = new RelayCommand<string>(RemoveInherit, s => s != null);
        AddBuiltInCommand       = new RelayCommand<string>(AddBuiltIn,   s => SelectedEntity != null);

        // Load a default empty project
        NewProject();
    }

    // ── Collections ──────────────────────────────────────────────────────

    public ObservableCollection<EntityViewModel> Entities { get; } = new();
    public ObservableCollection<EntityViewModel> FilteredEntities { get; } = new();

    // ── Selection ────────────────────────────────────────────────────────

    public EntityViewModel? SelectedEntity
    {
        get => _selectedEntity;
        set
        {
            SetField(ref _selectedEntity, value);
            if (value != null)
            {
                value.EntityResolver = name => Entities.FirstOrDefault(e => e.Name == name);
                value.RefreshAllProperties();
            }
            RelayCommand.Refresh();
        }
    }

    // ── Filter ───────────────────────────────────────────────────────────

    public string SearchText
    {
        get => _searchText;
        set { SetField(ref _searchText, value); ApplyFilter(); }
    }

    private void ApplyFilter()
    {
        FilteredEntities.Clear();
        var q = _searchText.Trim().ToLower();
        foreach (var e in Entities)
            if (string.IsNullOrEmpty(q) || e.Name.ToLower().Contains(q))
                FilteredEntities.Add(e);
    }

    // ── State ────────────────────────────────────────────────────────────

    public bool IsDirty   { get => _isDirty;  set { SetField(ref _isDirty,  value); OnPropertyChanged(nameof(WindowTitle)); } }
    public string StatusMessage { get => _statusMessage; set => SetField(ref _statusMessage, value); }
    public string WindowTitle => $"Entity Editor{(_currentFilePath != null ? $" — {Path.GetFileName(_currentFilePath)}" : "")}{(IsDirty ? " *" : "")}";

    public string UndoLabel => _undo.CanUndo ? $"Undo: {_undo.UndoDescription}" : "Undo";
    public string RedoLabel => _undo.CanRedo ? $"Redo: {_undo.RedoDescription}" : "Redo";

    public ObservableCollection<string> EntityTypeOptions { get; } = new(["point", "group", "abstract"]);
    public ObservableCollection<Models.PropertyType> PropertyTypes { get; } = new(
        Enum.GetValues<Models.PropertyType>());
    public ObservableCollection<PropertyViewModel> BuiltInProperties { get; } = new(
        Services.BuiltInProperties.All.Select(p => PropertyViewModel.FromData(p, new UndoRedoService())));

    // ── Commands ─────────────────────────────────────────────────────────

    public ICommand NewProjectCommand     { get; }
    public ICommand OpenProjectCommand    { get; }
    public ICommand SaveProjectCommand    { get; }
    public ICommand SaveAsProjectCommand  { get; }
    public ICommand ImportEntCommand      { get; }
    public ICommand ExportEntCommand      { get; }
    public ICommand UndoCommand           { get; }
    public ICommand RedoCommand           { get; }
    public ICommand AddEntityCommand      { get; }
    public ICommand DuplicateEntityCommand{ get; }
    public ICommand DeleteEntityCommand   { get; }
    public ICommand MoveEntityUpCommand   { get; }
    public ICommand MoveEntityDownCommand { get; }
    public ICommand AddPropertyCommand    { get; }
    public ICommand DeletePropertyCommand { get; }
    public ICommand AddInheritCommand     { get; }
    public ICommand RemoveInheritCommand  { get; }
    public ICommand AddBuiltInCommand     { get; }

    // ── Project operations ────────────────────────────────────────────────

    private void NewProject()
    {
        if (!ConfirmDiscard()) return;
        Entities.Clear();
        FilteredEntities.Clear();
        SelectedEntity = null;
        _currentFilePath = null;
        _undo.Clear();
        IsDirty = false;
        StatusMessage = "New project created.";
        OnPropertyChanged(nameof(WindowTitle));
    }

    private void OpenProject()
    {
        if (!ConfirmDiscard()) return;
        var dlg = new OpenFileDialog { Filter = "Entity Project (*.entproj)|*.entproj|All Files (*.*)|*.*" };
        if (dlg.ShowDialog() != true) return;
        try
        {
            var data = JsonProjectService.Load(dlg.FileName);
            LoadProject(data);
            _currentFilePath = dlg.FileName;
            _undo.Clear();
            IsDirty = false;
            StatusMessage = $"Opened: {dlg.FileName}";
            OnPropertyChanged(nameof(WindowTitle));
        }
        catch (Exception ex) { MessageBox.Show($"Failed to open file:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void SaveProject()
    {
        if (_currentFilePath == null) { SaveAsProject(); return; }
        SaveToPath(_currentFilePath);
    }

    private void SaveAsProject()
    {
        var dlg = new SaveFileDialog { Filter = "Entity Project (*.entproj)|*.entproj" };
        if (dlg.ShowDialog() != true) return;
        SaveToPath(dlg.FileName);
        _currentFilePath = dlg.FileName;
        OnPropertyChanged(nameof(WindowTitle));
    }

    private void SaveToPath(string path)
    {
        try
        {
            var data = new ProjectData { Entities = Entities.Select(e => e.ToData()).ToList() };
            JsonProjectService.Save(path, data);
            IsDirty = false;
            StatusMessage = $"Saved: {path}";
        }
        catch (Exception ex) { MessageBox.Show($"Failed to save:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void ImportEnt()
    {
        if (!ConfirmDiscard()) return;
        var dlg = new OpenFileDialog { Filter = "Entity Definition (*.ent)|*.ent|All Files (*.*)|*.*" };
        if (dlg.ShowDialog() != true) return;
        try
        {
            var data = EntParser.ImportFromEnt(dlg.FileName);
            LoadProject(data);
            _currentFilePath = null;
            _undo.Clear();
            IsDirty = true;
            StatusMessage = $"Imported: {dlg.FileName}";
            OnPropertyChanged(nameof(WindowTitle));
        }
        catch (Exception ex) { MessageBox.Show($"Failed to import:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void ExportEnt()
    {
        var dlg = new SaveFileDialog { Filter = "Entity Definition (*.ent)|*.ent" };
        if (dlg.ShowDialog() != true) return;
        try
        {
            var data = new ProjectData { Entities = Entities.Select(e => e.ToData()).ToList() };
            EntParser.ExportToEnt(dlg.FileName, data);
            StatusMessage = $"Exported to: {dlg.FileName}";
        }
        catch (Exception ex) { MessageBox.Show($"Failed to export:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void LoadProject(ProjectData data)
    {
        Entities.Clear();
        FilteredEntities.Clear();
        SelectedEntity = null;
        foreach (var ed in data.Entities)
        {
            var vm = EntityViewModel.FromData(ed, _undo);
            Entities.Add(vm);
        }
        ApplyFilter();
        RefreshAllEntityInheritance();
    }

    private void RefreshAllEntityInheritance()
    {
        foreach (var e in Entities)
        {
            e.EntityResolver = name => Entities.FirstOrDefault(x => x.Name == name);
            e.RefreshAllProperties();
        }
    }

    // ── Entity CRUD ───────────────────────────────────────────────────────

    private void AddEntity()
    {
        var vm = new EntityViewModel(_undo) { SuppressUndo = false };
        vm.Name = GenerateUniqueName("new_entity");
        _undo.Record(new AddItemCommand<EntityViewModel>("Add entity", Entities, vm));
        RefreshAllEntityInheritance();
        ApplyFilter();
        SelectedEntity = vm;
        IsDirty = true;
        StatusMessage = $"Added entity: {vm.Name}";
    }

    private void DuplicateEntity()
    {
        if (SelectedEntity == null) return;
        var src = SelectedEntity.ToData();
        src.Name = GenerateUniqueName(src.Name + "_copy");
        var vm = EntityViewModel.FromData(src, _undo);
        var idx = Entities.IndexOf(SelectedEntity) + 1;
        _undo.Record(new AddItemCommand<EntityViewModel>("Duplicate entity", Entities, vm, idx));
        RefreshAllEntityInheritance();
        ApplyFilter();
        SelectedEntity = vm;
        IsDirty = true;
    }

    private void DeleteEntity()
    {
        if (SelectedEntity == null) return;
        var name = SelectedEntity.Name;
        _undo.Record(new RemoveItemCommand<EntityViewModel>("Delete entity", Entities, SelectedEntity));
        ApplyFilter();
        SelectedEntity = Entities.FirstOrDefault();
        RefreshAllEntityInheritance();
        IsDirty = true;
        StatusMessage = $"Deleted entity: {name}";
    }

    private void MoveEntityUp()
    {
        if (SelectedEntity == null) return;
        int idx = Entities.IndexOf(SelectedEntity);
        if (idx <= 0) return;
        Entities.Move(idx, idx - 1);
        ApplyFilter();
        IsDirty = true;
    }

    private void MoveEntityDown()
    {
        if (SelectedEntity == null) return;
        int idx = Entities.IndexOf(SelectedEntity);
        if (idx >= Entities.Count - 1) return;
        Entities.Move(idx, idx + 1);
        ApplyFilter();
        IsDirty = true;
    }

    // ── Property CRUD ─────────────────────────────────────────────────────

    private void AddProperty()
    {
        if (SelectedEntity == null) return;
        var p = new PropertyViewModel(_undo)
        {
            SuppressUndo = false,
        };
        // Temporarily suppress so the add itself is the recorded action
        p.SuppressUndo = true;
        p._key = "new_key"; p._displayName = "New Property";
        p.SuppressUndo = false;
        _undo.Record(new AddItemCommand<PropertyViewModel>("Add property", SelectedEntity.OwnProperties, p));
        SelectedEntity.SelectedProperty = p;
        IsDirty = true;
    }

    private void DeleteProperty()
    {
        if (SelectedEntity?.SelectedProperty == null || SelectedEntity.SelectedProperty.IsInherited) return;
        var p = SelectedEntity.SelectedProperty;
        _undo.Record(new RemoveItemCommand<PropertyViewModel>("Delete property", SelectedEntity.OwnProperties, p));
        SelectedEntity.SelectedProperty = SelectedEntity.OwnProperties.FirstOrDefault();
        IsDirty = true;
    }

    private void AddInherit()
    {
        if (SelectedEntity == null) return;
        var available = Entities.Where(e => e != SelectedEntity && !SelectedEntity.Inherits.Contains(e.Name))
                                .Select(e => e.Name).ToList();
        if (!available.Any()) { MessageBox.Show("No other entities available to inherit from.", "Add Inheritance"); return; }
        var picker = new InheritPickerWindow(available) { Owner = Application.Current.MainWindow };
        if (picker.ShowDialog() == true && !string.IsNullOrEmpty(picker.SelectedName))
        {
            _undo.Record(new AddItemCommand<string>("Add inheritance", SelectedEntity.Inherits, picker.SelectedName));
            RefreshAllEntityInheritance();
            IsDirty = true;
        }
    }

    private void RemoveInherit(string? name)
    {
        if (SelectedEntity == null || name == null) return;
        _undo.Record(new RemoveItemCommand<string>("Remove inheritance", SelectedEntity.Inherits, name));
        RefreshAllEntityInheritance();
        IsDirty = true;
    }

    private void AddBuiltIn(string? key)
    {
        if (SelectedEntity == null || key == null) return;
        var builtIn = Services.BuiltInProperties.All.FirstOrDefault(p => p.Key == key);
        if (builtIn == null) return;
        if (SelectedEntity.OwnProperties.Any(p => p.Key == key))
        { MessageBox.Show($"Property '{key}' already exists.", "Duplicate Property"); return; }
        var vm = PropertyViewModel.FromData(builtIn, _undo);
        _undo.Record(new AddItemCommand<PropertyViewModel>("Add built-in property", SelectedEntity.OwnProperties, vm));
        SelectedEntity.SelectedProperty = vm;
        IsDirty = true;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private string GenerateUniqueName(string base_)
    {
        var name = base_;
        int i = 1;
        while (Entities.Any(e => e.Name == name)) name = $"{base_}_{i++}";
        return name;
    }

    private bool ConfirmDiscard()
    {
        if (!IsDirty) return true;
        var r = MessageBox.Show("You have unsaved changes. Discard them?", "Unsaved Changes",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        return r == MessageBoxResult.Yes;
    }

}

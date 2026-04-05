using EntityEditor.Services;
using EntityEditor.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EntityEditor;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainViewModel();
        DataContext = _vm;

        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.SelectedEntity))
                SubscribeToSelectedEntity();
        };
    }

    private void SubscribeToSelectedEntity()
    {
        if (_vm.SelectedEntity == null) return;
        _vm.SelectedEntity.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(EntityViewModel.SelectedProperty))
                UpdateTypeHint();
        };
    }

    private void UpdateTypeHint()
    {
        var prop = _vm.SelectedEntity?.SelectedProperty;
        if (TypeHintBlock == null) return;
        if (prop == null) { TypeHintBlock.Text = ""; return; }
        var key = prop.Type.ToString();
        TypeHintBlock.Text = BuiltInProperties.TypeDescriptions.TryGetValue(key, out var desc)
            ? $"Format: {desc}" : "";
    }

    private void PropertiesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_vm.SelectedEntity == null) return;
        if (PropertiesGrid.SelectedItem is PropertyViewModel pvm)
        {
            _vm.SelectedEntity.SelectedProperty = pvm;
            UpdateTypeHint();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Delete && EntityList.IsFocused)
            _vm.DeleteEntityCommand.Execute(null);
    }
}

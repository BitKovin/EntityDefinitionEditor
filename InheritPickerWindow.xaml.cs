using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace EntityEditor;

public partial class InheritPickerWindow : Window
{
    public string? SelectedName { get; private set; }

    public InheritPickerWindow(IEnumerable<string> names)
    {
        InitializeComponent();
        listBox.ItemsSource = names;
    }

    private void OkClick(object sender, RoutedEventArgs e)
    {
        SelectedName = listBox.SelectedItem as string;
        if (SelectedName == null) return;
        DialogResult = true;
    }

    private void CancelClick(object sender, RoutedEventArgs e) => DialogResult = false;

    private void ListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        SelectedName = listBox.SelectedItem as string;
        if (SelectedName != null) DialogResult = true;
    }
}

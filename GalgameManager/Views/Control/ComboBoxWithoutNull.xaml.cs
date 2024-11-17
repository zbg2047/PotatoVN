using CommunityToolkit.Mvvm.ComponentModel;
using DependencyPropertyGenerator;
using Microsoft.UI.Xaml;

namespace GalgameManager.Views.Control;

[DependencyProperty<DataTemplate>("ItemTemplate")]
[DependencyProperty<object>("ItemsSource")]
[DependencyProperty<object>("SelectedItem")]
[INotifyPropertyChanged]
public partial class ComboBoxWithoutNull
{
    [ObservableProperty] private object? _selectedItemTmp;

    public ComboBoxWithoutNull()
    {
        InitializeComponent();
    }
    
    partial void OnSelectedItemChanged(object? newValue)
    {
        if (newValue != SelectedItemTmp)
            SelectedItemTmp = newValue;
    }

    partial void OnSelectedItemTmpChanged(object? value)
    {
        if (value is not null && value != SelectedItem)
            SelectedItem = value;
    }
}
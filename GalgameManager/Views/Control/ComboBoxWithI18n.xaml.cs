using CommunityToolkit.Mvvm.ComponentModel;
using DependencyPropertyGenerator;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace GalgameManager.Views.Control;

[DependencyProperty<DataTemplate>("ItemTemplate")]
[DependencyProperty<object>("ItemsSource")]
[DependencyProperty<object>("SelectedItem")]
[INotifyPropertyChanged]
public partial class ComboBoxWithI18N
{
    [ObservableProperty] private object? _selectedItemTmp;
    public ComboBoxWithI18N()
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

public class ComboBoxWithI18NDataTemplateSelector : DataTemplateSelector
{
    public DataTemplate DefaultTemplate { get; set; } = null!;
    public DataTemplate? ItemTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return ItemTemplate ?? DefaultTemplate;
    }
}
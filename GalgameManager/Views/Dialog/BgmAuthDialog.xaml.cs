﻿using CommunityToolkit.Mvvm.ComponentModel;
using GalgameManager.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace GalgameManager.Views.Dialog;

public sealed partial class BgmAuthDialog : ContentDialog
{
    public string AccessToken
    {
        get => (string)GetValue(AccessTokenProperty);
        set => SetValue(AccessTokenProperty, value);
    }
    
    public static readonly DependencyProperty AccessTokenProperty = DependencyProperty.Register(
        nameof(AccessToken),
        typeof(string),
        typeof(BgmAuthDialog),
        new PropertyMetadata("")
    );
    
    public int SelectItem
    {
        get => (int)GetValue(SelectItemProperty);
        set => SetValue(SelectItemProperty, value);
    }
    
    public static readonly DependencyProperty SelectItemProperty = DependencyProperty.Register(
        nameof(SelectItem),
        typeof(int),
        typeof(BgmAuthDialog),
        new PropertyMetadata(0)
    );
    public BgmAuthDialog()
    {
        InitializeComponent();
        XamlRoot = App.MainWindow!.Content.XamlRoot;
        DefaultButton = ContentDialogButton.Primary;
        Title = "BgmAuthDialog_Title".GetLocalized();
        PrimaryButtonText = "Login".GetLocalized();
        CloseButtonText = "Cancel".GetLocalized();
    }

    public Visibility SelectItemToVisibility(int selectItem)
    {
        if (selectItem == 1) return Visibility.Visible;
        return Visibility.Collapsed;
    }

    public bool SelectItemToPrimaryButtonEnabled(int selectItem, string accessToken)
    {
        if (selectItem == 0) return true;
        if (selectItem == 1 && !string.IsNullOrEmpty(accessToken)) return true;
        return false;
    }
}

using Avalonia.Controls;
using Avalonia.Interactivity;
using PageFlip.ViewModels;
using System;
using System.Diagnostics;

namespace PageFlip.Views;

public partial class MainWindow : Window
{

    MainWindowViewModel MainWindowView => DataContext as MainWindowViewModel;
    public MainWindow()
    {
        InitializeComponent();
        SizeChanged += OnSizeChanged;
        Loaded += OnWindowLoaded;
    }

    private void OnWindowLoaded(object? sender, RoutedEventArgs e)
    {
        if (MainWindowView is null || sender is not Control c) return;
        if (MainWindowView != null)
        {
            MainWindowView.Loaded(e, (Control)sender, this.VisualChildren?[0],this.VisualRoot.RenderScaling);
        }
    } 
    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    { 
      
    }
}

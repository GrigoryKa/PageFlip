using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace PageFlip.ViewModels;

public abstract class ViewModelBase: ReactiveObject, IDisposable  
{
    protected bool _disposed;
    public void Dispose()
    {
        if (_disposed) return;
        Dispose(true);
        _disposed = true;
        GC.SuppressFinalize(this);
    }
    public abstract string FolderPath { get; set; }
    // Требуется реализовать в потомках — освобождение управляемых/неуправляемых ресурсов
    // на всякий на будущее
    protected abstract void Dispose(bool disposing);
}
public class MainViewModel : ViewModelBase
{
    public MainViewModel(string dirPath)
    {
        FolderPath = dirPath;
    }
    public MainViewModel() { }
 
    public override string FolderPath {
        get => _FolderPath; 
        set => this.RaiseAndSetIfChanged(ref _FolderPath, value);
    }

    protected override void Dispose(bool disposing)
    { 
       
    }

    private string _FolderPath ="";
}
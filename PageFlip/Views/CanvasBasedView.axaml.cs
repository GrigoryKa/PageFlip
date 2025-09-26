using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PageFlip.ViewModels;
using System.Reflection.Metadata;

namespace PageFlip.Views
{
    public partial class CanvasBasedView:  UserControl, IDisposable
    {
        public CanvasBasedView()
        {
            InitializeComponent();
        }

        //напрямую KeyBinding - неясно  как сделать для событий мыши в Avalonia, - поэтому так
        //из документации "MouseBindings are not supported. Instead, handle it in the view's code-behind. (DoubleTapped event)"
        //https://docs.avaloniaui.net/docs/concepts/input/binding-key-and-mouse
        CanvasBasedViewModel ViewModel => DataContext as CanvasBasedViewModel;
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var vroot = this.VisualRoot;
            if (ViewModel is null || sender is not Control c) return;
            ViewModel.OnLoaded(e, c, vroot?.RenderScaling ?? 1.0);
        }
        private void OnResized(object? sender, SizeChangedEventArgs e)
        {
            if (ViewModel is null || sender is not Control c) return;
            ViewModel.OnResized(e, c);
        }

        private void Canvas_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (ViewModel is null || sender is not Control c) return;
            ViewModel.OnPointerPressed(e, c);
        }

        private void Canvas_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (ViewModel is null || sender is not Control c) return;
            ViewModel.OnPointerMoved(e, c);
        }

        private void Canvas_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (ViewModel is null || sender is not Control c) return;
            ViewModel.OnPointerReleased(c, e);
        }

        public void Dispose()
        {
             
        }
    }
}

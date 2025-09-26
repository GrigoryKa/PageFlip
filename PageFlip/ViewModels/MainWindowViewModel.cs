using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace PageFlip.ViewModels
{

    public class MainWindowViewModel : ViewModelBase
    {
        private ViewModelBase?[] _viewModelBases = new ViewModelBase?[] { new MainViewModel(), new CanvasBasedViewModel() };
        private string?[] _SelectedApproachs = new string?[] { "На основе контролла\nPageFlipControl", "На основе view\nCanvasBasedViewModel c Canvas" };

        private int _selModel = 0;
        private int _max = 1;
        private int _WinHeight = 680;
        private int _WinWidth = 720;

        
        private double _scaling = 1;

        public string? PageFlipTitle
        {
            get => $"Просмотр изображений с перелистованием и загибом угла , {SelectedApproach}, ({FolderPath})";
        }
        public int WinWidth
        {
            get => _WinWidth;
            private set
            {
                this.RaiseAndSetIfChanged(ref _WinWidth, value);
                this.RaisePropertyChanged(nameof(MinWinWidth));
                this.RaisePropertyChanged(nameof(MaxWinWidth));
            }
        }        
        public int WinHeight
        {
            get => _WinHeight;
            private set
            {
                this.RaiseAndSetIfChanged(ref _WinHeight, value);
                this.RaisePropertyChanged(nameof(MinWinHeight));
                this.RaisePropertyChanged(nameof(MaxWinHeight));
            }
        }
        public int MinWinHeight
        {
            get => _WinHeight-15;          
        }
        public int MinWinWidth
        {
            get => _WinWidth-20;            
        }
        public int MaxWinHeight
        {
            get => _WinHeight + 15;
        }
        public int MaxWinWidth
        {
            get => _WinWidth + 20;
        }



        public override string FolderPath
        {
            get => _imageDir;
            set
            {
                this.RaiseAndSetIfChanged(ref _imageDir, value);
                _viewModelBases[SelectedIndex].FolderPath = value;
                this.RaisePropertyChanged(nameof(PageFlipTitle));                
            }
        }

        public int SelectedIndex
        {
            get => _selModel;
            private set 
            {
                this.RaiseAndSetIfChanged(ref _selModel, value);
                this.RaisePropertyChanged(nameof(SelectedApproach));
                this.RaisePropertyChanged(nameof(PageFlipTitle));
            }
        }
        public ViewModelBase? CurrentViewModel
        {
            get => _viewModelBases[_selModel];
            private set => this.RaiseAndSetIfChanged(ref _current, value);
        }
        public string? SelectedApproach
        {
            get => _SelectedApproachs[_selModel];         
        }

        private ViewModelBase? _current;
        private Visual  _rootVisual = null;
        private string _FolderPath;
        private PixelSize? _screenSize = null;
        private string _imageDir = "";
        //private string _SelectedApproach = "";
        public ReactiveCommand<Unit, Unit> BackCommand { get; }
        public ReactiveCommand<Unit, Unit> ForwardCommand { get; }
        public ReactiveCommand<Unit, string?> PickFolderCommand { get; }
        public MainWindowViewModel()
        {

            // FolderPath = ".\\assets\\images";
            FolderPath = "avares://PageFlip/Assets/images/";
            BackCommand = ReactiveCommand.Create(
                    () =>
                    {
                        SelectedIndex--;
                        _viewModelBases[SelectedIndex].FolderPath = FolderPath;
                        CurrentViewModel = _viewModelBases[SelectedIndex];
                    },
                    this.WhenAnyValue(x => x.SelectedIndex, i => i > 0));

            ForwardCommand = ReactiveCommand.Create(
                () =>
                {
                    try
                    {
                        SelectedIndex++;
                        _viewModelBases[SelectedIndex].FolderPath = FolderPath;
                        CurrentViewModel = _viewModelBases[SelectedIndex];
                    }
                    catch (Exception ex)
                    {

                    }
                },
                this.WhenAnyValue(x => x.SelectedIndex, i => i < _max)
            );
            PickFolderCommand = ReactiveCommand.CreateFromTask(async () => {
                string? path = null;
                if (_rootVisual != null)
                {
                    var top = TopLevel.GetTopLevel(_rootVisual);
                    var folder = await top.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Выберите папку" });
                    if (folder?.Count > 0)
                    {  
                        var sz = ImageStore.GetFirstImageSize(folder[0]?.TryGetLocalPath()??"");
                        if(sz != null && _screenSize!=null)
                        {
                         
                            double prop = .588*2; // 
                            double tmp = ((sz?.Width * 2 ?? 0) -322.912) / (sz?.Height ?? 0.0001);
                            prop = tmp;
                            double h = Math.Min(sz?.Height ?? .0001, _screenSize?.Height ?? .0001) - 280 * _scaling;  //350;
                            double w = h * prop;
                            WinWidth = (int)(w);
                            WinHeight = (int)(h);
                            path = folder[0].Path.AbsolutePath;
                        }
                    }
                }
                return path; 
            });
            PickFolderCommand.Subscribe((p) => FolderPath = p);
        }

       
        public void Loaded(RoutedEventArgs e, Control sender,Visual VisualRoot, double scaling)
        {
            _rootVisual = VisualRoot;
            _scaling = scaling;
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                _screenSize = desktop.MainWindow.Screens.Primary.Bounds.Size;
            }
        }

        protected override void Dispose(bool disposing)
        {
            foreach (var m in _viewModelBases)
            {
                if (m != null)
                    m.Dispose();
            }
        }

    }
   
}

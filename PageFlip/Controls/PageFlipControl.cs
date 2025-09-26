namespace PageFlip.Controls;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Converters;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PageFlip.Corner;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reflection;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Color = Avalonia.Media.Color;

public class PageFlipControl : Control, IDisposable
{
    #region свойства контрола
    public static readonly StyledProperty<string> FolderPathProperty =
        AvaloniaProperty.Register<PageFlipControl, string>(nameof(FolderPath), defaultValue: ".\\assets\\images");

    public string FolderPath
    {
        get => GetValue(FolderPathProperty);
        set
        {
            SetValue(FolderPathProperty, value);
        }
    }

    #endregion

    public PageFlipControl()
    {
        Loaded += OnLoaded;

        this.GetObservable(FolderPathProperty)
            .Subscribe(path =>
            {
                if (path != null)
                {
                    _images?.Dispose();
                    _images = new ImageStore(path);
                    Reload();
                }
            })
             .DisposeWith(_disposables);
    }

    public override void Render(DrawingContext dc)
    {
        base.Render(dc);

        if (_images == null || _images.Length == 0 || _selImages == null || _corner == null || _corner.Freeze)
            return;
        //  RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);
        using (dc.PushRenderOptions(new RenderOptions
        {
            BitmapInterpolationMode = BitmapInterpolationMode.HighQuality

        }))
            _corner.PagesRender(dc, _pImgIndex == 0, _pImgIndex >= _images.Length && _images.Length % 2 == 0);
    }


    #region приватные 

    private readonly CompositeDisposable _disposables = new();

    private Bitmap ResizeCoverCropTopRight(Bitmap source, PixelSize targetSize)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (targetSize.Width <= 0 || targetSize.Height <= 0)
            throw new ArgumentException("Target size must be positive.");

        double srcW = source.PixelSize.Width;
        double srcH = source.PixelSize.Height;
        //  double srcAR = srcW / srcH;
        double tgtW = targetSize.Width;
        double tgtH = targetSize.Height;
        //  double tgtAR = tgtW / tgtH;
        //double sclX = tgtW / srcW;
        //double sclY = tgtH / srcH;
        double correctscl = 1; // _RenderScaling ;
        var render = new RenderTargetBitmap(new PixelSize((int)(tgtW * correctscl), (int)(tgtH * correctscl)), source.Dpi);
        //https://github.com/AvaloniaUI/Avalonia/pull/12734
        using (var ctx = render.CreateDrawingContext(true))
        {
            using (ctx.PushRenderOptions(new RenderOptions
            {
                BitmapInterpolationMode = BitmapInterpolationMode.HighQuality,
                BitmapBlendingMode = BitmapBlendingMode.SourceOut,
                EdgeMode = EdgeMode.Aliased
            }))
            {
                Rect srcRect;
                // double srcX = srcW - desiredW;                   // смещаем к правому краю
                srcRect = new Rect(0, 0, srcW, srcH);
                var dstRect = new Rect(0, 0, tgtW * correctscl, tgtH * correctscl);           // целевая область полностью
                ctx.DrawImage(source, srcRect, dstRect);
            }
        }

        return render;
    }


    private void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        var p = e.GetPosition(this);
        e.Pointer.Capture(this);
        if (!_corner.MoveBack && p.X > _w - 100 && p.Y > _h - 100 && _pImgIndex < _images.Length - 1)
        {
            // _isDragging = true;
            //new Point(_w, _h), _w, _h, _offsetX,_offsetY, _pImgIndex, _selImages)
            //_corner.Update(p, _w, _h, _selImages[(int)_indSelectorEnum._nextImgIndex]?.PixelSize, _selImages[(int)_indSelectorEnum._nextNextImgIndex]?.PixelSize);
            _corner.Update(p, _w, _h, _selImages, true);

            InvalidateVisual();
            return;
        }

        if (!_corner.MoveBack && p.X < 100 && p.Y > _h - 100 && _pImgIndex > 1)
        {
            //_isDragging = true;
            // _moveBack = true;
            _pImgIndex -= 2;
            //сбросим все
            ResetSelectedBuffer();
            ResizePrevCurrent();
            // _corner.Update(p, _w, _h, _selImages[(int)_indSelectorEnum._nextImgIndex]?.PixelSize, _selImages[(int)_indSelectorEnum._nextNextImgIndex]?.PixelSize);
            // _corner.Update(p, _w, _h,  _selImages, false);
            e.Pointer.Capture(this);
            InvalidateVisual();
            _corner.Update(p, _w, _h, _selImages, true, true);
            InvalidateVisual();
            return;
        }

    }

    private void OnPointerMoved(object sender, PointerEventArgs e)
    {
        if (!_corner.IsDragging)
        {
            return;
        }
        _corner.Update(e.GetPosition(this), _w, _h, _selImages, true);
        InvalidateVisual();
    }

    private void OnPointerReleased(object sender, PointerReleasedEventArgs e)
    {
        if (!_corner.IsDragging) return;

        e.Pointer.Capture(null);

        if ((!_corner.MoveBack && (_corner.Pointer.X) < Bounds.Width * 0.75 && _images.Length > _pImgIndex))
        {
            //    open book
            // ---------------
            // page1  |  page2
            // _prev  |  _current | next | nextNext
            // =============== 
            //_next -- обратная сторона текущей страницы, которая при перевертывании переходит на page1 в _prev
            //pages: 0,1,2,3,4,5,6,7
            ///  flip - seq
            ///  page1   page2
            ///  null     0
            //    1       2
            //    3       4
            //    5       6
            //    7       null

            int i = 2;
            for (; i <= 3 && i < _images.Length && _selImages[i] != null; i++)
            {
                _selImages[i - 2] = _selImages[i];
                if (_selImages[i] != null)
                {
                    _selImages[i] = null;
                }
            }
            _pImgIndex += 2;

        }

        _corner.Update(new Point(_w, _h), _w, _h, _selImages, false);
        ResizePrevCurrent();
        InvalidateVisual();
    }

    //предварительно масштабируем и обрезаем
    private void ResizePrevCurrent()
    {
        if (_images == null) return;

        //конец нечетное к-во листов , оба листа показать prev, next
        if (_pImgIndex >= _images.Length && _images.Length % 2 != 0)
        {
            _selImages[0] = ResizeCoverCropTopRight(_images[_images.Length - 3], _imgSize);
            _selImages[1] = ResizeCoverCropTopRight(_images[_images.Length - 2], _imgSize);
            return;
        }

        for (int i = 0; i < 4 && _images.Length > (_pImgIndex + i - 1); i++)
        {
            if (_selImages[i] == null && _pImgIndex + i > 0)
                _selImages[i] = ResizeCoverCropTopRight(_images[_pImgIndex + i - 1], _imgSize);
        }

        //конец четное к-во листов , стереть последнюю при обновлении
        if (_pImgIndex == _images.Length && _images.Length % 2 == 0)
        {
            _selImages[1] = null;
        }
    }
    private void SizedCalcs()
    {
        _w = Bounds.Width;
        _h = Bounds.Height;
        _offsetX = 15;
        _offsetY = 15;
        _w -= _offsetX * 2;
        _h -= _offsetY * 2;
        _imgSize = new PixelSize((int)((_w / 2)), (int)_h);
        NewCorner();
    }

    private void ResetSelectedBuffer()
    {
        if (_selImages == null) return;
        for (int i = 0; i < 4; i++)
        {
            if (_selImages[i] != null)
            {
                _selImages[i].Dispose();
                _selImages[i] = null;
            }
        }
    }

    private void OnResized(object? sender, SizeChangedEventArgs e)
    {
        SizedCalcs();
        //сбросим все при измении размеров элемента
        ResetSelectedBuffer();
        //и перемасштабируем используемые картинки
        ResizePrevCurrent();
        InvalidateVisual();
    }

    private void Reload()
    {
        _pImgIndex = 0;
        if (_images == null)
            _images = new ImageStore(FolderPath);
        if (0 == Bounds.Width && 0 == Bounds.Height)
            return;
        ResetSelectedBuffer();
        SizedCalcs();
        ResizePrevCurrent();
        NewCorner();
        InvalidateVisual();
    }
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SubscribeEvents();
        Reload();
    }
    private void OnUnload(object sender, RoutedEventArgs e)
    {
        PointerPressed -= OnPointerPressed;
        PointerMoved -= OnPointerMoved;
        PointerReleased -= OnPointerReleased;
        Unloaded -= OnUnload;
        SizeChanged -= OnResized;
        _pImgIndex = 0;
        // _images.Dispose();
        //_selImages = null;
    }
    private void NewCorner() => _corner = new CornerModel(new Point(_w, _h), _w, _h, _offsetX, _offsetY, _selImages);

    private void SubscribeEvents()
    {
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        Unloaded += OnUnload;
        SizeChanged += OnResized;
    }

    public void Dispose()
    {
        PointerPressed -= OnPointerPressed;
        PointerMoved -= OnPointerMoved;
        PointerReleased -= OnPointerReleased;
        Loaded -= OnLoaded;
        Unloaded -= OnUnload;
        _selImages = null;
        _images = null;
        _images.Dispose();
        _disposables.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private bool _disposed = false;
    private int _pImgIndex;

    private Bitmap[] _selImages = new Bitmap[4] { null, null, null, null };
    private ImageStore _images;

    private CornerModel _corner;

    private double _w, _h, _offsetX, _offsetY;
    private PixelSize _imgSize = new PixelSize();
    // private bool _isDragging, _moveBack = false;
    // private string _imageDir ;
    #endregion
}


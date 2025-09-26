using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using PageFlip.Corner;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;

namespace PageFlip.ViewModels
{
    public class CanvasBasedViewModel : ViewModelBase
    {
        public CanvasBasedViewModel()
        {

        }

        #region Bindable свойства

        public override string FolderPath
        {
            get => _imageDir;
            set
            {
                _images?.Dispose();
                _images = new ImageStore(value);
                this.RaiseAndSetIfChanged(ref _imageDir, value);
                _pImgIndex = 0;

                SizedCalcs();
                if (_w <= 0 || _h <= 0)
                    return;
                //сбросим все при измении размеров элемента
                ResetSelectedBuffer();
                //и перемасштабируем используемые картинки
                ResizePrevCurrent();
                Update();
            }
        }

        #region публичные

        #region test
        // Bindable свойства
        public int Left
        {
            get => _polyX;
            set => this.RaiseAndSetIfChanged(ref _polyX, value);
        }

        public int Top
        {
            get => _polyY;
            set => this.RaiseAndSetIfChanged(ref _polyY, value);
        }

        public int Width
        {
            get => _Width;
            set => this.RaiseAndSetIfChanged(ref _Width, value);
        }

        public int Height
        {
            get => _Height;
            set => this.RaiseAndSetIfChanged(ref _Height, value);
        }
        public List<Point> Points
        {
            get => _points;
            set => this.RaiseAndSetIfChanged(ref _points, value);
        }

        public IBrush? Fill
        {
            get => _fill;
            set => this.RaiseAndSetIfChanged(ref _fill, value);
        }

        public IBrush? OpacityMask
        {
            get => _opacityMask;
            set => this.RaiseAndSetIfChanged(ref _opacityMask, value);
        }

        public Geometry? Clip
        {
            get => _clip;
            set => this.RaiseAndSetIfChanged(ref _clip, value);

        }
        #endregion
        ///////
        //Fill="{Binding FillPrev}" Width="{Binding FillWidth}" Height="{Binding FillHeight}"
        //			   Canvas.Left="{Binding FillLeft}" Canvas.Top="{Binding FillTop}"
        #region Prev
        public int PrevLeft
        {
            get => _prevX;
            set => this.RaiseAndSetIfChanged(ref _prevX, value);
        }

        public int PrevTop
        {
            get => _prevY;
            set => this.RaiseAndSetIfChanged(ref _prevY, value);
        }

        public int PrevWidth
        {
            get => _prevWidth;
            set => this.RaiseAndSetIfChanged(ref _prevWidth, value);
        }

        public int PrevHeight
        {
            get => _prevHeight;
            set => this.RaiseAndSetIfChanged(ref _prevHeight, value);
        }

        public IBrush? PrevFill
        {
            get => _prevfill;
            set => this.RaiseAndSetIfChanged(ref _prevfill, value);
        }
        #endregion
        #region Next
        public int NextLeft
        {
            get => _nextX;
            set => this.RaiseAndSetIfChanged(ref _nextX, value);
        }

        public int NextTop
        {
            get => _nextY;
            set => this.RaiseAndSetIfChanged(ref _nextY, value);
        }

        public int NextWidth
        {
            get => _nextWidth;
            set => this.RaiseAndSetIfChanged(ref _nextWidth, value);
        }

        public int NextHeight
        {
            get => _nextHeight;
            set => this.RaiseAndSetIfChanged(ref _nextHeight, value);
        }

        public IBrush? NextFill
        {
            get => _nextfill;
            set => this.RaiseAndSetIfChanged(ref _nextfill, value);
        }

        // Angle="{Binding NextTrAngle}" CenterX="{Binding NextCenterX}" CenterY="{Binding NextCenterY}
        public int NextTrAngle
        {
            get => _nextTrAngle;
            set => this.RaiseAndSetIfChanged(ref _nextTrAngle, value);
        }
        public int NextCenterX
        {
            get => _nextCenterX;
            set => this.RaiseAndSetIfChanged(ref _nextCenterX, value);
        }
        public int NextCenterY
        {
            get => _nextCenterY;
            set => this.RaiseAndSetIfChanged(ref _nextCenterY, value);
        }
        public Geometry? NextClip
        {
            get => _nextclip;
            set => this.RaiseAndSetIfChanged(ref _nextclip, value);
        }

        #endregion
        #region Current
        public int CurrentLeft
        {
            get => _currentX;
            set => this.RaiseAndSetIfChanged(ref _currentX, value);
        }

        public int CurrentTop
        {
            get => _currentY;
            set => this.RaiseAndSetIfChanged(ref _currentY, value);
        }

        public int CurrentWidth
        {
            get => _currentWidth;
            set => this.RaiseAndSetIfChanged(ref _currentWidth, value);
        }

        public int CurrentHeight
        {
            get => _currentHeight;
            set => this.RaiseAndSetIfChanged(ref _currentHeight, value);
        }

        public IBrush? CurrentFill
        {
            get => _currentfill;
            set => this.RaiseAndSetIfChanged(ref _currentfill, value);
        }
        #endregion

        #endregion

        #region приватные 

        #region test 
        // Позиция полигона на Canvas
        int _polyX = 350;
        int _polyY = 350;
        int _Width = 750;
        int _Height = 750;

        // Points для Polygon (строка "x1,y1 x2,y2 ...")
        List<Point> _points = new List<Point> { new Point(0, 0), new Point(200, 0), new Point(200, 120), new Point(0, 120) };


        // Fill (ImageBrush) и OpacityMask (LinearGradientBrush)
        IBrush? _fill;
        IBrush? _opacityMask;

        // Clip geometry
        Geometry? _clip;

        // Для обработки указателя
        PixelPoint _lastPointer;
        #endregion
        #region prev rectangle
        int _prevX = 0;
        int _prevY = 0;
        int _prevWidth = 0;
        int _prevHeight = 0;
        IBrush? _prevfill;
        #endregion
        #region _next rectangle
        int _nextX = 0;
        int _nextY = 0;
        int _nextWidth = 0;
        int _nextHeight = 0;
        int _nextCenterY = 0;
        int _nextCenterX = 0;
        int _nextTrAngle = 0;
        IBrush? _nextfill;
        Geometry? _nextclip;
        #endregion
        #region _current rectangle
        int _currentX = 350;
        int _currentY = 350;
        int _currentWidth = 750;
        int _currentHeight = 750;
        IBrush? _currentfill;
        #endregion

        #endregion


        #endregion

        #region routed events from code behinde
        /// <summary>
        /// events
        /// </summary>
        /// <param name="e"></param>
        /// <param name="sender"></param>
        public void OnResized(SizeChangedEventArgs e, Control? sender)
        {

            _Bounds = sender.Bounds;
            if (_images == null || _selImages == null) return;
            SizedCalcs();
            //сбросим все при измении размеров элемента
            ResetSelectedBuffer();
            //и перемасштабируем используемые картинки
            ResizePrevCurrent();
            Update();
        }
        public void OnLoaded(RoutedEventArgs e, Control sender, double renderScaling = 1)
        {
            _RenderScaling = renderScaling;
            _Bounds = sender.Bounds;
            sender.Unloaded += OnUnload;
            _pImgIndex = 0;
            SizedCalcs();
            TryLoadImages();
            ResizePrevCurrent();
            NewCorner();
            Update();
            _wasInit = true;
        }
        private void OnUnload(object sender, RoutedEventArgs e)
        {
            ResetSelectedBuffer();
            _pImgIndex = 0;

        }
        public void OnPointerReleased(Control sender, PointerReleasedEventArgs e)
        {
            if (!_corner.IsDragging) return;

            e.Pointer.Capture(null);

            if ((!_corner.MoveBack && (_corner.Pointer.X) < _Bounds.Width * 0.75 && _images.Length > _pImgIndex))
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
                    _selImages[i] = null;
                }
                _pImgIndex += 2;

            }

            _corner.Update(new Point(_w, _h), _w, _h, _selImages, _RenderScaling, false);

            ResizePrevCurrent();
            Update();

        }

        public void OnPointerPressed(PointerPressedEventArgs e, Control sender)
        {
            var p = e.GetPosition(sender);
            e.Pointer.Capture(sender);
            if (!_corner.MoveBack && p.X > _w - 100 && p.Y > _h - 100 && _pImgIndex < _images.Length - 1)
            {
                _corner.Update(p, _w, _h, _selImages, _RenderScaling, true);
                Update();
                return;
            }

            if (!_corner.MoveBack && p.X < 100 && p.Y > _h - 100 && _pImgIndex > 1)
            {
                _pImgIndex -= 2;
                //сбросим все
                ResetSelectedBuffer();
                ResizePrevCurrent();
                e.Pointer.Capture(sender);
                Update();
                _corner.Update(p, _w, _h, _selImages, _RenderScaling, true, true);
                Update();
                return;
            }
        }
        public void OnPointerMoved(PointerEventArgs e, Control? sender)
        {
            if (!_corner.IsDragging)
            {
                return;
            }
            _corner.Update(e.GetPosition(sender), _w, _h, _selImages, _RenderScaling, true);
            Update();
        }
        #endregion

        #region логика обновление
        /// <summary>
        /// ////
        /// </summary>
        /// 
        /// <summary>
        /// Преобразует bitmap к targetSize с сохранением пропорций и обрезкой
        /// только сверху (y от 0) и справа (x от width -> width - cropped).
        /// </summary>
        /// 
        private IImageBrush CreateImageBrush(Bitmap srcBmap, RelativeRect srcRelRect, RelativeRect dstRelRect, Stretch stretch = Stretch.None)
        {
            return new ImageBrush
            {
                Source = srcBmap,
                Stretch = stretch,
                SourceRect = srcRelRect,
                DestinationRect = dstRelRect
            };
        }

        private IImageBrush CreateImageBrush(Bitmap srcBmap, Rect srcRect, Rect dstRect, Stretch stretch = Stretch.None, RelativeUnit units = RelativeUnit.Absolute)
        {
            // RelativeRect srcRelRect = new RelativeRect(srcRect, units);
            // RelativeRect dstRelRect = new RelativeRect(dstRect, units);
            double wd = srcRect.Width - (dstRect.Width);
            double hd = srcRect.Height - (dstRect.Height);
            RelativeRect srcRelRect = new RelativeRect(srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height, units);
            RelativeRect dstRelRect = new RelativeRect(dstRect.X + wd / 2, dstRect.Y + hd / 2, dstRect.Width, dstRect.Height, units);
            return CreateImageBrush(srcBmap, srcRelRect, dstRelRect, stretch);
        }
        private void Update()
        {
            Width = (int)_Bounds.Width;
            Height = (int)_Bounds.Height;

            var render = new RenderTargetBitmap(new PixelSize((int)(_w * _RenderScaling), (int)(_h * _RenderScaling)));

            using (var ctx = render.CreateDrawingContext(true))
            {
                if (_images == null || _images.Length == 0 || _selImages == null || _corner.Freeze)
                    return;

                _corner.PagesRender(ctx, _pImgIndex == 0, _pImgIndex >= _images.Length && _images.Length % 2 == 0);

                //var _cur = render;
                var _cur = ResizeCoverCropTopRight(render, new PixelSize((int)(_RenderScaling * (double)render.PixelSize.Width), (int)(_RenderScaling * (double)render.PixelSize.Height)));
                if (_cur != null)
                {
                    CurrentFill = CreateImageBrush(_cur, new Rect(0, 0, _w, _h), new Rect(0, 0, _w * _RenderScaling, _h * _RenderScaling));
                }
            }
        }
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
                                                                                                  //   RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.HighQuality);
                    ctx.DrawImage(source, srcRect, dstRect);
                }
            }

            return render;
        }
        private void NewCorner()
        {
            //_corner = new CornerModel(new Point(_w, _h), _w, _h, SelectImage(_indSelectorEnum._nextImgIndex)?.PixelSize, SelectImage(_indSelectorEnum._nextNextImgIndex)?.PixelSize);
            _corner = new CornerModel(new Point(_w, _h), _w, _h, _offsetX, _offsetY, _selImages, _RenderScaling);
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
                // _selImages[0] = _images[_images.Length - 3];
                //_selImages[1] = _images[_images.Length - 2];
                return;
            }

            for (int i = 0; i < 4 && _images.Length > (_pImgIndex + i - 1); i++)
            {
                if (_selImages[i] == null && _pImgIndex + i > 0)
                    //   _selImages[i] = _images[_pImgIndex + i - 1];
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
            _w = _Bounds.Width;
            _h = _Bounds.Height;
            _offsetX = 15;
            _offsetY = 15;
            _w -= _offsetX * 2;
            _h -= _offsetY * 2;
            _imgSize = new PixelSize((int)((_w / 2)), (int)(_h));
            CurrentWidth = (int)(_w * _RenderScaling);  //_w;//(int)(_imgSize.Width);//(int)(_w / 2);
            CurrentHeight = (int)(_h * _RenderScaling);//_h;//(int)(_imgSize.Height - 3);//(int)(_h / 2);
            NextWidth = (int)(_imgSize.Width);
            NextHeight = (int)(_imgSize.Height - 3);
            PrevHeight = (int)(_imgSize.Height - 3);
            PrevWidth = (int)(_imgSize.Width);
            NextTop = 0;
            NextLeft = 0;
            CurrentTop = 0;
            CurrentLeft = 0;
            NewCorner();
        }

        private void ResetSelectedBuffer()
        {
            for (int i = 0; i < 4; i++)
                _selImages[i] = null;
        }

        private void TryLoadImages()
        {
            _images = new ImageStore(_imageDir);
            _pImgIndex = 0;
        }

        protected override void Dispose(bool disposing)
        {

        }

        private enum _indSelectorEnum
        {
            _prevImgIndex = 0,
            _currentImgIndex = 1,
            _nextImgIndex = 2,
            _nextNextImgIndex = 3
        }

        #endregion

        #region приватные 
        private int _pImgIndex;

        private Bitmap[] _selImages = new Bitmap[4] { null, null, null, null };

        private ImageStore _images;
        private CornerModel _corner;

        private double _w, _h, _offsetX, _offsetY;
        private PixelSize _imgSize = new PixelSize();
        
        private string _imageDir = "";

        private bool _wasInit = false;
        private Rect _Bounds;
        private double _RenderScaling = 1;

        #endregion

    }
}
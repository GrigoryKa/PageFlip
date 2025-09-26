using Avalonia;
using Avalonia.Controls.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace PageFlip.Corner
{

    public class CornerModel 
    {
        public bool MoveBack => _MoveBack;
        public bool IsDragging => _isDraging;
        public bool Freeze => _freeze;
        public Point Bs0 => _Bs0;
        public Point M => _M;
        public Point BLst => _BLst;
        //когда на верху Bast - перпендикуляр к B'A иначе _Csth
        public Point Bast => _Csth.X < (_w0) && _Cstw.Y < (50) ? _Bast : _Csth;
        public Point Ast => _Ast;
        public Point Csth => _Csth;
        public Point Cstw => _Cstw;
        public Point Cst => _Cst;
        public Point Pointer => _pointer;
        public double ClipX => _clipX;
        public double AreaH => _h0;
        public double AreaW => _w0;
        public double NextH => _hNext;
        public double NextW => _wNext;
        public double CornerH => _cornerH;
        public double CornerW => _cornerW;
        public double CornerHNext => _cornerHNext;
        public double CornerWNext => _cornerWNext;
        public double LDist => _lDist;
        public double MlDist => _mlDist;
        public double TetaRad => _teta;
        public double TetaDeg => _teta * 180 / Math.PI;
        public CornerModel(Point pointer, double areaW, double areaH,
            double offsetX, double offsetY,  Bitmap[]  selImages
           )
        {
            _offsetX = offsetX   ;
            _offsetY  = offsetY  ;
            Update(pointer, areaW, areaH,  selImages);
        }
        public CornerModel(Point pointer, double areaW, double areaH,
           double offsetX, double offsetY, Bitmap[] selImages, double renderSacling
          )
        {
            _offsetX = offsetX ;
            _offsetY = offsetY ;
            Update(pointer, areaW , areaH , selImages, renderSacling);
        }
        public void Update(Point pointer, double areaW, double areaH,
                            Bitmap[] selImages, double renderSacling, bool isDragging = false, bool moveBack = false)
        {
            _renderSacling = renderSacling*0.2 ;

            //scaling
            pointer = new Point(pointer.X , pointer.Y);
            if (selImages!=null && selImages[1] != null)
            {
                var v = (areaH - selImages[1].PixelSize.Height);
            }
            ///
            Update(pointer, areaW , areaH , selImages, isDragging, moveBack );
        }
        public void Update(Point pointer, double areaW, double areaH,
                             Bitmap[] selImages, bool isDragging = false, bool moveBack = false)
        { 
            _selImages = selImages;
            var nextPageSize = SelectImage(_indSelectorEnum._nextImgIndex)?.PixelSize;
            var NextNextPageSize = SelectImage(_indSelectorEnum._nextNextImgIndex)?.PixelSize;
            _isDraging = isDragging;
            _MoveBack = moveBack;
            
            if (pointer.X < 20)
                return;
            _freeze = true;
            _pointer = pointer;
            _selImages = selImages;
            double maxdH = (60 / areaW) * pointer.X;
            _Bs0 = new Point(Clamp(Pointer.X, 1, Math.Min(areaW - 2, Ast.X + 15)), Clamp(Pointer.Y, areaH - maxdH, areaH - 2));

            _w0 = areaW;
            _h0 = areaH;
            _wNext = nextPageSize?.Width ?? -1;
            _hNext = nextPageSize?.Height ?? -1;
            _wNextNext = NextNextPageSize?.Width ?? -1;
            _hNextNext = NextNextPageSize?.Height ?? -1;

            _T = ClampToPage(_Bs0, _w0, _h0);
            _clipX = Math.Min(_w0, Math.Max(0.000000001, _T.X));
            _cornerW = _w0 - _T.X;
            _cornerH = _h0 - _T.Y;
            _slpbst = (_cornerH == 0 ? 0.000000001 : _cornerH) / (_cornerW == 0 ? 0.000000001 : _cornerW);
            _bbst = _h0 - _w0 * _slpbst;
            _MLfunc = (lx0, bbst, slpbst) => bbst + slpbst * lx0;
            _M = new Point(_T.X + (_cornerW / 2), _MLfunc(_T.X + (_cornerW / 2), _bbst, _slpbst));
             _LfuncY = (lx1, slpbst, M) => _normPointY(lx1, slpbst, M); 
            _LfuncX = (ly1, slpbst, M) => _normPointX(ly1, slpbst, M); 
            _Ast = new Point(_LfuncX(_h0, _slpbst, _M), _h0);
            _Csth = new Point(_LfuncX(0, _slpbst, _M), 0.000000001);
            _Cstw = new Point(_w0, _LfuncY(_w0, _slpbst, _M));
            if (_Cstw.Y == double.NegativeInfinity && Debugger.IsAttached) Debugger.Break();

            _Cst = _Csth.X < (_w0) && _Cstw.Y < (50) ? _Csth : _Cstw;
            _lDist = Dist(_Cst, _Ast);
            if ((_lDist == double.MaxValue || _lDist == double.PositiveInfinity || _lDist.ToString().Contains("∞")) && Debugger.IsAttached) Debugger.Break();
            _mlDist = Dist(_Bs0, _Ast);
            _cornerWNext = _Bs0.X < (0 * 2) + (_w0 / 2) ? _wNext : Math.Max(_w0 - Bs0.X, _w0 - Ast.X);
            _cornerHNext = _Bs0.Y < 0 * 2 ? _hNext : Math.Max(_h0 - _Bs0.Y, _h0 - _Cst.Y);
            _teta = Math.Atan2(_h0 - _Bs0.Y + 0, _Ast.X - _Bs0.X + 1.5 * 0);
            _slpbast = (_h0 - _Bs0.Y) / (_Ast.X - _Bs0.X);
            _Bast = new Point(_LfuncX(0, _slpbast, _Bs0), 0.000000001);
            _BLst = new Point(_LfuncX(0, _slpbst, _Bs0), 0.000000001);
            _freeze = false;
        }

        public void PagesRender(DrawingContext dc, bool firstPage, bool lastPageNotOrder )
        {
            //
            var brBlack = new SolidColorBrush(Color.FromRgb(40, 40, 40), 0.5);
            var brBlack1 = new SolidColorBrush(Color.FromRgb(40, 40, 40), 0.4);
            var brGreen1 = new SolidColorBrush(Color.FromRgb(32, 83, 60), 0.4);
            var brGray = new SolidColorBrush(Color.FromRgb(70, 70, 70), 0.6);
            var brWhiteGray = new SolidColorBrush(Color.FromRgb(190, 190, 180), 1);
            var brWhite= new SolidColorBrush(Color.FromRgb(252, 252, 252), 0.99);
            var brWhiteGray1 = new SolidColorBrush(Color.FromRgb(100, 100, 90), 0.6);
            var penBlack = new Pen(brBlack);
            var penGray = new Pen(brGray);
            var penWhiteGray = new Pen(brWhiteGray1);
            /////

            if (_w0 <= 0 || _h0 <= 0) return;

            ///// границы элемента
            var brec = new Rect( _offsetX,  _offsetY,  _w0,  _h0);
            var brec1 = new Rect( _offsetX / 2,  _offsetY / 2,  (_w0 + _offsetX), (_h0 + _offsetY));
       
            //обрамление страниц 
            // если нулевая страница обводим только правую половину
            Rect pageRect, pageRect1;
            if (firstPage)
            {
                pageRect = new Rect(brec.Width / 2 + _offsetX, brec.Top,  brec.Width / 2,  brec.Height);
                pageRect1 = new Rect(AreaW / 2, 0, AreaW / 2, AreaH);

            } //если последняя страница и нечетное число только левую половину
            else if (lastPageNotOrder)
            {
                pageRect = new Rect(_offsetX, brec.Top, brec.Width / 2, brec.Height);
                pageRect1 = new Rect(0, 0, AreaW / 2, AreaH);
            }
            else
            {
                pageRect = brec;
                pageRect1 = new Rect(0, 0, AreaW, AreaH);
            }

            //края подложки
            dc.DrawRectangle(brGreen1, penBlack, new RoundedRect(brec1, 5));
            dc.DrawRectangle(brWhite, penBlack, pageRect);

            // Затем сдвигаем — всё рисование 
            using (dc.PushTransform((new TranslateTransform(_offsetX, _offsetY)).Value))
            //using (dc.PushGeometryClip(new RectangleGeometry(new Rect(0, 0, brect.Width, brect.Height))))
            {

                var rectLocal0 = new Rect(0, 0, AreaW, AreaH);

                //    open book
                // ---------------
                // page1  |  page2
                // _prev  |  _current
                // =============== 
                //_next -- обратная сторона текущей страницы, которая при перевертывании переходит на page1 в _prev
                // предыдущая страница
                var _prev = SelectImage(_indSelectorEnum._prevImgIndex);
                if (_prev != null)
                {
                    dc.DrawImage(_prev, new Rect(0, 0, _prev.PixelSize.Width, _prev.PixelSize.Height), new Rect(0, 0, _prev.PixelSize.Width, _prev.PixelSize.Height ));
                }
                // рисуем текущую страницу 
                var _current = SelectImage(_indSelectorEnum._currentImgIndex);
                if (_current != null)
                {
                    dc.DrawImage(_current, new Rect(0, 0, _current.PixelSize.Width, _current.PixelSize.Height), new Rect(_w0 / 2, 0, _current.PixelSize.Width , _current.PixelSize.Height ));
                }

                var _next = SelectImage(_indSelectorEnum._nextImgIndex);
                var _nextNext = SelectImage(_indSelectorEnum._nextNextImgIndex);
                ///// 
                //центр вертикальный градиент между страницами 
                var gstp3 = new GradientStops{
                    new GradientStop(Color.FromArgb(0,120, 120,  120), 0.10),
                    new GradientStop(Color.FromArgb(180,40, 40,  40), 0.50),
                    new GradientStop(Color.FromArgb(0,120, 120,  120),0.9),
                    };
                using (dc.PushGeometryClip(new RectangleGeometry(pageRect1)))
                    DrawPerpendicularGradient(dc, rectLocal0, new Point(AreaW / 2, 0), new Point(AreaW / 2, AreaH), gstp3, 0.02);

                //////
                if (!_isDraging || _next == null)
                {             
                    return;
                }
           
                var brTrap2 = new SolidColorBrush(Color.FromRgb(124, 120, 124), 0.4);
                var brTrap1 = new SolidColorBrush(Color.FromRgb(124, 120, 124), 0.1);
            
                var A = Bs0;
                var B = Cst;
                var Bl = Bast.X > 1 && Bast.X + 5 < AreaW ? Bast : B;
                var C = Ast;
                var D = new Point(Bs0.X - 1, Bs0.Y - 1);
                Point E = new Point(0, 0);
                var lDist = LDist;
                var mlDist = MlDist;

                double cornerWNext = CornerWNext;
                double cornerHNext = CornerHNext;
              
                // геометрия угла обрез 
                var trap1 = new StreamGeometry();
                using (var ctx = trap1.Open())
                {
                    ctx.BeginFigure(A, true);
                    ctx.LineTo(Bl);
                    ctx.LineTo(B);
                    ctx.LineTo(C);
                    ctx.LineTo(D);
                    ctx.EndFigure(true);
                }
                ///
                var trap3 = new StreamGeometry();
                using (var ctx = trap3.Open())
                {
                    ctx.BeginFigure(new Point(A.X - 7, A.Y - 7), true);
                    ctx.LineTo(new Point(Bl.X - 17, 0));
                    ctx.LineTo(new Point(B.X, B.Y));
                    ctx.LineTo(new Point(C.X - 7, C.Y - 7));
                    ctx.LineTo(new Point(D.X - 7, D.Y - 7));
                    ctx.EndFigure(true);
                }
                //trap2
                A = Ast;
                B = Cst;
                Bl = new Point(AreaW, Cst.Y);
                C = new Point(AreaW, Ast.Y);
                D = new Point(Ast.X - 1, Ast.Y - 1);

                // геометрия трапеции
                var trap2 = new StreamGeometry();
                using (var ctx = trap2.Open())
                {
                    ctx.BeginFigure(A, true);
                    ctx.LineTo(B);
                    ctx.LineTo(Bl);
                    ctx.LineTo(C);
                    ctx.LineTo(D);
                    ctx.EndFigure(true);
                }

                if (CornerW > 1 && CornerH > 1)
                {
                    ///right part NextW
                    Rect src, dst;
                    //тень слева под углом
                    using (dc.PushGeometryClip(trap3))
                    {
                        var gstp0 = new GradientStops{
                    new GradientStop(Color.FromArgb(0,130, 130,  130), 0),
                    new GradientStop(Color.FromArgb(220,80, 80,  80), 0.40),
                    new GradientStop(Color.FromArgb(155,20, 20,  20),0.75),
                    };
                        DrawPerpendicularGradient(dc, rectLocal0, Bs0, Bast.X > 1 && Bast.X + 5 < AreaW ? Bast : Cst, gstp0, 0.01);
                    }
                    using (dc.PushGeometryClip(trap2))
                    {
                        if (_nextNext != null)
                        {
                            src = new Rect(0, 0, NextW, NextH);
                            dst = new Rect(AreaW  / 2, 0, AreaW / 2, AreaH );
                            dc.DrawImage(_nextNext, src, dst);
                        }
                        //четная нет страницы _nextNext
                        else
                        {
                            dc.DrawGeometry(brWhiteGray, penWhiteGray, trap2);
                        }
                        //тень справа
                        var gstp1 = new GradientStops{
                                         new GradientStop(Color.FromArgb(0,40, 40,  40), 0),
                                         new GradientStop(Color.FromArgb(200,30, 30,  30), 0.70),
                                         new GradientStop(Color.FromArgb(255,0, 0,  0),0.85),
                                         };
                        DrawPerpendicularGradient(dc, rectLocal0, Cst, Ast, gstp1, 0.02);
                        if (!_MoveBack)
                            dc.DrawGeometry(brBlack1, penWhiteGray, trap2);
                    }

                    ////обводка  белое подложка
                    dc.DrawGeometry(brWhite, penWhiteGray, trap1);

                    src = new Rect(0, NextH - CornerHNext, CornerWNext, cornerHNext);              
                    dst = new Rect(Bs0.X, Bs0.Y - cornerHNext , cornerWNext , cornerHNext );
                    //сам угол с поворотом
                    var tr = new RotateTransform(TetaDeg, Bs0.X, Bs0.Y);
                    using (dc.PushGeometryClip(trap1))
                    {
                        using (dc.PushTransform(tr.Value))
                        {
                          dc.DrawImage(_next, src, dst);
                        }
                    //яркость белое градиент на сгибе
                    var gstp2 = new GradientStops{
                    new GradientStop(Color.FromArgb(10,50, 50, 50), 0),
                    new GradientStop(Color.FromArgb(100,125, 125, 125),0.13),
                    new GradientStop(Color.FromArgb(210,255, 255, 255),0.40),
                    new GradientStop(Color.FromArgb(25,0, 0, 0),0.75)
                    };
                        DrawPerpendicularGradient(dc, rectLocal0, Ast, Cst, gstp2, 0.09);
                    }
                    //обводка угла
                    dc.DrawGeometry(brTrap1, penWhiteGray, trap1);
                }
            }           
        }
        public enum _indSelectorEnum
        {
            _prevImgIndex = 0,
            _currentImgIndex = 1,
            _nextImgIndex = 2,
            _nextNextImgIndex = 3
        }
        public Bitmap SelectImage(_indSelectorEnum indIm) => _selImages[(int)indIm]??null;
        private void DrawPerpendicularGradient(DrawingContext dc, Rect brec, Point p0, Point p1, GradientStops GStops, double scale = 0.01)
        {
            // Переводим точки в локальные координаты brec 
            double lx0 = p0.X - brec.X, ly0 = p0.Y - brec.Y;
            double lx1 = p1.X - brec.X, ly1 = p1.Y - brec.Y;

            // вектор от p0 к p1
            double vx = lx1 - lx0;
            double vy = ly1 - ly0;
            double vlen = Math.Sqrt(vx * vx + vy * vy);
            if (vlen <= 1e-9) return; // точек нет — ничего не рисуем

            // нормаль (перпендикуляр) к линии, нормализуем
            double nx = -vy / vlen;
            double ny = vx / vlen;

            // центр полосы 
            double cx = (lx0 + lx1) * 0.5;
            double cy = (ly0 + ly1) * 0.5;
            double L = Math.Sqrt(brec.Width * brec.Width + brec.Height * brec.Height) * scale;
            var start = new RelativePoint(cx - nx * L, cy - ny * L, RelativeUnit.Absolute);
            var end = new RelativePoint(cx + nx * L, cy + ny * L, RelativeUnit.Absolute);

            // Для резкой полосы используем узкую зону в середине
            var brush = new LinearGradientBrush
            {
                StartPoint = start,
                EndPoint = end,
                GradientStops = GStops
            };

            dc.DrawRectangle(brush, null, brec);
        }
        Point _pointer;
        double _w0;
        double _h0;
        Point _T;
        double _clipX;
        ///
        double _cornerW;
        double _cornerH;
        //B'0
        Point _Bs0;
        //BA' от B' до A'  slope
        double _slpbast;
        Point _Bast;
        Func<double, double, Point, double> _BastfuncY;
        ///////
        /// B' slope
        double _slpbst;
        // 
        //ML  - линия  от B до B' (=  от (w,h) до T (~сам угол))
        //b
        double _bbst;
        Func<double, double, double, double> _MLfunc;
        //M - середина через которую проходит линия сгиба
        Point _M;
        //L линия сгиба как нормаль к ML в точке M
        //y - y0 = (-1/a)(x - x0)
        Func<double, double, Point, double> _LfuncY;
        // x = (y - x0(1/a + a) - b)/(-1/a)
        //y - y0 = (-1/a)x - (-1/a)x0
        // x = (y - y0 - (1/a)x0) / (-1/a)
        // x = x0 - a(y - y0)
        Func<double, double, Point, double> _LfuncX;
        //A' пересечение L c низом страницы y=h       
        Point _Ast = new Point(int.MaxValue, 0);
        //C'h пересечение L с верхом y=0
        //var Csth = new Point(LfuncX(_offsetY), _offsetY); //new Point(LfuncY(0), 0);
        Point _Csth;
        //C'w пересечение L с правым краем x=w
        //var Cstw = new Point(w0 - _offsetX, LfuncY(w0 - _offsetX)); //new Point(LfuncY(0), 0);
        Point _Cstw;
        //C' если пересекает верх то C'h иначе C'w 
        Point _Cst;
        //BL нормаль к ML  в точке B'
        //BL'/ BLst пересечение с верхом
        Point _BLst;
        // x = (y - x0(1/a + a) - b)/(-1/a)
        // y - y0 = (-1/a)x - (-1/a)x0
        // x = (y - y0 - (1/a)x0) / (-1/a)
        // x = x0 - a(y - y0)
        // y на перпедикуляре по x к прямой с угловой коэффициентом slpbst
        private static Func<double, double, Point, double> _normPointY = (lx1, slpbst, M) => (-1 / slpbst) * (lx1 - M.X) + M.Y;
        //x на перпедикуляре по y к прямой с угловой коэффициентом slpbst
        private static Func<double, double, Point, double> _normPointX = (ly1, slpbst, M) => M.X - slpbst * (ly1 - M.Y);
        private static Point ClampToPage(Point p, double w, double h) => new Point(Clamp(p.X, 0.000000001, w), Clamp(p.Y, 0.000000001, h));
        private static double Clamp(double v, double a, double b) => Math.Max(a, Math.Min(b, v));
        private static double Dist(Point p1, Point p2) => Math.Sqrt((p1.Y - p2.Y) * (p1.Y - p2.Y) + (p1.X - p2.X) * (p1.X - p2.X));
        double _lDist, _mlDist, _wNext, _hNext, _wNextNext, _hNextNext, _cornerWNext, _cornerHNext, _offsetX, _offsetY, _renderSacling = 1;
        double _teta; 
        bool _updtCall = false;
        private Bitmap[] _selImages = new Bitmap[4] { null, null, null, null };
        private bool _isDraging, _MoveBack , _freeze = false;
    }


}

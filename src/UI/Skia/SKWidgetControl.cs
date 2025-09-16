/*
 * EFT DMA Radar Lite
 * Brought to you by Lone (Lone DMA)
 * 
MIT License

Copyright (c) 2025 Lone DMA

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 *
*/

using EftDmaRadarLite.Misc;
using SkiaSharp.Views.WPF;

namespace EftDmaRadarLite.UI.Skia
{
    public abstract class SKWidgetControl : IDisposable
    {
        #region Fields
        private readonly Lock _sync = new();
        private readonly SKGLElement _parent;
        private bool _titleDrag;
        private bool _resizeDrag;
        private Vector2 _lastMousePosition;
        private SKPoint _location = new(1, 1);
        private SKSize _size = new(200, 200);
        private SKPath _resizeTriangle;
        private float _relativeX;
        private float _relativeY;
        #endregion

        #region Private Properties
        private float TitleBarHeight => 12.5f * ScaleFactor;
        private SKRect TitleBar => new(Rectangle.Left, Rectangle.Top, Rectangle.Right, Rectangle.Top + TitleBarHeight);
        private SKRect MinimizeButton => new(TitleBar.Right - TitleBarHeight,
            TitleBar.Top, TitleBar.Right, TitleBar.Bottom);
        #endregion

        #region Protected Properties
        protected string Title { get; }
        protected bool CanResize { get; }
        protected float ScaleFactor { get; private set; }
        protected SKPath ResizeTriangle => _resizeTriangle;
        #endregion

        #region Public Properties
        public bool Minimized { get; protected set; }
        public SKRect ClientRectangle => new(Rectangle.Left, Rectangle.Top + TitleBarHeight, Rectangle.Right, Rectangle.Bottom);
        public SKSize Size
        {
            get => _size;
            set
            {
                lock (_sync)
                {
                    if (!value.Width.IsNormalOrZero() || !value.Height.IsNormalOrZero())
                        return;
                    if (value.Width < 0f || value.Height < 0f)
                        return;
                    value.Width = (int)value.Width;
                    value.Height = (int)value.Height;
                    _size = value;
                    InitializeResizeTriangle();
                }
            }
        }
        public SKPoint Location
        {
            get => _location;
            set
            {
                lock (_sync)
                {
                    if ((value.X != 0f && !value.X.IsNormalOrZero()) ||
                        (value.Y != 0f && !value.Y.IsNormalOrZero()))
                        return;
                    var size = _parent.CanvasSize;
                    var cr = new SKRect(0, 0, (int)Math.Round(size.Width), (int)Math.Round(size.Height));
                    if (cr.Width == 0 ||
                        cr.Height == 0)
                        return;
                    _location = value;
                    CorrectLocationBounds(cr);
                    _relativeX = value.X / cr.Width;
                    _relativeY = value.Y / cr.Height;
                    InitializeResizeTriangle();
                }
            }
        }
        public SKRect Rectangle => new SKRect(Location.X,
            Location.Y,
            Location.X + Size.Width,
            Location.Y + Size.Height + TitleBarHeight);
        #endregion

        #region Constructor
        protected SKWidgetControl(SKGLElement parent, string title, SKPoint location, SKSize clientSize, float scaleFactor, bool canResize = true)
        {
            _parent = parent;
            CanResize = canResize;
            Title = title;
            ScaleFactor = scaleFactor;
            Size = clientSize;
            Location = location;
            parent.MouseLeave += Parent_MouseLeave;
            parent.MouseMove += Parent_MouseMove;
            parent.MouseDown += Parent_MouseDown;
            parent.MouseUp += Parent_MouseUp;
            parent.SizeChanged += Parent_SizeChanged;
            InitializeResizeTriangle();
            CanResize = canResize;
        }

        #endregion

        #region Hooked Parent Events

        private void Parent_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var element = (IInputElement)sender;
            Point pos = e.GetPosition(element);
            _lastMousePosition = new((float)pos.X, (float)pos.Y);
            var test = HitTest(new SKPoint((float)pos.X, (float)pos.Y));
            switch (test)
            {
                case SKWidgetClickEvent.ClickedMinimize:
                    Minimized = !Minimized;
                    Location = Location;
                    break;
                case SKWidgetClickEvent.ClickedTitleBar:
                    _titleDrag = true;
                    break;
                case SKWidgetClickEvent.ClickedResize:
                    _resizeDrag = true;
                    break;
            }
        }

        private void Parent_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _titleDrag = false;
            _resizeDrag = false;
        }

        private void Parent_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _titleDrag = false;
            _resizeDrag = false;
        }

        private void Parent_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var element = (IInputElement)sender;
            Point pos = e.GetPosition(element);
            if (_resizeDrag && CanResize)
            {
                if (pos.X < Rectangle.Left || pos.Y < Rectangle.Top)
                    return;
                var newSize = new SKSize(Math.Abs(Rectangle.Left - (float)pos.X), Math.Abs(Rectangle.Top - (float)pos.Y));
                Size = newSize;
            }
            else if (_titleDrag)
            {
                int deltaX = (int)Math.Round(pos.X - _lastMousePosition.X);
                int deltaY = (int)Math.Round(pos.Y - _lastMousePosition.Y);
                var newLoc = new SKPoint(Location.X + deltaX, Location.Y + deltaY);
                // Set the new location
                Location = newLoc;
            }
            _lastMousePosition = new((float)pos.X, (float)pos.Y);
        }

        private void Parent_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var size = _parent.CanvasSize;
            var cr = new SKRect(0, 0, (int)Math.Round(size.Width), (int)Math.Round(size.Height));

            // Calculate the new location based on relative position
            Location = new SKPoint(
                cr.Width * _relativeX,
                cr.Height * _relativeY);
        }
        #endregion

        #region Public Methods
        public virtual void Draw(SKCanvas canvas)
        {
            if (!Minimized)
                canvas.DrawRect(Rectangle, WidgetBackgroundPaint);
            canvas.DrawRect(TitleBar, TitleBarPaint);
            float titleCenterY = TitleBar.Top + (TitleBar.Height / 2);

            float titleYOffset = (Font.Metrics.Ascent + Font.Metrics.Descent) / 2;
            canvas.DrawText(
                Title,
                new(TitleBar.Left + 2.5f * ScaleFactor, titleCenterY - titleYOffset),
                SKTextAlign.Left,
                Font,
                TitleBarText);

            canvas.DrawRect(MinimizeButton, ButtonBackgroundPaint);
            // Draw Rest of stuff...
            DrawMinimizeButton(canvas);
            if (!Minimized && CanResize)
                DrawResizeCorner(canvas);
        }

        public virtual void SetScaleFactor(float newScale)
        {
            ScaleFactor = newScale;
            InitializeResizeTriangle();
        }
        #endregion

        #region Private Methods
        private void CorrectLocationBounds(SKRect clientRectangle)
        {
            var rect = Minimized ? TitleBar : Rectangle;

            if (rect.Left < clientRectangle.Left)
                _location = new SKPoint(clientRectangle.Left, _location.Y);
            else if (rect.Right > clientRectangle.Right)
                _location = new SKPoint(clientRectangle.Right - rect.Width, _location.Y);

            if (rect.Top < clientRectangle.Top)
                _location = new SKPoint(_location.X, clientRectangle.Top);
            else if (rect.Bottom > clientRectangle.Bottom)
                _location = new SKPoint(_location.X, clientRectangle.Bottom - rect.Height);
        }
        private void DrawMinimizeButton(SKCanvas canvas)
        {
            float minHalfLength = MinimizeButton.Width / 4;
            if (Minimized)
            {
                canvas.DrawLine(MinimizeButton.MidX - minHalfLength,
                    MinimizeButton.MidY,
                    MinimizeButton.MidX + minHalfLength,
                    MinimizeButton.MidY,
                    SymbolPaint);
                canvas.DrawLine(MinimizeButton.MidX,
                    MinimizeButton.MidY - minHalfLength,
                    MinimizeButton.MidX,
                    MinimizeButton.MidY + minHalfLength,
                    SymbolPaint);
            }
            else
                canvas.DrawLine(MinimizeButton.MidX - minHalfLength,
                    MinimizeButton.MidY,
                    MinimizeButton.MidX + minHalfLength,
                    MinimizeButton.MidY,
                    SymbolPaint);
        }

        private void InitializeResizeTriangle()
        {
            float triangleSize = 10.5f * ScaleFactor;
            var bottomRight = new SKPoint(Rectangle.Right, Rectangle.Bottom);
            var topOfTriangle = new SKPoint(bottomRight.X, bottomRight.Y - triangleSize);
            var leftOfTriangle = new SKPoint(bottomRight.X - triangleSize, bottomRight.Y);

            var path = new SKPath();
            path.MoveTo(bottomRight);
            path.LineTo(topOfTriangle);
            path.LineTo(leftOfTriangle);
            path.Close();
            var old = Interlocked.Exchange(ref _resizeTriangle, path);
            old?.Dispose();
        }

        private void DrawResizeCorner(SKCanvas canvas)
        {
            var path = ResizeTriangle;
            if (path is not null)
                canvas.DrawPath(path, TitleBarPaint);
        }
        private SKWidgetClickEvent HitTest(SKPoint point)
        {
            var result = SKWidgetClickEvent.None;
            bool clicked = point.X >= Rectangle.Left && point.X <= Rectangle.Right && point.Y >= Rectangle.Top && point.Y <= Rectangle.Bottom;
            if (!clicked)
                return result;
            result = SKWidgetClickEvent.Clicked;
            bool titleClicked = point.X >= TitleBar.Left && point.X <= TitleBar.Right && point.Y >= TitleBar.Top && point.Y <= TitleBar.Bottom;
            if (titleClicked)
                result = SKWidgetClickEvent.ClickedTitleBar;
            bool clientClicked = point.X >= ClientRectangle.Left && point.X <= ClientRectangle.Right && point.Y >= ClientRectangle.Top && point.Y <= ClientRectangle.Bottom;
            if (!Minimized && clientClicked)
                result = SKWidgetClickEvent.ClickedClientArea;
            bool minClicked = point.X >= MinimizeButton.Left && point.X <= MinimizeButton.Right && point.Y >= MinimizeButton.Top && point.Y <= MinimizeButton.Bottom;
            if (minClicked)
                result = SKWidgetClickEvent.ClickedMinimize;
            var resizePath = _resizeTriangle;
            if (!Minimized && resizePath is not null && resizePath.Contains(point.X, point.Y))
                result = SKWidgetClickEvent.ClickedResize;
            return result;
        }
        #endregion

        #region Paints
        private static SKPaint WidgetBackgroundPaint { get; } = new SKPaint
        {
            Color = SKColors.Black.WithAlpha(0xBE),
            StrokeWidth = 1,
            Style = SKPaintStyle.Fill,
        };

        private static SKPaint TitleBarPaint { get; } = new SKPaint
        {
            Color = SKColors.Gray,
            StrokeWidth = 0.5f,
            Style = SKPaintStyle.Fill,
        };

        private static SKPaint ButtonBackgroundPaint { get; } = new SKPaint
        {
            Color = SKColors.LightGray,
            StrokeWidth = 0.1f,
            Style = SKPaintStyle.Fill,
        };

        private static SKPaint SymbolPaint { get; } = new SKPaint
        {
            Color = SKColors.Black,
            StrokeWidth = 2f,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };

        private static SKPaint TitleBarText { get; } = new SKPaint
        {
            Color = SKColors.White,
            IsStroke = false,
            IsAntialias = true
        };

        private static SKFont Font { get; } = new SKFont(CustomFonts.NeoSansStdRegular, 9f)
        {
            Subpixel = true
        };

        internal static void SetScaleFactorInternal(float scale)
        {
            Font.Size = 9f * scale;
        }
        #endregion

        #region IDisposable
        private bool _disposed;
        public virtual void Dispose()
        {
            bool disposed = Interlocked.Exchange(ref _disposed, true);
            if (!disposed)
            {
                _parent.MouseLeave -= Parent_MouseLeave;
                _parent.MouseDown -= Parent_MouseDown;
                _parent.MouseUp -= Parent_MouseUp;
                _parent.MouseMove -= Parent_MouseMove;
                _parent.SizeChanged -= Parent_SizeChanged;
                ResizeTriangle?.Dispose();
            }
        }
        #endregion
    }
}
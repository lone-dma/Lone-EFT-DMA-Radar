/*
 * Lone EFT DMA Radar
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

using LoneEftDmaRadar.Misc;

namespace LoneEftDmaRadar.UI.Skia
{
    /// <summary>
    /// Base class for interactive Skia widgets.
    /// Provides dragging, optional resizing, minimizing and basic chrome rendering.
    /// </summary>
    public abstract class AbstractSKWidget : IDisposable
    {
        private readonly Lock _sync = new();

        private bool _titleDrag;
        private bool _resizeDrag;

        private Vector2 _lastMousePosition;

        private SKPoint _location = new(1, 1);
        private SKSize _size = new(200, 200);
        private SKSize _canvasSize;
        private SKPath _resizeTriangle;
        private float _relativeX;
        private float _relativeY;
        private bool _disposed;

        protected virtual float TitlePadding => 2.5f * ScaleFactor;
        protected virtual float BaseFontSize => 9f;
        protected virtual float TitleBarBaseHeight => 12.5f;
        protected virtual float ResizeGlyphBaseSize => 10.5f;

        /// <summary>
        /// Top margin to account for ImGui main menu bar.
        /// </summary>
        protected virtual float TopMargin => 20f;

        private float TitleBarHeight => TitleBarBaseHeight * ScaleFactor;
        private SKRect TitleBar => new(Rectangle.Left, Rectangle.Top, Rectangle.Right, Rectangle.Top + TitleBarHeight);
        private SKRect MinimizeButton => new(TitleBar.Right - TitleBarHeight, TitleBar.Top, TitleBar.Right, TitleBar.Bottom);

        protected string Title { get; }
        protected bool CanResize { get; }
        protected float ScaleFactor { get; private set; }
        protected SKPath ResizeTriangle => _resizeTriangle;

        public bool Minimized { get; protected set; }
        public bool IsMinimized => Minimized;

        public SKRect ClientRectangle => new(
            Rectangle.Left,
            Rectangle.Top + TitleBarHeight,
            Rectangle.Right,
            Rectangle.Bottom);

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
                    var clamped = ClampSize(value);
                    if (clamped.Equals(_size))
                        return;

                    _size = clamped;
                    InitializeResizeTriangle();

                    if (_canvasSize.Width > 0 && _canvasSize.Height > 0)
                    {
                        var canvasRect = new SKRect(0, 0, _canvasSize.Width, _canvasSize.Height);
                        CorrectLocationBounds(canvasRect);
                        _relativeX = canvasRect.Width > 0 ? _location.X / canvasRect.Width : 0f;
                        _relativeY = canvasRect.Height > 0 ? _location.Y / canvasRect.Height : 0f;
                    }
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

                    if (_canvasSize.Width <= 0 || _canvasSize.Height <= 0)
                        return;

                    var canvasRect = new SKRect(0, 0, _canvasSize.Width, _canvasSize.Height);

                    _location = value;
                    CorrectLocationBounds(canvasRect);
                    _relativeX = canvasRect.Width > 0 ? _location.X / canvasRect.Width : 0f;
                    _relativeY = canvasRect.Height > 0 ? _location.Y / canvasRect.Height : 0f;

                    InitializeResizeTriangle();
                }
            }
        }

        public SKRect Rectangle => new(
            Location.X,
            Location.Y,
            Location.X + Size.Width,
            Location.Y + Size.Height + TitleBarHeight);

        protected AbstractSKWidget(
            SKSize canvasSize,
            string title,
            SKPoint location,
            SKSize clientSize,
            float scaleFactor,
            bool canResize = true)
        {
            _canvasSize = canvasSize;
            Title = title;
            CanResize = canResize;
            ScaleFactor = scaleFactor;
            Size = clientSize;
            Location = location;
            InitializeResizeTriangle();
        }

        /// <summary>
        /// Update the canvas size (call on window resize).
        /// </summary>
        public void UpdateCanvasSize(SKSize newSize)
        {
            if (newSize.Width <= 0 || newSize.Height <= 0)
                return;

            lock (_sync)
            {
                _canvasSize = newSize;
                var cr = new SKRect(0, 0, newSize.Width, newSize.Height);
                Location = new SKPoint(cr.Width * _relativeX, cr.Height * _relativeY);
            }
        }

        /// <summary>
        /// Handle mouse down event from the radar.
        /// </summary>
        public void HandleMouseDown(Vector2 position)
        {
            _lastMousePosition = position;
            var pt = new SKPoint(position.X, position.Y);

            switch (HitTest(pt))
            {
                case SKWidgetClickEvent.ClickedMinimize:
                    ToggleMinimized();
                    break;
                case SKWidgetClickEvent.ClickedTitleBar:
                    _titleDrag = true;
                    break;
                case SKWidgetClickEvent.ClickedResize:
                    _resizeDrag = true;
                    break;
            }
        }

        /// <summary>
        /// Handle mouse up event.
        /// </summary>
        public void HandleMouseUp() => CancelInteractions();

        /// <summary>
        /// Handle mouse leave event.
        /// </summary>
        public void HandleMouseLeave() => CancelInteractions();

        /// <summary>
        /// Handle mouse move event.
        /// </summary>
        public void HandleMouseMove(Vector2 position)
        {
            var posF = new SKPoint(position.X, position.Y);

            if (_resizeDrag && CanResize)
            {
                // Calculate new size based on distance from location (top-left of widget)
                // The resize handle is at the bottom-right of the client area
                float newWidth = posF.X - _location.X;
                float newHeight = posF.Y - _location.Y - TitleBarHeight;

                if (newWidth > 0 && newHeight > 0)
                {
                    // Clamp size to canvas bounds to prevent location correction from moving the widget
                    if (_canvasSize.Width > 0 && _canvasSize.Height > 0)
                    {
                        float maxWidth = _canvasSize.Width - _location.X;
                        float maxHeight = _canvasSize.Height - _location.Y - TitleBarHeight;
                        newWidth = Math.Min(newWidth, maxWidth);
                        newHeight = Math.Min(newHeight, maxHeight);
                    }

                    Size = new SKSize(newWidth, newHeight);
                }
            }
            else if (_titleDrag)
            {
                var dx = (int)Math.Round(posF.X - _lastMousePosition.X);
                var dy = (int)Math.Round(posF.Y - _lastMousePosition.Y);
                if (dx != 0 || dy != 0)
                    Location = new SKPoint(Location.X + dx, Location.Y + dy);
            }

            _lastMousePosition = position;
        }

        /// <summary>
        /// Check if mouse position is over this widget.
        /// </summary>
        public bool ContainsPoint(Vector2 position)
        {
            return Rectangle.Contains(position.X, position.Y);
        }

        public virtual void Draw(SKCanvas canvas)
        {
            if (!Minimized)
                canvas.DrawRect(Rectangle, WidgetBackgroundPaint);

            canvas.DrawRect(TitleBar, TitleBarPaint);

            float titleCenterY = TitleBar.MidY;
            float titleYOffset = (Font.Metrics.Ascent + Font.Metrics.Descent) / 2f;
            canvas.DrawText(
                Title,
                new(TitleBar.Left + TitlePadding, titleCenterY - titleYOffset),
                SKTextAlign.Left,
                Font,
                TitleBarText);

            canvas.DrawRect(MinimizeButton, ButtonBackgroundPaint);
            DrawMinimizeButton(canvas);

            if (!Minimized && CanResize)
                DrawResizeCorner(canvas);
        }

        public virtual void SetScaleFactor(float newScale)
        {
            if (newScale <= 0 || !newScale.IsNormalOrZero())
                return;
            ScaleFactor = newScale;
            Font.Size = BaseFontSize * newScale;
            InitializeResizeTriangle();

            if (_canvasSize.Width > 0 && _canvasSize.Height > 0)
            {
                var canvasRect = new SKRect(0, 0, _canvasSize.Width, _canvasSize.Height);
                CorrectLocationBounds(canvasRect);
                _relativeX = canvasRect.Width > 0 ? _location.X / canvasRect.Width : 0f;
                _relativeY = canvasRect.Height > 0 ? _location.Y / canvasRect.Height : 0f;
            }
        }

        protected virtual SKSize ClampSize(SKSize requested)
        {
            const float min = 16f;
            return new SKSize(Math.Max(min, requested.Width), Math.Max(min, requested.Height));
        }

        private void ToggleMinimized()
        {
            Minimized = !Minimized;
            if (_canvasSize.Width > 0 && _canvasSize.Height > 0)
            {
                var canvasRect = new SKRect(0, 0, _canvasSize.Width, _canvasSize.Height);
                CorrectLocationBounds(canvasRect);
                _relativeX = canvasRect.Width > 0 ? _location.X / canvasRect.Width : 0f;
                _relativeY = canvasRect.Height > 0 ? _location.Y / canvasRect.Height : 0f;
            }
        }

        private void CancelInteractions()
        {
            _titleDrag = false;
            _resizeDrag = false;
        }

        private void CorrectLocationBounds(SKRect clientRectangle)
        {
            var rect = Minimized
                ? new SKRect(_location.X, _location.Y,
                    _location.X + Size.Width,
                    _location.Y + TitleBarHeight)
                : Rectangle;

            // Apply top margin to keep widgets below the menu bar
            float minTop = clientRectangle.Top + TopMargin;

            if (rect.Left < clientRectangle.Left)
                _location = new SKPoint(clientRectangle.Left, _location.Y);
            else if (rect.Right > clientRectangle.Right)
                _location = new SKPoint(clientRectangle.Right - rect.Width, _location.Y);

            if (rect.Top < minTop)
                _location = new SKPoint(_location.X, minTop);
            else if (rect.Bottom > clientRectangle.Bottom)
                _location = new SKPoint(_location.X, clientRectangle.Bottom - rect.Height);
        }

        private SKWidgetClickEvent HitTest(SKPoint point)
        {
            if (!Rectangle.Contains(point.X, point.Y))
                return SKWidgetClickEvent.None;
            if (MinimizeButton.Contains(point.X, point.Y))
                return SKWidgetClickEvent.ClickedMinimize;
            if (TitleBar.Contains(point.X, point.Y))
                return SKWidgetClickEvent.ClickedTitleBar;
            if (!Minimized &&
                CanResize &&
                _resizeTriangle is not null &&
                _resizeTriangle.Contains(point.X, point.Y))
                return SKWidgetClickEvent.ClickedResize;
            if (!Minimized && ClientRectangle.Contains(point.X, point.Y))
                return SKWidgetClickEvent.ClickedClientArea;
            return SKWidgetClickEvent.Clicked;
        }

        private void DrawMinimizeButton(SKCanvas canvas)
        {
            float halfLength = MinimizeButton.Width / 4f;
            canvas.DrawLine(
                MinimizeButton.MidX - halfLength,
                MinimizeButton.MidY,
                MinimizeButton.MidX + halfLength,
                MinimizeButton.MidY,
                SymbolPaint);
            if (Minimized)
            {
                canvas.DrawLine(
                    MinimizeButton.MidX,
                    MinimizeButton.MidY - halfLength,
                    MinimizeButton.MidX,
                    MinimizeButton.MidY + halfLength,
                    SymbolPaint);
            }
        }

        private void InitializeResizeTriangle()
        {
            float triSize = ResizeGlyphBaseSize * ScaleFactor;
            var bottomRight = new SKPoint(Rectangle.Right, Rectangle.Bottom);
            var top = new SKPoint(bottomRight.X, bottomRight.Y - triSize);
            var left = new SKPoint(bottomRight.X - triSize, bottomRight.Y);

            var path = new SKPath();
            path.MoveTo(bottomRight);
            path.LineTo(top);
            path.LineTo(left);
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

        private static SKPaint WidgetBackgroundPaint { get; } = new()
        {
            Color = SKColors.Black.WithAlpha(0xBE),
            StrokeWidth = 1,
            Style = SKPaintStyle.Fill
        };

        private static SKPaint TitleBarPaint { get; } = new()
        {
            Color = SKColors.Gray,
            StrokeWidth = 0.5f,
            Style = SKPaintStyle.Fill
        };

        protected virtual SKFont Font { get; } = new(CustomFonts.NeoSansStdRegular, 9f)
        {
            Subpixel = true
        };

        private static SKPaint ButtonBackgroundPaint { get; } = new()
        {
            Color = SKColors.LightGray,
            StrokeWidth = 0.1f,
            Style = SKPaintStyle.Fill
        };

        private static SKPaint SymbolPaint { get; } = new()
        {
            Color = SKColors.Black,
            StrokeWidth = 2f,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };

        private static SKPaint TitleBarText { get; } = new()
        {
            Color = SKColors.White,
            IsStroke = false,
            IsAntialias = true
        };

        public virtual void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, true))
                return;
            ResizeTriangle?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
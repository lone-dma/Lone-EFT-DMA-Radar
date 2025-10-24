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

using LoneArenaDmaRadar.Arena.GameWorld.Explosives;
using LoneArenaDmaRadar.Arena.GameWorld.Player;
using LoneArenaDmaRadar.UI.Radar.Maps;
using LoneArenaDmaRadar.UI.Radar.Views;
using LoneArenaDmaRadar.UI.Skia;
using SkiaSharp.Views.WPF;
using System.Windows.Controls;

namespace LoneArenaDmaRadar.UI.Radar.ViewModels
{
    public sealed class RadarViewModel
    {
        #region Static Interface

        /// <summary>
        /// Game has started and Radar is starting up...
        /// </summary>
        private static bool Starting => Memory?.Starting ?? false;

        /// <summary>
        /// Radar has found Escape From Tarkov process and is ready.
        /// </summary>
        private static bool Ready => Memory?.Ready ?? false;

        /// <summary>
        /// Radar has found Local Game World, and a Raid Instance is active.
        /// </summary>
        private static bool InRaid => Memory?.InRaid ?? false;

        /// <summary>
        /// Map Identifier of Current Map.
        /// </summary>
        private static string MapID
        {
            get
            {
                string id = Memory.MapID;
                id ??= "null";
                return id;
            }
        }

        /// <summary>
        /// LocalPlayer (who is running Radar) 'Player' object.
        /// </summary>
        private static LocalPlayer LocalPlayer => Memory?.LocalPlayer;

        /// <summary>
        /// All Players in Local Game World (including dead/exfil'd) 'Player' collection.
        /// </summary>
        private static IReadOnlyCollection<AbstractPlayer> AllPlayers => Memory?.Players;

        /// <summary>
        /// Contains all 'Hot' explosives in Local Game World, and their position(s).
        /// </summary>
        private static IReadOnlyCollection<IExplosiveItem> Explosives => Memory?.Explosives;

        /// <summary>
        /// Contains all 'mouse-overable' items.
        /// </summary>
        private static IEnumerable<IMouseoverEntity> MouseOverItems
        {
            get
            {
                var players = AllPlayers
                    .Where(x => x is not Arena.GameWorld.Player.LocalPlayer
                        && !x.HasExfild) ?? 
                        Enumerable.Empty<AbstractPlayer>();

                return players;
            }
        }

        #endregion

        #region Fields/Properties/Startup

        private readonly RadarTab _parent;
        private readonly PeriodicTimer _periodicTimer = new PeriodicTimer(period: TimeSpan.FromSeconds(1));
        private int _fps = 0;
        private bool _mouseDown;
        private IMouseoverEntity _mouseOverItem;
        private Vector2 _lastMousePosition;
        private Vector2 _mapPanPosition;

        /// <summary>
        /// Skia Radar Viewport.
        /// </summary>
        public SKGLElement Radar => _parent.Radar;

        public RadarViewModel(RadarTab parent)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            parent.Radar.MouseMove += Radar_MouseMove;
            parent.Radar.MouseDown += Radar_MouseDown;
            parent.Radar.MouseUp += Radar_MouseUp;
            parent.Radar.MouseLeave += Radar_MouseLeave;
            _ = OnStartupAsync();
            _ = RunPeriodicTimerAsync();
        }

        /// <summary>
        /// Complete Skia/GL Setup after GL Context is initialized.
        /// </summary>
        private async Task OnStartupAsync()
        {
            await _parent.Dispatcher.Invoke(async () =>
            {
                while (Radar.GRContext is null)
                    await Task.Delay(10);
                Radar.GRContext.SetResourceCacheLimit(512 * 1024 * 1024); // 512 MB

                Radar.PaintSurface += Radar_PaintSurface;
            });
        }

        #endregion

        #region Render Loop

        /// <summary>
        /// Main Render Loop for Radar.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Radar_PaintSurface(object sender, SKPaintGLSurfaceEventArgs e)
        {
            // Working vars
            var isStarting = Starting;
            var isReady = Ready;
            var inRaid = InRaid;
            var canvas = e.Surface.Canvas;
            // Begin draw
            try
            {
                Interlocked.Increment(ref _fps); // Increment FPS counter
                SetMapName();
                /// Check for map switch
                string mapID = MapID; // Cache ref
                if (!mapID.Equals(EftMapManager.Map?.ID, StringComparison.OrdinalIgnoreCase)) // Map changed
                {
                    EftMapManager.LoadMap(mapID);
                }
                canvas.Clear(); // Clear canvas
                if (inRaid && LocalPlayer is LocalPlayer localPlayer) // LocalPlayer is in a raid -> Begin Drawing...
                {
                    var map = EftMapManager.Map; // Cache ref
                    ArgumentNullException.ThrowIfNull(map, nameof(map));
                    var closestToMouse = _mouseOverItem; // cache ref
                    // Get LocalPlayer location
                    var localPlayerPos = localPlayer.Position;
                    var localPlayerMapPos = localPlayerPos.ToMapPos(map.Config);
                    if (MainWindow.Instance?.Radar?.MapSetupHelper?.ViewModel is MapSetupHelperViewModel mapSetup && mapSetup.IsVisible)
                    {
                        mapSetup.Coords = $"Unity X,Y,Z: {localPlayerPos.X},{localPlayerPos.Y},{localPlayerPos.Z}";
                    }
                    // Prepare to draw Game Map
                    EftMapParams mapParams; // Drawing Source
                    if (MainWindow.Instance?.Radar?.Overlay?.ViewModel?.IsMapFreeEnabled ?? false) // Map fixed location, click to pan map
                    {
                        if (_mapPanPosition == default)
                        {
                            _mapPanPosition = localPlayerMapPos;
                        }
                        mapParams = map.GetParameters(Radar, App.Config.UI.Zoom, ref _mapPanPosition);
                    }
                    else
                    {
                        _mapPanPosition = default;
                        mapParams = map.GetParameters(Radar, App.Config.UI.Zoom, ref localPlayerMapPos); // Map auto follow LocalPlayer
                    }
                    var info = e.RawInfo;
                    var mapCanvasBounds = new SKRect() // Drawing Destination
                    {
                        Left = info.Rect.Left,
                        Right = info.Rect.Right,
                        Top = info.Rect.Top,
                        Bottom = info.Rect.Bottom
                    };
                    // Draw Map
                    map.Draw(canvas, localPlayer.Position.Y, mapParams.Bounds, mapCanvasBounds);
                    // Draw other players
                    var allPlayers = AllPlayers?
                        .Where(x => !x.HasExfild); // Skip exfil'd players

                    if (Explosives is IReadOnlyCollection<IExplosiveItem> explosives) // Draw grenades
                    {
                        foreach (var explosive in explosives)
                        {
                            explosive.Draw(canvas, mapParams, localPlayer);
                        }
                    }

                    if (allPlayers is not null)
                    {
                        foreach (var player in allPlayers) // Draw PMCs
                        {
                            if (player == localPlayer)
                                continue; // Already drawn local player, move on
                            player.Draw(canvas, mapParams, localPlayer);
                        }
                    }

                    // Draw LocalPlayer over everything else
                    localPlayer.Draw(canvas, mapParams, localPlayer);
                    closestToMouse?.DrawMouseover(canvas, mapParams, localPlayer); // Mouseover Item
                }
                else // LocalPlayer is *not* in a Raid -> Display Reason
                {
                    if (!isStarting)
                        GameNotRunningStatus(canvas);
                    else if (isStarting && !isReady)
                        StartingUpStatus(canvas);
                    else if (!inRaid)
                        WaitingForMatchStatus(canvas);
                }
                canvas.Flush(); // commit frame to GPU
            }
            catch (Exception ex) // Log rendering errors
            {
                Debug.WriteLine($"***** CRITICAL RENDER ERROR: {ex}");
            }
        }

        #endregion

        #region Status Messages

        private int _statusOrder = 1; // Backing field dont use
        /// <summary>
        /// Status order for rotating status message animation.
        /// </summary>
        private int StatusOrder
        {
            get => _statusOrder;
            set
            {
                if (_statusOrder >= 3) // Reset status order to beginning
                {
                    _statusOrder = 1;
                }
                else // Increment
                {
                    _statusOrder++;
                }
            }
        }

        /// <summary>
        /// Display 'Game Process Not Running!' status message.
        /// </summary>
        /// <param name="canvas"></param>
        private static void GameNotRunningStatus(SKCanvas canvas)
        {
            const string notRunning = "Game Process Not Running!";
            var bounds = canvas.LocalClipBounds;
            float textWidth = SKFonts.UILarge.MeasureText(notRunning);
            canvas.DrawText(notRunning,
                (bounds.Width / 2) - textWidth / 2f, bounds.Height / 2,
                SKTextAlign.Left,
                SKFonts.UILarge,
                SKPaints.TextRadarStatus);
        }
        /// <summary>
        /// Display 'Starting Up...' status message.
        /// </summary>
        /// <param name="canvas"></param>
        private void StartingUpStatus(SKCanvas canvas)
        {
            const string startingUp1 = "Starting Up.";
            const string startingUp2 = "Starting Up..";
            const string startingUp3 = "Starting Up...";
            var bounds = canvas.LocalClipBounds;
            int order = StatusOrder;
            string status = order == 1 ?
                startingUp1 : order == 2 ?
                startingUp2 : startingUp3;
            float textWidth = SKFonts.UILarge.MeasureText(startingUp1);
            canvas.DrawText(status,
                (bounds.Width / 2) - textWidth / 2f, bounds.Height / 2,
                SKTextAlign.Left,
                SKFonts.UILarge,
                SKPaints.TextRadarStatus);
        }
        /// <summary>
        /// Display 'Waiting for Match Start...' status message.
        /// </summary>
        /// <param name="canvas"></param>
        private void WaitingForMatchStatus(SKCanvas canvas)
        {
            const string waitingFor1 = "Waiting for Match Start.";
            const string waitingFor2 = "Waiting for Match Start..";
            const string waitingFor3 = "Waiting for Match Start...";
            var bounds = canvas.LocalClipBounds;
            int order = StatusOrder;
            string status = order == 1 ?
                waitingFor1 : order == 2 ?
                waitingFor2 : waitingFor3;
            float textWidth = SKFonts.UILarge.MeasureText(waitingFor1);
            canvas.DrawText(status,
                (bounds.Width / 2) - textWidth / 2f, bounds.Height / 2,
                SKTextAlign.Left,
                SKFonts.UILarge,
                SKPaints.TextRadarStatus);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Purge SKResources to free up memory.
        /// </summary>
        public void PurgeSKResources()
        {
            _parent.Dispatcher.Invoke(() =>
            {
                Radar.GRContext?.PurgeResources();
            });
        }

        /// <summary>
        /// Set the Map Name on Radar Tab.
        /// </summary>
        private static void SetMapName()
        {
            string map = EftMapManager.Map?.Config?.Name;
            string name = map is null ? 
                "Radar" : $"Radar ({map})";
            if (MainWindow.Instance?.RadarTab is TabItem tab)
            {
                tab.Header = name;
            }
        }

        /// <summary>
        /// Set the FPS Counter.
        /// </summary>
        private async Task RunPeriodicTimerAsync()
        {
            while (await _periodicTimer.WaitForNextTickAsync())
            {
                // Increment status order
                StatusOrder++;
                // Parse FPS and set window title
                int fps = Interlocked.Exchange(ref _fps, 0); // Get FPS -> Reset FPS counter
                string title = $"{App.Name} ({fps} fps)";
                if (MainWindow.Instance is MainWindow mainWindow)
                {
                    mainWindow.Title = title; // Set new window title
                }
            }
        }

        /// <summary>
        /// Zooms the map 'in'.
        /// </summary>
        public void ZoomIn(int amt)
        {
            if (App.Config.UI.Zoom - amt >= 1)
            {
                App.Config.UI.Zoom -= amt;
            }
            else
            {
                App.Config.UI.Zoom = 1;
            }
        }

        /// <summary>
        /// Zooms the map 'out'.
        /// </summary>
        public void ZoomOut(int amt)
        {
            if (App.Config.UI.Zoom + amt <= 200)
            {
                App.Config.UI.Zoom += amt;
            }
            else
            {
                App.Config.UI.Zoom = 200;
            }
        }

        #endregion

        #region Event Handlers

        private void Radar_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _mouseDown = false;
        }

        private void Radar_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _mouseDown = false;
        }

        private void Radar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // get mouse pos relative to the Radar control
            var element = sender as IInputElement;
            var pt = e.GetPosition(element);
            var mouseX = (float)pt.X;
            var mouseY = (float)pt.Y;
            var mouse = new Vector2(mouseX, mouseY);
            if (e.LeftButton is System.Windows.Input.MouseButtonState.Pressed)
            {
                _lastMousePosition = mouse;
                _mouseDown = true;
                if (e.ClickCount >= 2 && _mouseOverItem is ObservedPlayer observed)
                {
                    if (InRaid && observed.IsStreaming)
                    {
                        Process.Start(new ProcessStartInfo()
                        {
                            FileName = observed.TwitchChannelURL,
                            UseShellExecute = true
                        });
                    }

                }
            }
            if (e.RightButton is System.Windows.Input.MouseButtonState.Pressed)
            {
                if (_mouseOverItem is AbstractPlayer player)
                {
                    player.IsFocused = !player.IsFocused;
                }
            }
        }

        private void Radar_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // get mouse pos relative to the Radar control
            var element = sender as IInputElement;
            var pt = e.GetPosition(element);
            var mouseX = (float)pt.X;
            var mouseY = (float)pt.Y;
            var mouse = new Vector2(mouseX, mouseY);

            if (_mouseDown && MainWindow.Instance?.Radar?.Overlay?.ViewModel is RadarOverlayViewModel vm && vm.IsMapFreeEnabled) // panning
            {
                var deltaX = -(mouseX - _lastMousePosition.X);
                var deltaY = -(mouseY - _lastMousePosition.Y);

                _mapPanPosition.X += (float)deltaX;
                _mapPanPosition.Y += (float)deltaY;
                _lastMousePosition = mouse;
            }
            else
            {
                if (!InRaid)
                {
                    ClearRefs();
                    return;
                }

                var items = MouseOverItems;
                if (items?.Any() != true)
                {
                    ClearRefs();
                    return;
                }

                // find closest
                var closest = items.Aggregate(
                    (x1, x2) => Vector2.Distance(x1.MouseoverPosition, mouse)
                             < Vector2.Distance(x2.MouseoverPosition, mouse)
                        ? x1 : x2);

                if (Vector2.Distance(closest.MouseoverPosition, mouse) >= 12)
                {
                    ClearRefs();
                    return;
                }

                switch (closest)
                {
                    case AbstractPlayer player:
                        _mouseOverItem = player;
                        break;

                    default:
                        ClearRefs();
                        break;
                }

                void ClearRefs()
                {
                    _mouseOverItem = null;
                }
            }
        }

        #endregion
    }
}

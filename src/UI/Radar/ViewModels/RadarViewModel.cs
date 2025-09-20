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

using Collections.Pooled;
using EftDmaRadarLite.Misc;
using EftDmaRadarLite.Tarkov.Data;
using EftDmaRadarLite.Tarkov.GameWorld.Exits;
using EftDmaRadarLite.Tarkov.GameWorld.Explosives;
using EftDmaRadarLite.Tarkov.Loot;
using EftDmaRadarLite.Tarkov.Player;
using EftDmaRadarLite.Tarkov.Quests;
using EftDmaRadarLite.UI.Loot;
using EftDmaRadarLite.UI.Radar.Views;
using EftDmaRadarLite.UI.Skia;
using EftDmaRadarLite.UI.Skia.Maps;
using SkiaSharp.Views.WPF;
using System.Windows.Controls;

namespace EftDmaRadarLite.UI.Radar.ViewModels
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
        /// All Filtered Loot on the map.
        /// </summary>
        private static IEnumerable<LootItem> Loot => Memory?.Loot?.FilteredLoot;

        /// <summary>
        /// All Static Containers on the map.
        /// </summary>
        private static IEnumerable<StaticLootContainer> Containers => Memory?.Loot?.StaticContainers;

        /// <summary>
        /// All Players in Local Game World (including dead/exfil'd) 'Player' collection.
        /// </summary>
        private static IReadOnlyCollection<PlayerBase> AllPlayers => Memory?.Players;

        /// <summary>
        /// Contains all 'Hot' explosives in Local Game World, and their position(s).
        /// </summary>
        private static IReadOnlyCollection<IExplosiveItem> Explosives => Memory?.Explosives;

        /// <summary>
        /// Contains all 'Exits' in Local Game World, and their status/position(s).
        /// </summary>
        private static IReadOnlyCollection<IExitPoint> Exits => Memory?.Exits;

        /// <summary>
        /// Item Search Filter has been set/applied.
        /// </summary>
        private static bool FilterIsSet =>
            !string.IsNullOrEmpty(LootFilter.SearchString);

        /// <summary>
        /// True if corpses are visible as loot.
        /// </summary>
        private static bool LootCorpsesVisible => (MainWindow.Instance?.Settings?.ViewModel?.ShowLoot ?? false) && !(MainWindow.Instance?.Radar?.Overlay?.ViewModel?.HideCorpses ?? false) && !FilterIsSet;

        /// <summary>
        /// Contains all 'mouse-overable' items.
        /// </summary>
        private static IEnumerable<IMouseoverEntity> MouseOverItems
        {
            get
            {
                var players = AllPlayers
                    .Where(x => x is not Tarkov.Player.LocalPlayer
                        && !x.HasExfild && (LootCorpsesVisible ? x.IsAlive : true)) ?? 
                        Enumerable.Empty<PlayerBase>();

                var loot = Loot ?? Enumerable.Empty<IMouseoverEntity>();
                var containers = Containers ?? Enumerable.Empty<IMouseoverEntity>();
                var exits = Exits ?? Enumerable.Empty<IMouseoverEntity>();
                var questZones = Memory.QuestManager?.LocationConditions?.Values ?? Enumerable.Empty<IMouseoverEntity>();

                if (FilterIsSet && !(MainWindow.Instance?.Radar?.Overlay?.ViewModel?.HideCorpses ?? false)) // Item Search
                    players = players.Where(x =>
                        x.LootObject is null || !loot.Contains(x.LootObject)); // Don't show both corpse objects

                var result = loot.Concat(containers).Concat(players).Concat(exits).Concat(questZones);
                return result.Any() ? result : null;
            }
        }

        /// <summary>
        /// Currently 'Moused Over' Group.
        /// </summary>
        public static int? MouseoverGroup { get; private set; }

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
        /// <summary>
        /// ESP Widget Viewport.
        /// </summary>
        public EspWidget ESPWidget { get; private set; }
        /// <summary>
        /// Player Info Widget Viewport.
        /// </summary>
        public PlayerInfoWidget InfoWidget { get; private set; }

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
                if (App.Config.EspWidget.Location == default)
                {
                    var size = Radar.CanvasSize;
                    var cr = new SKRect(0, 0, size.Width, size.Height);
                    App.Config.EspWidget.Location = new SKRect(cr.Left, cr.Bottom - 200, cr.Left + 200, cr.Bottom);
                }

                if (App.Config.InfoWidget.Location == default)
                {
                    var size = Radar.CanvasSize;
                    var cr = new SKRect(0, 0, size.Width, size.Height);
                    App.Config.InfoWidget.Location = new SKRect(cr.Right - 1, cr.Top, cr.Right, cr.Top + 1);
                }

                ESPWidget = new EspWidget(Radar, App.Config.EspWidget.Location, App.Config.EspWidget.Minimized,
                    App.Config.UI.UIScale);
                InfoWidget = new PlayerInfoWidget(Radar, App.Config.InfoWidget.Location,
                    App.Config.InfoWidget.Minimized, App.Config.UI.UIScale);
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
                    // Draw LocalPlayer
                    localPlayer.Draw(canvas, mapParams, localPlayer);
                    // Draw other players
                    var allPlayers = AllPlayers?
                        .Where(x => !x.HasExfild); // Skip exfil'd players
                    if (App.Config.Loot.Enabled) // Draw loot (if enabled)
                    {
                        if (Loot?.Reverse() is IEnumerable<LootItem> loot) // Draw important loot last (on top)
                        {
                            foreach (var item in loot)
                            {
                                if (App.Config.Loot.HideCorpses && item is LootCorpse)
                                    continue;
                                item.Draw(canvas, mapParams, localPlayer);
                            }
                        }
                        if (App.Config.Containers.Enabled) // Draw Containers
                        {
                            if (Containers is IEnumerable<StaticLootContainer> containers && 
                                MainWindow.Instance?.Settings?.ViewModel is SettingsViewModel vm)
                            {
                                foreach (var container in containers)
                                {
                                    if (vm.ContainerIsTracked(container.ID ?? "NULL"))
                                    {
                                        if (App.Config.Containers.HideSearched && container.Searched)
                                            continue;
                                        container.Draw(canvas, mapParams, localPlayer);
                                    }
                                }
                            }
                        }
                    }

                    if (App.Config.QuestHelper.Enabled)
                    {
                        if (Loot?.OfType<QuestItem>() is IEnumerable<QuestItem> questItems)
                        {
                            foreach (var item in questItems)
                            {
                                item.Draw(canvas, mapParams, localPlayer);
                            }
                        }
                        if (Memory.QuestManager?.LocationConditions?.Values is IEnumerable<QuestLocation> questLocations)
                        {
                            foreach (var loc in questLocations)
                            {
                                loc.Draw(canvas, mapParams, localPlayer);
                            }
                        }
                    }

                    if (App.Config.UI.ShowMines &&
                        StaticGameData.Mines.TryGetValue(mapID, out var mines)) // Draw Mines
                    {
                        foreach (ref var mine in mines.Span)
                        {
                            var mineZoomedPos = mine.ToMapPos(map.Config).ToZoomedPos(mapParams);
                            mineZoomedPos.DrawMineMarker(canvas);
                        }
                    }

                    if (Explosives is IReadOnlyCollection<IExplosiveItem> explosives) // Draw grenades
                    {
                        foreach (var explosive in explosives)
                        {
                            explosive.Draw(canvas, mapParams, localPlayer);
                        }
                    }

                    if (Exits is IReadOnlyCollection<IExitPoint> exits)
                    {
                        foreach (var exit in exits)
                        {
                            if (exit is Exfil exfil && !localPlayer.IsPmc && exfil.Status is Exfil.EStatus.Closed)
                                continue;
                            exit.Draw(canvas, mapParams, localPlayer);
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
                    if (App.Config.UI.ConnectGroups) // Connect Groups together
                    {
                        var groupedPlayers = allPlayers?
                            .Where(x => x.IsHumanHostileActive && x.GroupID != -1);
                        if (groupedPlayers is not null)
                        {
                            using var groups = groupedPlayers.Select(x => x.GroupID).ToPooledSet();
                            foreach (var grp in groups)
                            {
                                var grpMembers = groupedPlayers.Where(x => x.GroupID == grp);
                                if (grpMembers is not null && grpMembers.Any())
                                {
                                    var combinations = grpMembers
                                        .SelectMany(x => grpMembers, (x, y) =>
                                            Tuple.Create(
                                                x.Position.ToMapPos(map.Config).ToZoomedPos(mapParams),
                                                y.Position.ToMapPos(map.Config).ToZoomedPos(mapParams)));
                                    foreach (var pair in combinations)
                                    {
                                        canvas.DrawLine(pair.Item1.X, pair.Item1.Y, pair.Item2.X, pair.Item2.Y, SKPaints.PaintConnectorGroup);
                                    }
                                }
                            }
                        }
                    }

                    if (allPlayers is not null && App.Config.InfoWidget.Enabled) // Players Overlay
                    {
                        InfoWidget?.Draw(canvas, localPlayer, allPlayers);
                    }
                    closestToMouse?.DrawMouseover(canvas, mapParams, localPlayer); // Mouseover Item
                    if (App.Config.EspWidget.Enabled) // ESP Widget
                    {
                        ESPWidget?.Draw(canvas);
                    }
                }
                else // LocalPlayer is *not* in a Raid -> Display Reason
                {
                    if (!isStarting)
                        GameNotRunningStatus(canvas);
                    else if (isStarting && !isReady)
                        StartingUpStatus(canvas);
                    else if (!inRaid)
                        WaitingForRaidStatus(canvas);
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
        /// Display 'Waiting for Raid Start...' status message.
        /// </summary>
        /// <param name="canvas"></param>
        private void WaitingForRaidStatus(SKCanvas canvas)
        {
            const string waitingFor1 = "Waiting for Raid Start.";
            const string waitingFor2 = "Waiting for Raid Start..";
            const string waitingFor3 = "Waiting for Raid Start...";
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
                if (_mouseOverItem is PlayerBase player)
                {
                    player.IsFocused = !player.IsFocused;
                }
            }
            if (MainWindow.Instance?.Radar?.Overlay?.ViewModel is RadarOverlayViewModel vm && vm.IsLootOverlayVisible)
            {
                vm.IsLootOverlayVisible = false; // Hide Loot Overlay on Mouse Down
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
                    case PlayerBase player:
                        _mouseOverItem = player;
                        MouseoverGroup = (player.IsHumanHostile && player.GroupID != -1)
                            ? player.GroupID
                            : (int?)null;
                        break;

                    case LootCorpse corpseObj:
                        _mouseOverItem = corpseObj;
                        var corpse = corpseObj.Player;
                        MouseoverGroup = (corpse?.IsHumanHostile == true && corpse.GroupID != -1)
                            ? corpse.GroupID
                            : (int?)null;
                        break;

                    case LootContainer ctr:
                        _mouseOverItem = ctr;
                        MouseoverGroup = null;
                        break;

                    case IExitPoint exit:
                    case QuestLocation quest:
                        _mouseOverItem = closest;
                        MouseoverGroup = null;
                        break;

                    default:
                        ClearRefs();
                        break;
                }

                void ClearRefs()
                {
                    _mouseOverItem = null;
                    MouseoverGroup = null;
                }
            }
        }

        #endregion
    }
}

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

using Collections.Pooled;
using ImGuiNET;
using LoneEftDmaRadar.Tarkov.GameWorld.Exits;
using LoneEftDmaRadar.Tarkov.GameWorld.Explosives;
using LoneEftDmaRadar.Tarkov.GameWorld.Hazards;
using LoneEftDmaRadar.Tarkov.GameWorld.Loot;
using LoneEftDmaRadar.Tarkov.GameWorld.Player;
using LoneEftDmaRadar.Tarkov.GameWorld.Quests;
using LoneEftDmaRadar.UI.ColorPicker;
using LoneEftDmaRadar.UI.Hotkeys;
using LoneEftDmaRadar.UI.Hotkeys.Internal;
using LoneEftDmaRadar.UI.Loot;
using LoneEftDmaRadar.UI.Maps;
using LoneEftDmaRadar.UI.Panels;
using LoneEftDmaRadar.UI.Skia;
using LoneEftDmaRadar.UI.Widgets;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace LoneEftDmaRadar.UI
{
    internal static partial class RadarWindow
    {
        #region Fields

        private static IWindow _window = null!;
        private static GL _gl = null!;
        private static IInputContext _input = null!;

        private static ImGuiController _imgui = null!;
        private static SKSurface _skSurface = null!;
        private static GRContext _grContext = null!;
        private static volatile bool _purgeSkResources = false;

        private static readonly PeriodicTimer _fpsTimer = new(TimeSpan.FromSeconds(1));
        private static int _fpsCounter = 0;
        private static int _statusOrder = 1;

        // Mouse tracking
        private static bool _mouseDown;
        private static Vector2 _lastMousePosition;
        private static IMouseoverEntity _mouseOverItem;

        // UI State (moved from RadarUIState)
        private static int _fps;
        private static bool _isLootFiltersOpen;
        private static bool _isWatchlistOpen;
        private static bool _isHistoryOpen;
        private static bool _isWebRadarOpen;
        private static bool _isMapFreeEnabled;
        private static Vector2 _mapPanPosition;

        #endregion

        #region Static Properties

        public static IntPtr Handle => _window?.Native?.Win32?.Hwnd ?? IntPtr.Zero;
        private static bool Starting => Memory.Starting;
        private static bool Ready => Memory.Ready;
        private static bool InRaid => Memory.InRaid;
        private static string MapID => Memory.MapID ?? "null";
        private static LocalPlayer LocalPlayer => Memory.LocalPlayer;
        private static IEnumerable<LootItem> FilteredLoot => Memory.Loot?.FilteredLoot;
        private static IEnumerable<StaticLootContainer> Containers => Memory.Loot?.StaticContainers;
        private static IReadOnlyCollection<AbstractPlayer> AllPlayers => Memory.Players;
        private static IReadOnlyCollection<IExplosiveItem> Explosives => Memory.Explosives;
        private static IReadOnlyCollection<IWorldHazard> Hazards => Memory.Game?.Hazards;
        private static IReadOnlyCollection<IExitPoint> Exits => Memory.Exits;
        private static QuestManager Quests => Memory.QuestManager;
        private static bool SearchFilterIsSet => !string.IsNullOrEmpty(LootFilter.SearchString);
        private static bool LootCorpsesVisible => Program.Config.Loot.Enabled && !Program.Config.Loot.HideCorpses && !SearchFilterIsSet;

        /// <summary>
        /// Currently 'Moused Over' Group.
        /// </summary>
        public static int? MouseoverGroup { get; private set; }

        /// <summary>
        /// Whether map free mode is enabled.
        /// </summary>
        public static bool IsMapFreeEnabled
        {
            get => _isMapFreeEnabled;
            set => _isMapFreeEnabled = value;
        }

        /// <summary>
        /// Map pan position when in free mode.
        /// </summary>
        public static Vector2 MapPanPosition
        {
            get => _mapPanPosition;
            set => _mapPanPosition = value;
        }

        #endregion

        #region Initialization

        internal static void Initialize()
        {
            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(
                (int)Program.Config.UI.WindowSize.Width,
                (int)Program.Config.UI.WindowSize.Height);
            options.Title = Program.Name;
            options.VSync = true;

            // Restore maximized state from config (not fullscreen)
            if (Program.Config.UI.WindowMaximized)
            {
                options.WindowState = WindowState.Maximized;
            }

            _window = Window.Create(options);

            _window.Load += OnLoad;
            _window.Render += OnRender;
            _window.Resize += OnResize;
            _window.Closing += OnClosing;
            _window.StateChanged += OnStateChanged;

            // Start FPS timer
            _ = RunFpsTimerAsync();

            _window.Run();
        }

        private static void OnLoad()
        {
            _gl = GL.GetApi(_window);

            // Apply dark mode and window icon (Windows only)
            if (_window.Native?.Win32 is { } win32)
            {
                EnableDarkMode(win32.Hwnd);
                SetWindowIcon(win32.Hwnd);
            }

            // Create input context FIRST (before ImGuiController to share it)
            _input = _window.CreateInput();

            // --- Skia GPU context ---
            var glInterface = GRGlInterface.Create(name =>
                _window.GLContext!.TryGetProcAddress(name, out var addr) ? addr : 0);

            _grContext = GRContext.CreateGl(glInterface);
            _grContext.SetResourceCacheLimit(512 * 1024 * 1024); // 512 MB

            CreateSkiaSurface();

            // --- ImGui ---
            ImGui.CreateContext();

            // Pass the existing input context to ImGuiController to share it
            _imgui = new ImGuiController(
                _gl,
                _window,
                _window.Size.X,
                _window.Size.Y,
                _input  // Share the input context
            );

            // Set IniFilename AFTER context and controller are created, then load settings
            unsafe
            {
                string path = Path.Combine(Program.ConfigPath.FullName, "imgui.ini");
                ImGuiNET.ImGuiNative.igGetIO()->IniFilename = (byte*)Marshal.StringToHGlobalAnsi(path);
                
                // Explicitly load the ini file if it exists
                if (File.Exists(path))
                {
                    ImGui.LoadIniSettingsFromDisk(path);
                }
            }

            // Configure style AFTER ImGuiController is fully initialized
            // to prevent flicker from font texture recreation
            ImGui.StyleColorsDark();

            // Setup mouse events on the shared input context
            foreach (var mouse in _input.Mice)
            {
                mouse.MouseDown += OnMouseDown;
                mouse.MouseUp += OnMouseUp;
                mouse.MouseMove += OnMouseMove;
                mouse.Scroll += OnMouseScroll;
            }

            // Setup local keyboard hotkeys
            foreach (var keyboard in _input.Keyboards)
            {
                keyboard.KeyDown += OnKeyDown;
            }

            // Register hotkey action controllers (for remote DMA hotkeys)
            RegisterHotkeyControllers();

            // Initialize UI panels
            ColorPickerPanel.Initialize();
            SettingsPanel.Initialize();
            LootFiltersPanel.Initialize();
            PlayerWatchlistPanel.Initialize();

            // Initialize widgets
            InitializeWidgets();
        }

        private static void InitializeWidgets()
        {
            AimviewWidget.Initialize(_gl, _grContext);
        }

        private static void CreateSkiaSurface()
        {
            _skSurface?.Dispose();

            var fbInfo = new GRGlFramebufferInfo(
                0, // default framebuffer
                (uint)InternalFormat.Rgba8
            );

            var backendRenderTarget = new GRBackendRenderTarget(
                _window.Size.X,
                _window.Size.Y,
                0,
                0,
                fbInfo
            );

            _skSurface = SKSurface.Create(
                _grContext,
                backendRenderTarget,
                GRSurfaceOrigin.BottomLeft,
                SKColorType.Rgba8888
            );
        }

        private static void OnResize(Vector2D<int> size)
        {
            _gl.Viewport(size);
            CreateSkiaSurface();
            _imgui.WindowResized(size.X, size.Y);
        }

        private static void OnStateChanged(WindowState state)
        {
            // Track maximized state for persistence
            // Note: Fullscreen (hidden border + maximized) is NOT persisted - only regular maximized
            if (_window.WindowBorder == WindowBorder.Resizable)
            {
                Program.Config.UI.WindowMaximized = (state == WindowState.Maximized);
            }
        }

        private static void OnClosing()
        {
            // Save window state - only save size if not maximized/fullscreen
            if (_window.WindowState == WindowState.Normal)
            {
                Program.Config.UI.WindowSize = new SKSize(_window.Size.X, _window.Size.Y);
            }

            // Only save maximized if it's regular maximized (not fullscreen)
            Program.Config.UI.WindowMaximized = (_window.WindowState == WindowState.Maximized &&
                                                  _window.WindowBorder == WindowBorder.Resizable);

            Program.Config.Save();
        }

        #endregion

        #region Render Loop

        private static void OnRender(double delta)
        {
            try
            {
                Interlocked.Increment(ref _fpsCounter);
                if (Interlocked.Exchange(ref _purgeSkResources, false) == true)
                {
                    _grContext.PurgeResources();
                }

                // --- SCENE RENDER (Skia) ---
                // Render Skia FIRST, before ImGui.NewFrame() is called
                // This prevents Skia's GL flush from corrupting ImGui's texture state
                var canvas = _skSurface.Canvas;
                canvas.Clear(SKColors.Black);
                DrawRadarScene(canvas);
                canvas.Flush();
                _grContext.Flush();

                // Render AimviewWidget to its FBO (during Skia phase, before ImGui)
                AimviewWidget.RenderToFbo();

                // Restore viewport to full window after FBO rendering
                _gl.Viewport(0, 0, (uint)_window.Size.X, (uint)_window.Size.Y);
                _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

                // --- UI RENDER (ImGui) ---
                // Now start ImGui frame AFTER Skia has flushed
                _imgui.Update((float)delta);
                DrawImGuiUI();
                _imgui.Render();
            }
            catch (Exception ex)
            {
                Logging.WriteLine($"***** CRITICAL RENDER ERROR: {ex}");
            }
        }

        private static void DrawRadarScene(SKCanvas canvas)
        {
            var isStarting = Starting;
            var isReady = Ready;
            var inRaid = InRaid;

            if (inRaid && LocalPlayer is LocalPlayer localPlayer && EftMapManager.LoadMap(MapID) is IEftMap map)
            {
                DrawInRaidRadar(canvas, localPlayer, map);
            }
            else
            {
                DrawStatusMessage(canvas, isStarting, isReady);
            }
        }

        private static void DrawInRaidRadar(SKCanvas canvas, LocalPlayer localPlayer, IEftMap map)
        {
            var closestToMouse = _mouseOverItem;
            var localPlayerPos = localPlayer.Position;
            var localPlayerMapPos = localPlayerPos.ToMapPos(map.Config);

            // Update map setup helper coords
            if (MapSetupHelperPanel.ShowOverlay || MapSetupHelperPanel.IsOpen)
            {
                MapSetupHelperPanel.Coords = $"Unity X,Y,Z: {localPlayerPos.X:F1},{localPlayerPos.Y:F1},{localPlayerPos.Z:F1}";
            }

            // Get map parameters
            EftMapParams mapParams;
            var canvasSize = new SKSize(_window.Size.X, _window.Size.Y);

            if (_isMapFreeEnabled)
            {
                if (_mapPanPosition == default)
                {
                    _mapPanPosition = localPlayerMapPos;
                }
                var panPos = _mapPanPosition;
                mapParams = map.GetParameters(canvasSize, Program.Config.UI.Zoom, ref panPos);
                _mapPanPosition = panPos;
            }
            else
            {
                _mapPanPosition = default;
                mapParams = map.GetParameters(canvasSize, Program.Config.UI.Zoom, ref localPlayerMapPos);
            }

            var mapCanvasBounds = new SKRect(0, 0, canvasSize.Width, canvasSize.Height);

            // Draw Map
            map.Draw(canvas, localPlayer.Position.Y, mapParams.Bounds, mapCanvasBounds);

            // Draw loot
            if (Program.Config.Loot.Enabled)
            {
                if (FilteredLoot is IEnumerable<LootItem> loot)
                {
                    foreach (var item in loot)
                    {
                        item.Draw(canvas, mapParams, localPlayer);
                    }
                }

                if (Program.Config.Containers.Enabled && Containers is IEnumerable<StaticLootContainer> containers)
                {
                    foreach (var container in containers)
                    {
                        if (Program.Config.Containers.Selected.ContainsKey(container.ID ?? "NULL"))
                        {
                            container.Draw(canvas, mapParams, localPlayer);
                        }
                    }
                }
            }

            // Draw hazards
            if (Program.Config.UI.ShowHazards && Hazards is IReadOnlyCollection<IWorldHazard> hazards)
            {
                foreach (var hazard in hazards)
                {
                    hazard.Draw(canvas, mapParams, localPlayer);
                }
            }

            // Draw explosives
            if (Explosives is IReadOnlyCollection<IExplosiveItem> explosives)
            {
                foreach (var explosive in explosives)
                {
                    explosive.Draw(canvas, mapParams, localPlayer);
                }
            }

            // Draw exits
            if (Program.Config.UI.ShowExfils && Exits is IReadOnlyCollection<IExitPoint> exits)
            {
                foreach (var exit in exits)
                {
                    exit.Draw(canvas, mapParams, localPlayer);
                }
            }

            // Draw quest locations
            if (Program.Config.QuestHelper.Enabled && Quests?.LocationConditions?.Values is IEnumerable<QuestLocation> questLocations)
            {
                foreach (var loc in questLocations)
                {
                    loc.Draw(canvas, mapParams, localPlayer);
                }
            }

            // Draw players
            var allPlayers = AllPlayers?.Where(x => !x.HasExfild);
            if (allPlayers is not null)
            {
                foreach (var player in allPlayers)
                {
                    if (player == localPlayer)
                        continue;
                    player.Draw(canvas, mapParams, localPlayer);
                }
            }

            // Draw group connectors
            if (Program.Config.UI.ConnectGroups && allPlayers is not null)
            {
                DrawGroupConnectors(canvas, allPlayers, map, mapParams);
            }

            // Draw local player on top
            localPlayer.Draw(canvas, mapParams, localPlayer);

            // Draw mouseover
            closestToMouse?.DrawMouseover(canvas, mapParams, localPlayer);
        }

        private static void DrawGroupConnectors(SKCanvas canvas, IEnumerable<AbstractPlayer> allPlayers, IEftMap map, EftMapParams mapParams)
        {
            using var groupedByGrp = new PooledDictionary<int, PooledList<SKPoint>>(capacity: 16);
            try
            {
                foreach (var player in allPlayers)
                {
                    if (player.IsHumanHostileActive && player.GroupID != -1)
                    {
                        if (!groupedByGrp.TryGetValue(player.GroupID, out var list))
                        {
                            list = new PooledList<SKPoint>(capacity: 5);
                            groupedByGrp[player.GroupID] = list;
                        }
                        list.Add(player.Position.ToMapPos(map.Config).ToZoomedPos(mapParams));
                    }
                }

                foreach (var grp in groupedByGrp.Values)
                {
                    for (int i = 0; i < grp.Count; i++)
                    {
                        for (int j = i + 1; j < grp.Count; j++)
                        {
                            canvas.DrawLine(grp[i].X, grp[i].Y, grp[j].X, grp[j].Y, SKPaints.PaintConnectorGroup);
                        }
                    }
                }
            }
            finally
            {
                foreach (var list in groupedByGrp.Values)
                    list.Dispose();
            }
        }

        private static void DrawStatusMessage(SKCanvas canvas, bool isStarting, bool isReady)
        {
            var bounds = new SKRect(0, 0, _window.Size.X, _window.Size.Y);

            // Base text (no trailing dots) and how many dots to draw
            string baseText;
            int dotCount;

            if (!isStarting)
            {
                baseText = "Game Process Not Running!";
                dotCount = 0;
            }
            else if (isStarting && !isReady)
            {
                baseText = "Starting Up";
                dotCount = _statusOrder; // 1..3
            }
            else
            {
                baseText = "Waiting for Raid Start";
                dotCount = _statusOrder; // 1..3
            }

            // Ensure dotCount never exceeds three
            dotCount = Math.Clamp(dotCount, 0, 3);

            // Measure widths: reserve space for the base text + up to 3 dots
            float baseWidth = SKFonts.UILarge.MeasureText(baseText);
            float dotWidth = SKFonts.UILarge.MeasureText(".");
            float totalMaxWidth = baseWidth + (dotWidth * 3);

            // Position the whole block centered, draw base text left-aligned at startX
            float startX = (bounds.Width / 2f) - (totalMaxWidth / 2f);
            float y = bounds.Height / 2f;

            canvas.DrawText(baseText,
                startX, y,
                SKTextAlign.Left,
                SKFonts.UILarge,
                SKPaints.TextRadarStatus);

            // Draw trailing dots to the right of the base text (they grow to the right only)
            if (dotCount > 0)
            {
                var dots = new string('.', dotCount);
                float dotsX = startX + baseWidth;
                canvas.DrawText(dots,
                    dotsX, y,
                    SKTextAlign.Left,
                    SKFonts.UILarge,
                    SKPaints.TextRadarStatus);
            }
        }

        private static void DrawImGuiUI()
        {
            // Draw overlay controls
            RadarOverlayPanel.DrawTopBar();
            RadarOverlayPanel.DrawLootOverlay();
            RadarOverlayPanel.DrawMapSetupHelper();

            // Draw main menu bar
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.MenuItem("Settings", null, SettingsPanel.IsOpen))
                {
                    SettingsPanel.IsOpen = !SettingsPanel.IsOpen;
                }

                ImGui.Separator();

                if (ImGui.MenuItem("Web Radar", null, _isWebRadarOpen))
                {
                    _isWebRadarOpen = !_isWebRadarOpen;
                }
                if (ImGui.MenuItem("History", null, _isHistoryOpen))
                {
                    _isHistoryOpen = !_isHistoryOpen;
                }
                if (ImGui.MenuItem("Watchlist", null, _isWatchlistOpen))
                {
                    _isWatchlistOpen = !_isWatchlistOpen;
                }
                if (ImGui.MenuItem("Loot Filters", null, _isLootFiltersOpen))
                {
                    _isLootFiltersOpen = !_isLootFiltersOpen;
                }

                // Display current map and FPS on the right
                string mapName = EftMapManager.Map?.Config?.Name ?? "No Map";
                string rightText = $"{mapName} | {_fps} FPS";
                float rightTextWidth = ImGui.CalcTextSize(rightText).X;
                ImGui.SetCursorPosX(ImGui.GetWindowWidth() - rightTextWidth - 10);
                ImGui.Text(rightText);

                ImGui.EndMainMenuBar();
            }

            // Draw windows
            DrawWindows();
        }

        private static void DrawWindows()
        {
            // Settings Panel
            if (SettingsPanel.IsOpen)
            {
                SettingsPanel.Draw();
            }

            // Loot Filters Window
            if (_isLootFiltersOpen)
            {
                DrawLootFiltersWindow();
            }

            // Watchlist Window
            if (_isWatchlistOpen)
            {
                DrawWatchlistWindow();
            }

            // History Window
            if (_isHistoryOpen)
            {
                DrawHistoryWindow();
            }

            // Web Radar Window
            if (_isWebRadarOpen)
            {
                DrawWebRadarWindow();
            }

            // Color Picker
            if (ColorPickerPanel.IsOpen)
            {
                ColorPickerPanel.Draw();
            }

            // Hotkey Manager
            if (HotkeyManagerPanel.IsOpen)
            {
                HotkeyManagerPanel.Draw();
            }

            // Map Setup Helper
            if (MapSetupHelperPanel.IsOpen)
            {
                MapSetupHelperPanel.Draw();
            }

            // Aimview Widget
            if (AimviewWidget.IsOpen && InRaid)
            {
                AimviewWidget.Draw();
            }

            // Player Info Widget
            if (PlayerInfoWidget.IsOpen && InRaid)
            {
                PlayerInfoWidget.Draw();
            }
        }

        private static void DrawLootFiltersWindow()
        {
            bool isOpen = _isLootFiltersOpen;
            ImGui.SetNextWindowSize(new Vector2(650, 550), ImGuiCond.FirstUseEver);
            if (ImGui.Begin("Loot Filters", ref isOpen))
            {
                LootFiltersPanel.Draw();
                ImGui.Separator();
                if (ImGui.Button("Apply & Close"))
                {
                    LootFiltersPanel.RefreshLootFilter();
                    isOpen = false;
                }
            }
            ImGui.End();
            _isLootFiltersOpen = isOpen;
        }

        private static void DrawWatchlistWindow()
        {
            bool isOpen = _isWatchlistOpen;
            ImGui.SetNextWindowSize(new Vector2(600, 500), ImGuiCond.FirstUseEver);
            if (ImGui.Begin("Player Watchlist", ref isOpen))
            {
                PlayerWatchlistPanel.Draw();
            }
            ImGui.End();
            _isWatchlistOpen = isOpen;
        }

        private static void DrawHistoryWindow()
        {
            bool isOpen = _isHistoryOpen;
            ImGui.SetNextWindowSize(new Vector2(900, 400), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(800, 300), new Vector2(float.MaxValue, float.MaxValue));
            if (ImGui.Begin("Player History", ref isOpen))
            {
                PlayerHistoryPanel.Draw();
            }
            ImGui.End();
            _isHistoryOpen = isOpen;
        }

        private static void DrawWebRadarWindow()
        {
            bool isOpen = _isWebRadarOpen;
            ImGui.SetNextWindowSize(new Vector2(450, 350), ImGuiCond.FirstUseEver);
            if (ImGui.Begin("Web Radar", ref isOpen))
            {
                WebRadarPanel.Draw();
            }
            ImGui.End();
            _isWebRadarOpen = isOpen;
        }

        #endregion

        #region Input Handling

        private static void OnMouseDown(IMouse mouse, MouseButton button)
        {
            // Let ImGui handle mouse if it wants to
            if (ImGui.GetIO().WantCaptureMouse)
                return;

            var pos = mouse.Position;
            var mousePos = new Vector2(pos.X, pos.Y);

            if (button == MouseButton.Left)
            {
                _lastMousePosition = mousePos;
                _mouseDown = true;
            }
            else if (button == MouseButton.Right)
            {
                if (_mouseOverItem is AbstractPlayer player)
                {
                    player.IsFocused = !player.IsFocused;
                }
            }
        }

        private static void OnMouseUp(IMouse mouse, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                _mouseDown = false;
            }
        }

        private static readonly RateLimiter _mouseMoveRL = new(TimeSpan.FromSeconds(1d / 60));
        private static void OnMouseMove(IMouse mouse, Vector2 position)
        {
            if (!_mouseMoveRL.TryEnter())
                return;

            // Let ImGui handle mouse if it wants to
            if (ImGui.GetIO().WantCaptureMouse)
            {
                _mouseDown = false; // Reset drag state when ImGui captures
                return;
            }

            var mousePos = new Vector2(position.X, position.Y);

            if (_mouseDown && _isMapFreeEnabled)
            {
                var deltaX = -(mousePos.X - _lastMousePosition.X);
                var deltaY = -(mousePos.Y - _lastMousePosition.Y);

                _mapPanPosition = new Vector2(
                    _mapPanPosition.X + deltaX,
                    _mapPanPosition.Y + deltaY);
                _lastMousePosition = mousePos;
            }
            else
            {
                ProcessMouseoverData(mousePos);
            }
        }

        private static void OnMouseScroll(IMouse mouse, ScrollWheel wheel)
        {
            // Zoom with scroll wheel (when not over ImGui)
            if (!ImGui.GetIO().WantCaptureMouse)
            {
                int delta = (int)(wheel.Y * 5);
                int newZoom = Program.Config.UI.Zoom - delta;
                Program.Config.UI.Zoom = Math.Clamp(newZoom, 1, 200);
            }
        }

        private static void ProcessMouseoverData(Vector2 mousePos)
        {
            if (!InRaid)
            {
                ClearMouseoverRefs();
                return;
            }

            var items = GetMouseoverItems();
            if (items is null)
            {
                ClearMouseoverRefs();
                return;
            }

            IMouseoverEntity closest = null;
            float bestDistSq = float.MaxValue;
            foreach (var it in items)
            {
                var d = Vector2.DistanceSquared(it.MouseoverPosition, mousePos);
                if (d < bestDistSq)
                {
                    bestDistSq = d;
                    closest = it;
                }
            }

            const float hoverThreshold = 144f; // 12 squared
            if (closest is null || bestDistSq >= hoverThreshold)
            {
                ClearMouseoverRefs();
                return;
            }

            switch (closest)
            {
                case AbstractPlayer player:
                    _mouseOverItem = player;
                    MouseoverGroup = (player.IsHumanHostile && player.GroupID != -1) ? player.GroupID : null;
                    if (LootCorpsesVisible && player.LootObject is LootCorpse playerCorpse)
                    {
                        _mouseOverItem = playerCorpse;
                    }
                    break;

                case LootCorpse corpseObj:
                    _mouseOverItem = corpseObj;
                    var corpse = corpseObj.Player;
                    MouseoverGroup = (corpse?.IsHumanHostile == true && corpse.GroupID != -1) ? corpse.GroupID : null;
                    break;

                case LootItem loot:
                    _mouseOverItem = loot;
                    MouseoverGroup = null;
                    break;

                case IExitPoint:
                case QuestLocation:
                case IWorldHazard:
                    _mouseOverItem = closest;
                    MouseoverGroup = null;
                    break;

                default:
                    ClearMouseoverRefs();
                    break;
            }
        }

        private static void ClearMouseoverRefs()
        {
            _mouseOverItem = null;
            MouseoverGroup = null;
        }

        private static IEnumerable<IMouseoverEntity> GetMouseoverItems()
        {
            var players = AllPlayers?
                .Where(x => x is not LoneEftDmaRadar.Tarkov.GameWorld.Player.LocalPlayer && !x.HasExfild && (!LootCorpsesVisible || x.IsAlive)) ??
                Enumerable.Empty<AbstractPlayer>();

            var loot = Program.Config.Loot.Enabled ?
                FilteredLoot ?? Enumerable.Empty<IMouseoverEntity>() : Enumerable.Empty<IMouseoverEntity>();
            var containers = Program.Config.Loot.Enabled && Program.Config.Containers.Enabled ?
                Containers ?? Enumerable.Empty<IMouseoverEntity>() : Enumerable.Empty<IMouseoverEntity>();
            var exits = Program.Config.UI.ShowExfils ?
                Exits ?? Enumerable.Empty<IMouseoverEntity>() : Enumerable.Empty<IMouseoverEntity>();
            var quests = Program.Config.QuestHelper.Enabled ?
                Quests?.LocationConditions?.Values?.OfType<IMouseoverEntity>() ?? Enumerable.Empty<IMouseoverEntity>()
                : Enumerable.Empty<IMouseoverEntity>();
            var hazards = Program.Config.UI.ShowHazards ?
                Hazards ?? Enumerable.Empty<IMouseoverEntity>()
                : Enumerable.Empty<IMouseoverEntity>();

            if (SearchFilterIsSet)
                players = players.Where(x => x.LootObject is null || !loot.Contains(x.LootObject));

            var result = loot.Concat(containers).Concat(players).Concat(exits).Concat(quests).Concat(hazards);

            using var enumerator = result.GetEnumerator();
            if (!enumerator.MoveNext())
                return null;

            return result;
        }

        #endregion

        #region Helpers

        private const int HK_ZOOMTICKAMT = 5;
        private const int HK_ZOOMTICKDELAY = 120;

        /// <summary>
        /// Handles local keyboard hotkeys for the window.
        /// </summary>
        private static void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
        {
            // Don't process hotkeys if ImGui wants keyboard input (e.g., typing in a text field)
            if (ImGui.GetIO().WantCaptureKeyboard)
                return;

            switch (key)
            {
                case Key.F1:
                    ZoomIn(HK_ZOOMTICKAMT);
                    break;
                case Key.F2:
                    ZoomOut(HK_ZOOMTICKAMT);
                    break;
                case Key.F3:
                    Program.Config.Loot.Enabled = !Program.Config.Loot.Enabled;
                    break;
            }
        }

        /// <summary>
        /// Registers all hotkey action controllers using reflection.
        /// Finds methods decorated with [Hotkey] attribute and registers them.
        /// </summary>
        private static void RegisterHotkeyControllers()
        {
            var methods = typeof(RadarWindow).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                .Where(m => m.GetCustomAttribute<HotkeyAttribute>() is not null);

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<HotkeyAttribute>();
                if (attr is not null)
                {
                    var controller = new HotkeyActionController(
                        attr.Name,
                        attr.Type,
                        method.CreateDelegate<HotkeyDelegate>(),
                        attr.Interval);
                    HotkeyAction.RegisterController(controller);
                }
            }
        }

        #region Hotkey Action Handlers

        [Hotkey("Toggle Show Quest Items")]
        private static void ToggleShowQuestItems_HotkeyStateChanged(bool isKeyDown)
        {
            if (isKeyDown)
            {
                LootFilter.ShowQuestItems = !LootFilter.ShowQuestItems;
                Memory.Loot?.RefreshFilter();
            }
        }

        [Hotkey("Toggle Aimview Widget")]
        private static void ToggleAimviewWidget_HotkeyStateChanged(bool isKeyDown)
        {
            if (isKeyDown)
                Program.Config.AimviewWidget.Enabled = !Program.Config.AimviewWidget.Enabled;
        }

        [Hotkey("Toggle Show Meds")]
        private static void ToggleShowMeds_HotkeyStateChanged(bool isKeyDown)
        {
            if (isKeyDown)
            {
                LootFilter.ShowMeds = !LootFilter.ShowMeds;
                Memory.Loot?.RefreshFilter();
            }
        }

        [Hotkey("Toggle Show Food")]
        private static void ToggleShowFood_HotkeyStateChanged(bool isKeyDown)
        {
            if (isKeyDown)
            {
                LootFilter.ShowFood = !LootFilter.ShowFood;
                Memory.Loot?.RefreshFilter();
            }
        }

        [Hotkey("Toggle Game Info Tab")]
        private static void ToggleInfo_HotkeyStateChanged(bool isKeyDown)
        {
            if (isKeyDown)
                Program.Config.InfoWidget.Enabled = !Program.Config.InfoWidget.Enabled;
        }

        [Hotkey("Toggle Player Names")]
        private static void ToggleNames_HotkeyStateChanged(bool isKeyDown)
        {
            if (isKeyDown)
                Program.Config.UI.HideNames = !Program.Config.UI.HideNames;
        }

        [Hotkey("Toggle Loot")]
        private static void ToggleLoot_HotkeyStateChanged(bool isKeyDown)
        {
            if (isKeyDown)
                Program.Config.Loot.Enabled = !Program.Config.Loot.Enabled;
        }

        [Hotkey("Zoom Out", HotkeyType.OnIntervalElapsed, HK_ZOOMTICKDELAY)]
        private static void ZoomOut_HotkeyDelayElapsed(bool isKeyDown)
        {
            ZoomOut(HK_ZOOMTICKAMT);
        }

        [Hotkey("Zoom In", HotkeyType.OnIntervalElapsed, HK_ZOOMTICKDELAY)]
        private static void ZoomIn_HotkeyDelayElapsed(bool isKeyDown)
        {
            ZoomIn(HK_ZOOMTICKAMT);
        }

        #endregion

        /// <summary>
        /// Zooms the map 'in'.
        /// </summary>
        public static void ZoomIn(int amt)
        {
            Program.Config.UI.Zoom = Math.Max(1, Program.Config.UI.Zoom - amt);
        }

        /// <summary>
        /// Zooms the map 'out'.
        /// </summary>
        public static void ZoomOut(int amt)
        {
            Program.Config.UI.Zoom = Math.Min(200, Program.Config.UI.Zoom + amt);
        }

        /// <summary>
        /// Purge SKResources to free up memory.
        /// </summary>
        public static void PurgeSKResources()
        {
            _purgeSkResources = true;
        }

        private static async Task RunFpsTimerAsync()
        {
            while (await _fpsTimer.WaitForNextTickAsync())
            {
                _statusOrder = (_statusOrder >= 3) ? 1 : _statusOrder + 1;
                _fps = Interlocked.Exchange(ref _fpsCounter, 0);
            }
        }

        #endregion

        #region Win32 Interop

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        [LibraryImport("dwmapi.dll")]
        private static partial int DwmSetWindowAttribute(nint hwnd, int attr, ref int attrValue, int attrSize);

        [LibraryImport("user32.dll")]
        private static partial nint SendMessageW(nint hWnd, uint Msg, nint wParam, nint lParam);

        [LibraryImport("user32.dll")]
        private static partial nint LoadImageW(nint hInst, nint lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
        private static partial nint GetModuleHandleW(string lpModuleName);

        private const uint IMAGE_ICON = 1;
        private const uint LR_DEFAULTCOLOR = 0x00000000;
        private const uint WM_SETICON = 0x0080;
        private const nint ICON_SMALL = 0;
        private const nint ICON_BIG = 1;

        // Resource ID for the application icon (matches ApplicationIcon in csproj)
        // Windows uses the first icon resource, typically ID 32512
        private const int IDI_APPLICATION = 32512;

        /// <summary>
        /// Enables dark mode for the window title bar.
        /// </summary>
        private static void EnableDarkMode(nint hwnd)
        {
            int useImmersiveDarkMode = 1;
            _ = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useImmersiveDarkMode, sizeof(int));
        }

        /// <summary>
        /// Sets the window icon from the embedded application icon resource.
        /// </summary>
        private static void SetWindowIcon(nint hwnd)
        {
            var hModule = GetModuleHandleW(null);
            if (hModule == nint.Zero)
                return;

            // Load the icon from the embedded resource (uses MAKEINTRESOURCE pattern)
            var hIconSmall = LoadImageW(hModule, IDI_APPLICATION, IMAGE_ICON, 16, 16, LR_DEFAULTCOLOR);
            var hIconBig = LoadImageW(hModule, IDI_APPLICATION, IMAGE_ICON, 32, 32, LR_DEFAULTCOLOR);

            if (hIconSmall != nint.Zero)
                SendMessageW(hwnd, WM_SETICON, ICON_SMALL, hIconSmall);
            if (hIconBig != nint.Zero)
                SendMessageW(hwnd, WM_SETICON, ICON_BIG, hIconBig);
        }

        #endregion

    }
}

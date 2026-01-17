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
using LoneEftDmaRadar.Misc;
using LoneEftDmaRadar.Tarkov;
using LoneEftDmaRadar.Tarkov.World.Exits;
using LoneEftDmaRadar.Tarkov.World.Explosives;
using LoneEftDmaRadar.Tarkov.World.Hazards;
using LoneEftDmaRadar.Tarkov.World.Loot;
using LoneEftDmaRadar.Tarkov.World.Player;
using LoneEftDmaRadar.Tarkov.World.Quests;
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
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace LoneEftDmaRadar.UI
{
    internal static partial class RadarWindow
    {
        #region Initialization

        private static Task _load = null!;
        private static IWindow _window = null!;
        private static GL _gl = null!;
        private static IInputContext _input = null!;
        private static SKSurface _skSurface = null!;
        private static GRContext _grContext = null!;
        private static GRBackendRenderTarget _skBackendRenderTarget = null!;
        private static readonly RateLimiter _purgeRL = new(TimeSpan.FromSeconds(1));

        private static EftDmaConfig Config { get; } = Program.Config;
        public static IntPtr Handle => _window?.Native?.Win32?.Hwnd ?? IntPtr.Zero;

        internal static void Initialize()
        {
            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(
                (int)Config.UI.WindowSize.Width,
                (int)Config.UI.WindowSize.Height);
            options.Title = Program.Name;
            options.VSync = false;
            options.FramesPerSecond = Config.UI.FPS;
            options.PreferredStencilBufferBits = 8;
            options.PreferredBitDepth = new Vector4D<int>(8, 8, 8, 8);

            // Restore maximized state from config (not fullscreen)
            if (Config.UI.WindowMaximized)
            {
                options.WindowState = WindowState.Maximized;
            }

            _window = Window.Create(options);

            _window.Load += OnLoad;
            _window.Render += OnRender;
            _window.Resize += OnResize;
            _window.Closing += OnClosing;
            _window.StateChanged += OnStateChanged;

            // Start Dispatcher, and Set Synchronization Context
            Dispatcher = new SilkDispatcher(_window);
            SynchronizationContext.SetSynchronizationContext(new SilkSyncContext(Dispatcher));
        }

        /// <summary>
        /// Run the Radar Window (Blocking Call until closed).
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Run() => _window.Run();

        private static void OnLoad()
        {
            _gl = GL.GetApi(_window);

            // ApplyCustomImGuiStyle dark mode and window icon (Windows only)
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
            _grContext.SetResourceCacheLimit(512 * 1024 * 1024);

            CreateSkiaSurface();

            // ImGuiController will setup ImGui context
            // Use the onConfigureIO callback to configure fonts BEFORE the atlas is built
            _imgui = new ImGuiController(
                gl: _gl,
                view: _window,
                input: _input,
                onConfigureIO: () =>
                {
                    ConfigureImGuiFonts(13f);
                }
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

            ApplyCustomImGuiStyle();
            SettingsPanel.UpdateUIScale(Config.UI.UIScale);

            _load = OnLoadAsync(); // Load remaining modules and UI components asynchronously
            _window.Update += OnLoadAsync_Update;
        }

        private static void OnLoadAsync_Update(double delta)
        {
            if (_load?.IsCompleted ?? false)
            {
                _load.GetAwaiter().GetResult(); // Observes any exceptions
                _load = null;
                _window.Update -= OnLoadAsync_Update;
            }
        }

        /// <summary>
        /// Load async modules and remaining UI components.
        /// </summary>
        private static async Task OnLoadAsync()
        {
            SKPaints.PaintBitmap.ColorFilter = SKPaints.GetDarkModeColorFilter(0.7f);
            SKPaints.PaintBitmapAlpha.ColorFilter = SKPaints.GetDarkModeColorFilter(0.7f);
            var tarkovDataManager = TarkovDataManager.ModuleInitAsync();
            var eftMapManager = EftMapManager.ModuleInitAsync();
            var memoryInterface = Memory.ModuleInitAsync();

            // Wait for all tasks
            await Task.WhenAll(tarkovDataManager, eftMapManager, memoryInterface);
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

            // Initialize widgets
            AimviewWidget.Initialize(_gl, _grContext);
            LootWidget.Initialize();

            // Start FPS timer
            _ = RunFpsTimerAsync();

            // Ready
            Program.UpdateState(AppState.ProcessNotStarted);
        }

        private static void CreateSkiaSurface()
        {
            _skSurface?.Dispose();
            _skSurface = null;
            _skBackendRenderTarget?.Dispose();
            _skBackendRenderTarget = null;

            var size = _window.FramebufferSize;
            if (size.X <= 0 || size.Y <= 0 || _grContext is null)
            {
                _skSurface = null!;
                _skBackendRenderTarget = null!;
                return;
            }

            _gl.GetInteger(GetPName.SampleBuffers, out int sampleBuffers);
            _gl.GetInteger(GetPName.Samples, out int samples);
            if (sampleBuffers == 0)
                samples = 0;
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0); // bind default framebuffer
            _gl.GetFramebufferAttachmentParameter(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.StencilAttachment,
                FramebufferAttachmentParameterName.StencilSize,
                out int stencilBits
            );

            var fbInfo = new GRGlFramebufferInfo(
                0, // default framebuffer
                (uint)InternalFormat.Rgba8
            );

            _skBackendRenderTarget = new GRBackendRenderTarget(
                size.X,
                size.Y,
                samples,
                stencilBits,
                fbInfo
            );

            _skSurface = SKSurface.Create(
                _grContext,
                _skBackendRenderTarget,
                GRSurfaceOrigin.BottomLeft,
                SKColorType.Rgba8888
            );
        }


        #endregion

        #region Radar Operation

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
        private static bool LootCorpsesVisible => Config.Loot.Enabled && !Config.Loot.HideCorpses && !SearchFilterIsSet;
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

        /// <summary>
        /// Main Render Loop.
        /// </summary>
        /// <remarks>
        /// WARNING: Be careful modifying this method. The order of operations is critical to prevent rendering/resource issues.
        /// </remarks>
        /// <param name="delta"></param>
        private static void OnRender(double delta)
        {
            if (_grContext is null || _skSurface is null)
                return;
            try
            {
                // Frame Setup
                Interlocked.Increment(ref _fpsCounter);
                _grContext.ResetContext();
                if (_purgeRL.TryEnter())
                {
                    _grContext.PurgeUnlockedResources(false);
                }

                // Scene Render (Skia)
                var fbSize = _window.FramebufferSize;
                var state = Program.State;
                DrawSkiaScene(ref fbSize, state);

                if (state > AppState.Initializing)
                {
                    // Aimview Widget (Skia)
                    AimviewWidget.Render();
                    // UI Render (ImGui)
                    DrawImGuiUI(ref fbSize, delta);
                }
            }
            catch (Exception ex)
            {
                Logging.WriteLine($"***** CRITICAL RENDER ERROR: {ex}");
            }
        }

        private static void DrawSkiaScene(ref Vector2D<int> fbSize, AppState state)
        {
            _gl.Viewport(0, 0, (uint)fbSize.X, (uint)fbSize.Y);
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            // Explicitly clear the backbuffer to avoid blending against stale pixels.
            _gl.ClearColor(0f, 0f, 0f, 1f); // BLACK
            _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.StencilBufferBit | ClearBufferMask.DepthBufferBit);

            var canvas = _skSurface.Canvas;
            try
            {
                if (state == AppState.InRaid && LocalPlayer is LocalPlayer localPlayer && EftMapManager.LoadMap(MapID) is IEftMap map)
                {
                    DrawRadar(canvas, localPlayer, map);
                }
                else
                {
                    DrawStatusMessage(canvas, state);
                }
            }
            finally
            {
                canvas.Flush();
                _grContext.Flush();
            }
        }

        private static void DrawRadar(SKCanvas canvas, LocalPlayer localPlayer, IEftMap map)
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
                mapParams = map.GetParameters(canvasSize, Config.UI.Zoom, ref panPos);
                _mapPanPosition = panPos;
            }
            else
            {
                _mapPanPosition = default;
                mapParams = map.GetParameters(canvasSize, Config.UI.Zoom, ref localPlayerMapPos);
            }

            var mapCanvasBounds = new SKRect(0, 0, canvasSize.Width, canvasSize.Height);

            // Draw Map
            map.Draw(canvas, localPlayer.Position.Y, mapParams.Bounds, mapCanvasBounds);

            // Draw loot
            if (Config.Loot.Enabled)
            {
                if (FilteredLoot is IEnumerable<LootItem> loot)
                {
                    foreach (var item in loot)
                    {
                        item.Draw(canvas, mapParams, localPlayer);
                    }
                }

                if (Config.Containers.Enabled && Containers is IEnumerable<StaticLootContainer> containers)
                {
                    foreach (var container in containers)
                    {
                        if (Config.Containers.Selected.ContainsKey(container.ID ?? "NULL"))
                        {
                            container.Draw(canvas, mapParams, localPlayer);
                        }
                    }
                }
            }

            // Draw hazards
            if (Config.UI.ShowHazards && Hazards is IReadOnlyCollection<IWorldHazard> hazards)
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
            if (Config.UI.ShowExfils && Exits is IReadOnlyCollection<IExitPoint> exits)
            {
                foreach (var exit in exits)
                {
                    exit.Draw(canvas, mapParams, localPlayer);
                }
            }

            // Draw quest locations
            if (Config.QuestHelper.Enabled && Quests?.LocationConditions?.Values is IEnumerable<QuestLocation> questLocations)
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

            // Draw Map Pings
            foreach (var ping in _activeMapPings.Values)
            {
                ping.Draw(canvas, mapParams, localPlayer);
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
                    if (player.IsHumanHostileActive && player.GroupId != AbstractPlayer.SoloGroupId)
                    {
                        if (!groupedByGrp.TryGetValue(player.GroupId, out var list))
                        {
                            list = new PooledList<SKPoint>(capacity: 5);
                            groupedByGrp[player.GroupId] = list;
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


        private static void DrawStatusMessage(SKCanvas canvas, AppState state)
        {
            var bounds = new SKRect(0, 0, _window.Size.X, _window.Size.Y);

            // Base text (no trailing dots) and how many dots to draw
            string baseText;
            int dotCount;

            switch (state)
            {
                case AppState.Initializing:
                    baseText = "Initializing";
                    dotCount = _statusOrder; // 1..3
                    break;
                case AppState.ProcessNotStarted:
                    baseText = "Game Process Not Running!";
                    dotCount = 0;
                    break;
                case AppState.ProcessStarting:
                    baseText = "Starting Up";
                    dotCount = _statusOrder; // 1..3
                    break;
                case AppState.WaitingForRaid:
                    baseText = "Waiting for Raid Start";
                    dotCount = _statusOrder; // 1..3
                    break;
                default:
                    return;
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

        private static IEnumerable<IMouseoverEntity> GetMouseoverItems()
        {
            var players = AllPlayers?
                .Where(x => x is not LoneEftDmaRadar.Tarkov.World.Player.LocalPlayer && !x.HasExfild && (!LootCorpsesVisible || x.IsAlive)) ??
                Enumerable.Empty<AbstractPlayer>();

            var loot = Config.Loot.Enabled ?
                FilteredLoot ?? Enumerable.Empty<IMouseoverEntity>() : Enumerable.Empty<IMouseoverEntity>();
            var containers = Config.Loot.Enabled && Config.Containers.Enabled ?
                Containers ?? Enumerable.Empty<IMouseoverEntity>() : Enumerable.Empty<IMouseoverEntity>();
            var exits = Config.UI.ShowExfils ?
                Exits ?? Enumerable.Empty<IMouseoverEntity>() : Enumerable.Empty<IMouseoverEntity>();
            var quests = Config.QuestHelper.Enabled ?
                Quests?.LocationConditions?.Values?.OfType<IMouseoverEntity>() ?? Enumerable.Empty<IMouseoverEntity>()
                : Enumerable.Empty<IMouseoverEntity>();
            var hazards = Config.UI.ShowHazards ?
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

        #region ImGui Menus

        private static ImGuiController _imgui = null!;

        private static void DrawImGuiUI(ref Vector2D<int> fbSize, double delta)
        {
            _gl.Viewport(0, 0, (uint)fbSize.X, (uint)fbSize.Y);
            _gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            _imgui.Update((float)delta);
            try
            {
                // Draw overlay controls
                RadarMenuPanel.Draw();
                MapSetupHelperPanel.DrawOverlay();

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
            finally
            {
                _imgui.Render();
            }
        }

        private static void DrawWindows()
        {
            var state = Program.State;
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
            if (AimviewWidget.IsOpen && state == AppState.InRaid)
            {
                AimviewWidget.Draw();
            }

            // Player Info Widget
            if (PlayerInfoWidget.IsOpen && state == AppState.InRaid)
            {
                PlayerInfoWidget.Draw();
            }

            // Loot Widget
            if (LootWidget.IsOpen && state == AppState.InRaid && Config.Loot.Enabled)
            {
                LootWidget.Draw();
            }
        }

        private static void DrawLootFiltersWindow()
        {
            bool isOpen = _isLootFiltersOpen;
            ImGui.SetNextWindowSize(new Vector2(900, 700), ImGuiCond.FirstUseEver);
            if (ImGui.Begin("Loot Filters", ref isOpen))
            {
                LootFiltersPanel.Draw();
                ImGui.Separator();
                if (ImGui.Button("Apply & Close"))
                {
                    LootFiltersPanel.RefreshLootFilter();
                    isOpen = false; // close the window this frame
                }
            }
            ImGui.End();

            // If the user closed the window via the X button, apply the filter once on close.
            if (_isLootFiltersOpen && !isOpen)
            {
                LootFiltersPanel.RefreshLootFilter();
            }

            _isLootFiltersOpen = isOpen;
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

        #region UI State and Events

        private static readonly PeriodicTimer _fpsTimer = new(TimeSpan.FromSeconds(1));
        private static readonly ConcurrentDictionary<IMapEntity, MapPingEffect> _activeMapPings = new();
        private static int _fpsCounter = 0;
        private static int _statusOrder = 1;
        private static bool _mouseDown;
        private static Vector2 _lastMousePosition;
        private static IMouseoverEntity _mouseOverItem;
        private static DateTime _lastClickTime;
        private const double DoubleClickThresholdMs = 300;
        private static int _fps;
        private static bool _isLootFiltersOpen;
        private static bool _isWebRadarOpen;
        private static bool _isMapFreeEnabled;
        private static Vector2 _mapPanPosition;

        /// <summary>
        /// RadarWindow Dispatcher for marshaling to the UI thread.
        /// </summary>
        public static SilkDispatcher Dispatcher { get; private set; } = null!;

        /// <summary>
        /// Ping a map entity with an expanding circle effect.
        /// </summary>
        /// <param name="entity">Entity to be pinged.</param>
        public static void PingMapEntity(IMapEntity entity)
        {
            _ = _activeMapPings.GetOrAdd(entity, e =>
            {
                return new MapPingEffect(e);
            });
        }

        private readonly struct MapPingEffect : IMapEntity
        {
            // Based on implementation from https://github.com/dma-educational-resources/eft-dma-radar
            private static readonly long _duration = TimeSpan.FromSeconds(2).Ticks;
            private readonly IMapEntity _entity;
            private readonly long _start;

            public MapPingEffect(IMapEntity entity)
            {
                _entity = entity;
                _start = Stopwatch.GetTimestamp();
            }

            public ref readonly Vector3 Position => ref _entity.Position;

            public void Draw(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
            {
                if (_entity is LootItem && !Config.Loot.Enabled) // Don't draw ping if loot is disabled
                    return;
                var now = Stopwatch.GetTimestamp();
                var elapsedTicks = now - _start;

                if (elapsedTicks >= _duration)
                {
                    _activeMapPings.TryRemove(_entity, out _);
                    return;
                }

                float progress = (float)elapsedTicks / _duration;
                float radius = 10 + 50 * progress;
                float alpha = 1f - progress;

                var center = _entity.Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams);

                using var paint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 4 * Config.UI.UIScale,
                    Color = SKPaints.PaintMapPing.Color.WithAlpha((byte)(alpha * 255)),
                    IsAntialias = true
                };

                canvas.DrawCircle(center.X, center.Y, radius, paint);
                _activeMapPings.TryAdd(_entity, this);
            }
        }

        private static void OnResize(Vector2D<int> size)
        {
            _gl.Viewport(size);
            CreateSkiaSurface();
        }

        private static void OnStateChanged(WindowState state)
        {
            // Track maximized state for persistence
            // Note: Fullscreen (hidden border + maximized) is NOT persisted - only regular maximized
            if (_window.WindowBorder == WindowBorder.Resizable)
            {
                Config.UI.WindowMaximized = (state == WindowState.Maximized);
            }
        }

        private static void OnClosing()
        {
            // Save window state - only save size if not maximized/fullscreen
            if (_window.WindowState == WindowState.Normal)
            {
                Config.UI.WindowSize = new SKSize(_window.Size.X, _window.Size.Y);
            }

            Config.UI.WindowMaximized = _window.WindowState == WindowState.Maximized;
            // CurrentDomain_ProcessExit will execute after this point
        }

        private static void OnMouseDown(IMouse mouse, MouseButton button)
        {
            // Let ImGui handle mouse if it wants to
            if (ImGui.GetIO().WantCaptureMouse)
                return;

            var pos = mouse.Position;
            var mousePos = new Vector2(pos.X, pos.Y);

            if (button == MouseButton.Left)
            {
                // Check for double-click
                var now = DateTime.UtcNow;
                bool isDoubleClick = (now - _lastClickTime).TotalMilliseconds < DoubleClickThresholdMs;
                _lastClickTime = now;

                if (isDoubleClick)
                {
                    if (_mouseOverItem is ObservedPlayer obs) // Toggle Teammate Status on Double Click
                    {
                        obs.ToggleTeammate();
                    }
                }

                _lastMousePosition = mousePos;
                _mouseDown = true;
            }
            else if (button == MouseButton.Right)
            {
                if (_mouseOverItem is AbstractPlayer player)
                {
                    player.SetFocus(!player.IsFocused);
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
                int newZoom = Config.UI.Zoom - delta;
                Config.UI.Zoom = Math.Clamp(newZoom, 1, 200);
            }
        }

        private static void ProcessMouseoverData(Vector2 mousePos)
        {
            if (Program.State != AppState.InRaid)
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
                    MouseoverGroup = (player.IsHumanHostile && player.GroupId != AbstractPlayer.SoloGroupId) ? player.GroupId : null;
                    if (LootCorpsesVisible && player.LootObject is LootCorpse playerCorpse)
                    {
                        _mouseOverItem = playerCorpse;
                    }
                    break;

                case LootCorpse corpseObj:
                    _mouseOverItem = corpseObj;
                    var corpse = corpseObj.Player;
                    MouseoverGroup = (corpse?.IsHumanHostile == true && corpse.GroupId != AbstractPlayer.SoloGroupId) ? corpse.GroupId : null;
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

            static void ClearMouseoverRefs()
            {
                _mouseOverItem = null;
                MouseoverGroup = null;
            }
        }

        private static async Task RunFpsTimerAsync()
        {
            while (await _fpsTimer.WaitForNextTickAsync()) // 1 Second Interval
            {
                _statusOrder = (_statusOrder >= 3) ? 1 : _statusOrder + 1;
                _fps = Interlocked.Exchange(ref _fpsCounter, 0);
            }
        }

        /// <summary>
        /// Cross-thread dispatcher for invoking actions on the main UI thread.
        /// </summary>
        public sealed class SilkDispatcher
        {
            private readonly ConcurrentQueue<Action> _q1 = new();
            private readonly ConcurrentQueue<Action> _q2 = new();
            private ConcurrentQueue<Action> _queue;

            /// <summary>
            /// Managed thread ID of the UI thread this dispatcher is associated with.
            /// </summary>
            public int ThreadId { get; }

            private SilkDispatcher() { }

            public SilkDispatcher(IWindow window)
            {
                _queue = _q1;
                ThreadId = Environment.CurrentManagedThreadId;
                window.Update += Window_Update;
            }

            private void Window_Update(double delta) => Pump();

            private void Pump()
            {
                if (!_queue.IsEmpty)
                {
                    ConcurrentQueue<Action> snapshot;
                    if (_queue == _q1)
                    {
                        snapshot = Interlocked.Exchange(ref _queue, _q2);
                    }
                    else
                    {
                        snapshot = Interlocked.Exchange(ref _queue, _q1);
                    }
                    while (snapshot.TryDequeue(out var action)) // will be empty after dequeueing all
                        action();
                }
            }

            /// <summary>
            /// Invoke an action on the UI thread, blocking until complete.
            /// </summary>
            /// <param name="action"></param>
            /// <exception cref="TargetInvocationException"></exception>
            public void Invoke(Action action)
            {
                if (Environment.CurrentManagedThreadId == ThreadId) // Fast Path
                {
                    action();
                    return;
                }

                using var sync = new ManualResetEventSlim();
                Exception capturedEx = null;
                _queue.Enqueue(() =>
                {
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        capturedEx = ex;
                    }
                    finally
                    {
                        sync.Set();
                    }
                });

                sync.Wait();

                if (capturedEx is not null)
                    throw new TargetInvocationException(capturedEx);
            }

            /// <summary>
            /// Invoke an action on the UI thread asynchronously.
            /// </summary>
            /// <param name="action"></param>
            /// <returns></returns>
            public Task InvokeAsync(Action action)
            {
                if (Environment.CurrentManagedThreadId == ThreadId) // Fast Path
                {
                    action();
                    return Task.CompletedTask;
                }

                var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                _queue.Enqueue(() =>
                {
                    try
                    {
                        action();
                        tcs.SetResult();
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                });

                return tcs.Task;
            }
        }

        /// <summary>
        /// Custom SynchronizationContext that posts work to the SilkDispatcher.
        /// </summary>
        private sealed class SilkSyncContext : SynchronizationContext
        {
            private readonly SilkDispatcher _dispatcher;

            private SilkSyncContext() { }

            public SilkSyncContext(SilkDispatcher dispatcher)
            {
                _dispatcher = dispatcher;
            }

            public override void Post(SendOrPostCallback d, object state)
            {
                _ = _dispatcher.InvokeAsync(() => d(state));
            }

            public override void Send(SendOrPostCallback d, object state)
            {
                if (Environment.CurrentManagedThreadId == _dispatcher.ThreadId)
                {
                    d(state); // inline if on render thread
                }
                else
                {
                    _dispatcher.Invoke(() => d(state)); // blocking if off-thread
                }
            }
        }

        #endregion

        #region Hotkeys

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
                    Config.Loot.Enabled = !Config.Loot.Enabled;
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
                Config.AimviewWidget.Enabled = !Config.AimviewWidget.Enabled;
        }

        [Hotkey("Toggle Player Info Widget")]
        private static void ToggleInfo_HotkeyStateChanged(bool isKeyDown)
        {
            if (isKeyDown)
                Config.InfoWidget.Enabled = !Config.InfoWidget.Enabled;
        }

        [Hotkey("Toggle Loot Widget")]
        private static void ToggleLootWidget_HotkeyStateChanged(bool isKeyDown)
        {
            if (isKeyDown)
                Config.LootWidget.Enabled = !Config.LootWidget.Enabled;
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

        [Hotkey("Toggle Loot")]
        private static void ToggleLoot_HotkeyStateChanged(bool isKeyDown)
        {
            if (isKeyDown)
                Config.Loot.Enabled = !Config.Loot.Enabled;
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
            Config.UI.Zoom = Math.Max(1, Config.UI.Zoom - amt);
        }

        /// <summary>
        /// Zooms the map 'out'.
        /// </summary>
        public static void ZoomOut(int amt)
        {
            Config.UI.Zoom = Math.Min(200, Config.UI.Zoom + amt);
        }

        #endregion

        #region Fonts & Styling

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


        /// <summary>
        /// Configures custom fonts. Must be called before font atlas is built.
        /// </summary>
        private static unsafe void ConfigureImGuiFonts(float basePixelSize)
        {
            var io = ImGui.GetIO();
            var fontBytes = LoadEmbeddedFontBytes();

            if (fontBytes is null || fontBytes.Length == 0)
                return; // Fall back to default font

            io.Fonts.Clear();

            var cfg = new ImFontConfigPtr(ImGuiNET.ImGuiNative.ImFontConfig_ImFontConfig());
            cfg.OversampleH = 3;
            cfg.OversampleV = 2;
            cfg.PixelSnapH = true;

            fixed (byte* pFont = fontBytes)
            {
                io.Fonts.AddFontFromMemoryTTF((nint)pFont, fontBytes.Length, basePixelSize, cfg);
            }

            cfg.Destroy();

            static byte[] LoadEmbeddedFontBytes()
            {
                try
                {
                    using var stream = Utilities.OpenResource("LoneEftDmaRadar.Resources.NeoSansStdRegular.otf");
                    if (stream is null)
                        return null;

                    var bytes = new byte[stream.Length];
                    stream.ReadExactly(bytes);
                    return bytes;
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Applies a custom ImGui style/theme.
        /// </summary>
        public static void ApplyCustomImGuiStyle()
        {
            ImGui.StyleColorsDark();

            var style = ImGui.GetStyle();

            // HUD / Radar style: rounded, calm contrast, minimal borders
            style.WindowRounding = 10f;
            style.ChildRounding = 10f;
            style.PopupRounding = 10f;
            style.FrameRounding = 8f;
            style.ScrollbarRounding = 12f;
            style.GrabRounding = 10f;
            style.TabRounding = 10f;

            // Comfortable spacing for menus, not too chunky
            style.WindowPadding = new Vector2(12, 10);
            style.FramePadding = new Vector2(8, 5);
            style.ItemSpacing = new Vector2(8, 6);
            style.ItemInnerSpacing = new Vector2(6, 4);
            style.ScrollbarSize = 14f;
            style.GrabMinSize = 12f;

            // Subtle borders (avoid boxy look)
            style.WindowBorderSize = 0.0f;
            style.ChildBorderSize = 0.0f;
            style.PopupBorderSize = 1.0f;
            style.FrameBorderSize = 0.0f;

            style.WindowTitleAlign = new Vector2(0.5f, 0.5f);
            style.ButtonTextAlign = new Vector2(0.5f, 0.5f);
            style.WindowMenuButtonPosition = ImGuiDir.None;

            // Slight translucency helps the map feel like the primary layer
            style.Alpha = 0.96f;

            var colors = style.Colors;

            // Dark HUD palette (blue/gray) + teal accent
            Vector4 bg = new(0.06f, 0.07f, 0.09f, 1.00f);
            Vector4 bg2 = new(0.09f, 0.10f, 0.12f, 1.00f);
            Vector4 frame = new(0.12f, 0.13f, 0.16f, 1.00f);
            Vector4 frameHover = new(0.16f, 0.17f, 0.21f, 1.00f);
            Vector4 frameActive = new(0.18f, 0.20f, 0.24f, 1.00f);

            Vector4 text = new(0.95f, 0.96f, 0.98f, 1.00f);
            Vector4 textDisabled = new(0.55f, 0.58f, 0.62f, 1.00f);

            Vector4 border = new(0.20f, 0.22f, 0.27f, 0.85f);

            Vector4 accent = new(0.12f, 0.78f, 0.71f, 1.00f);
            Vector4 accentHover = new(0.18f, 0.86f, 0.78f, 1.00f);
            Vector4 accentActive = new(0.10f, 0.66f, 0.60f, 1.00f);

            // Text
            colors[(int)ImGuiCol.Text] = text;
            colors[(int)ImGuiCol.TextDisabled] = textDisabled;

            // Windows
            colors[(int)ImGuiCol.WindowBg] = new Vector4(bg.X, bg.Y, bg.Z, 0.92f);
            colors[(int)ImGuiCol.ChildBg] = new Vector4(0f, 0f, 0f, 0f);
            colors[(int)ImGuiCol.PopupBg] = new Vector4(bg2.X, bg2.Y, bg2.Z, 0.98f);

            // Borders
            colors[(int)ImGuiCol.Border] = border;
            colors[(int)ImGuiCol.BorderShadow] = new Vector4(0f, 0f, 0f, 0f);

            // Frames / Inputs
            colors[(int)ImGuiCol.FrameBg] = new Vector4(frame.X, frame.Y, frame.Z, 0.95f);
            colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(frameHover.X, frameHover.Y, frameHover.Z, 0.95f);
            colors[(int)ImGuiCol.FrameBgActive] = new Vector4(frameActive.X, frameActive.Y, frameActive.Z, 1.00f);

            // Titles / Menu bar
            colors[(int)ImGuiCol.TitleBg] = new Vector4(bg2.X, bg2.Y, bg2.Z, 0.95f);
            colors[(int)ImGuiCol.TitleBgActive] = new Vector4(bg2.X, bg2.Y, bg2.Z, 0.98f);
            colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(bg2.X, bg2.Y, bg2.Z, 0.75f);
            colors[(int)ImGuiCol.MenuBarBg] = new Vector4(bg2.X, bg2.Y, bg2.Z, 0.92f);

            // Scrollbar
            colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(bg.X, bg.Y, bg.Z, 0.70f);
            colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.30f, 0.33f, 0.40f, 0.70f);
            colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.38f, 0.42f, 0.50f, 0.80f);
            colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.45f, 0.50f, 0.60f, 0.90f);

            // Check / Slider
            colors[(int)ImGuiCol.CheckMark] = accent;
            colors[(int)ImGuiCol.SliderGrab] = new Vector4(accent.X, accent.Y, accent.Z, 0.75f);
            colors[(int)ImGuiCol.SliderGrabActive] = accentActive;

            // Buttons (teal only on interaction)
            colors[(int)ImGuiCol.Button] = new Vector4(frame.X, frame.Y, frame.Z, 0.75f);
            colors[(int)ImGuiCol.ButtonHovered] = new Vector4(accent.X, accent.Y, accent.Z, 0.35f);
            colors[(int)ImGuiCol.ButtonActive] = new Vector4(accent.X, accent.Y, accent.Z, 0.55f);

            // Headers (tree nodes, selectable, etc.)
            colors[(int)ImGuiCol.Header] = new Vector4(frame.X, frame.Y, frame.Z, 0.55f);
            colors[(int)ImGuiCol.HeaderHovered] = new Vector4(accent.X, accent.Y, accent.Z, 0.30f);
            colors[(int)ImGuiCol.HeaderActive] = new Vector4(accent.X, accent.Y, accent.Z, 0.45f);

            // Separators
            colors[(int)ImGuiCol.Separator] = new Vector4(border.X, border.Y, border.Z, 0.60f);
            colors[(int)ImGuiCol.SeparatorHovered] = new Vector4(accentHover.X, accentHover.Y, accentHover.Z, 0.70f);
            colors[(int)ImGuiCol.SeparatorActive] = accent;

            // Resize grip (keep subtle)
            colors[(int)ImGuiCol.ResizeGrip] = new Vector4(accent.X, accent.Y, accent.Z, 0.12f);
            colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(accentHover.X, accentHover.Y, accentHover.Z, 0.30f);
            colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(accentActive.X, accentActive.Y, accentActive.Z, 0.45f);

            // Tabs (if supported by your ImGui.NET version)
            colors[(int)ImGuiCol.Tab] = new Vector4(frame.X, frame.Y, frame.Z, 0.60f);
            colors[(int)ImGuiCol.TabHovered] = new Vector4(accent.X, accent.Y, accent.Z, 0.25f);

            // Selection
            colors[(int)ImGuiCol.TextSelectedBg] = new Vector4(accent.X, accent.Y, accent.Z, 0.35f);
            colors[(int)ImGuiCol.DragDropTarget] = new Vector4(accentHover.X, accentHover.Y, accentHover.Z, 0.90f);
            colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(0f, 0f, 0f, 0.65f);

            // Apply scaling after the baseline theme is set.
            _lastImGuiUiScale = 1f;
            ApplyImGuiScale(Config.UI.UIScale);
        }

        private static float _lastImGuiUiScale = 1f;

        private static void ApplyImGuiScale(float uiScale)
        {
            uiScale = Math.Clamp(uiScale, 0.5f, 2.0f);

            // Scale fonts via global font scale (doesn't rebuild atlas)
            var io = ImGui.GetIO();
            io.FontGlobalScale = uiScale;

            // Style sizes are already authored for 1.0; use incremental scaling ratio to avoid compounding.
            // This is safer than trying to copy/assign full ImGuiStylePtr.
            float last = _lastImGuiUiScale;
            if (last <= 0f)
                last = 1f;
            float ratio = uiScale / last;

            // Apply only the delta ratio.
            var style = ImGui.GetStyle();
            style.ScaleAllSizes(ratio);

            // Guard against invalid window mins.
            style.WindowMinSize = new Vector2(Math.Max(1f, style.WindowMinSize.X), Math.Max(1f, style.WindowMinSize.Y));

            _lastImGuiUiScale = uiScale;
        }

        #endregion
    }
}

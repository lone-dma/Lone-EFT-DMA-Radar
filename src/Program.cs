global using SDK;
global using SkiaSharp;
global using System.Buffers;
global using System.Collections;
global using System.Collections.Concurrent;
global using System.Data;
global using System.Diagnostics;
global using System.IO;
global using System.Net;
global using System.Numerics;
global using System.Reflection;
global using System.Runtime.CompilerServices;
global using System.Runtime.InteropServices;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using Clipboard = LoneEftDmaRadar.UI.Misc.Clipboard;
global using MessageBox = LoneEftDmaRadar.UI.Misc.MessageBox;
global using MessageBoxButton = LoneEftDmaRadar.UI.Misc.MessageBoxButton;
global using MessageBoxImage = LoneEftDmaRadar.UI.Misc.MessageBoxImage;
global using MessageBoxOptions = LoneEftDmaRadar.UI.Misc.MessageBoxOptions;
global using MessageBoxResult = LoneEftDmaRadar.UI.Misc.MessageBoxResult;
global using RateLimiter = LoneEftDmaRadar.Misc.RateLimiter;
using LoneEftDmaRadar.Tarkov;
using LoneEftDmaRadar.UI;
using LoneEftDmaRadar.UI.Maps;
using LoneEftDmaRadar.UI.Misc;
using LoneEftDmaRadar.UI.Skia;
using LoneEftDmaRadar.Web.TarkovDev;
using Microsoft.Extensions.DependencyInjection;
using Silk.NET.Input.Glfw;
using Silk.NET.Windowing.Glfw;
using Velopack;
using Velopack.Sources;

namespace LoneEftDmaRadar
{
    internal partial class Program
    {
        private const string BaseName = "Lone EFT DMA Radar";
        private const string MUTEX_ID = "0f908ff7-e614-6a93-60a3-cee36c9cea91";
        private static readonly Mutex _mutex;

        /// <summary>
        /// Application Name with Version.
        /// </summary>
        internal static string Name { get; } = $"{BaseName} v{GetSemVer2OrDefault()}";
        /// <summary>
        /// Path to the Configuration Folder in %AppData%
        /// </summary>
        public static DirectoryInfo ConfigPath { get; } =
            new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Lone-EFT-DMA"));
        /// <summary>
        /// Global Program Configuration.
        /// </summary>
        public static EftDmaConfig Config { get; }
        /// <summary>
        /// TRUE if the application is installed via Velopack.
        /// </summary>
        public static bool IsInstalled { get; }
        /// <summary>
        /// Service Provider for Dependency Injection.
        /// NOTE: Web Radar has it's own container.
        /// </summary>
        public static IServiceProvider ServiceProvider { get; }
        /// <summary>
        /// HttpClientFactory for creating HttpClients.
        /// </summary>
        public static IHttpClientFactory HttpClientFactory { get; }

        static Program()
        {
            GlfwWindowing.RegisterPlatform();
            GlfwInput.RegisterPlatform();
            VelopackApp.Build().Run();
            try
            {
                IsInstalled = new UpdateManager(".").IsInstalled;
                _mutex = new Mutex(true, MUTEX_ID, out bool singleton);
                if (!singleton)
                    throw new InvalidOperationException("The application is already running.");
                Config = EftDmaConfig.Load();
                ServiceProvider = BuildServiceProvider();
                HttpClientFactory = ServiceProvider.GetRequiredService<IHttpClientFactory>();
                SetHighPerformanceMode();
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Name, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxOptions.DefaultDesktopOnly);
                throw;
            }
        }
        static void Main()
        {
            try
            {
                // Show loading window during initialization
                using var loadingWindow = new LoadingWindow();
                loadingWindow.Show();

                // Run initialization on a background thread while loading window pumps messages on main thread
                var initTask = Task.Run(() => ConfigureProgramAsync(loadingWindow));

                // Keep the loading window responsive until initialization completes
                while (!initTask.IsCompleted)
                {
                    loadingWindow.DoEvents();
                    Thread.Sleep(16); // ~60fps
                }

                // Close loading window
                loadingWindow.Close();

                // Check if initialization failed
                if (initTask.IsFaulted)
                {
                    throw initTask.Exception!.InnerException ?? initTask.Exception;
                }

                // Now start the radar window (this blocks until window closes)
                RadarWindow.Initialize();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Name, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxOptions.DefaultDesktopOnly);
                throw;
            }
        }

        #region Boilerplate

        /// <summary>
        /// Configure Program Startup with loading window progress updates.
        /// </summary>
        private static async Task ConfigureProgramAsync(LoadingWindow loadingWindow)
        {
            loadingWindow.UpdateProgress(10, "Loading, Please Wait...");

            _ = Task.Run(CheckForUpdatesAsync); // Run continuations on the thread pool

            var tarkovDataManager = TarkovDataManager.ModuleInitAsync();
            var eftMapManager = EftMapManager.ModuleInitAsync();
            var memoryInterface = Memory.ModuleInitAsync();

            var misc = Task.Run(() =>
            {
                SKPaints.PaintBitmap.ColorFilter = SKPaints.GetDarkModeColorFilter(0.7f);
                SKPaints.PaintBitmapAlpha.ColorFilter = SKPaints.GetDarkModeColorFilter(0.7f);
            });

            // Wait for all tasks
            await Task.WhenAll(tarkovDataManager, eftMapManager, memoryInterface, misc);

            loadingWindow.UpdateProgress(100, "Loading Completed!");
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e) => OnShutdown();
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                Logging.WriteLine($"*** UNHANDLED EXCEPTION (Terminating: {e.IsTerminating}): {ex}");
            }
            if (e.IsTerminating)
            {
                OnShutdown();
            }
        }

        private static void OnShutdown()
        {
            Logging.WriteLine("Saving Config and Closing DMA Connection...");
            Config.Save();
            Memory.Close();
            Logging.WriteLine("Exiting...");
        }

        /// <summary>
        /// Sets up the Dependency Injection container for the application.
        /// </summary>
        /// <returns></returns>
        private static IServiceProvider BuildServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddHttpClient(); // Add default HttpClientFactory
            TarkovDevGraphQLApi.Configure(services);
            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Sets High Performance mode in Windows Power Plans and Process Priority.
        /// </summary>
        private static void SetHighPerformanceMode()
        {
            /// Prepare Process for High Performance Mode
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED |
                                           EXECUTION_STATE.ES_DISPLAY_REQUIRED);
            var highPerformanceGuid = new Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
            if (PowerSetActiveScheme(IntPtr.Zero, ref highPerformanceGuid) != 0)
                Logging.WriteLine("WARNING: Unable to set High Performance Power Plan");
            const uint timerResolutionMs = 5;
            if (TimeBeginPeriod(timerResolutionMs) != 0)
                Logging.WriteLine($"WARNING: Unable to set timer resolution to {timerResolutionMs}ms. This may cause performance issues.");
        }

        private static async Task CheckForUpdatesAsync()
        {
            try
            {
                if (!IsInstalled)
                    return;
                var updater = new UpdateManager(
                    source: new GithubSource(
                        repoUrl: "https://github.com/lone-dma/Lone-EFT-DMA-Radar",
                        accessToken: null,
                        prerelease: false));

                var newVersion = await updater.CheckForUpdatesAsync();
                if (newVersion is not null)
                {
                    var result = MessageBox.Show(
                        messageBoxText: $"A new version ({newVersion.TargetFullRelease.Version}) is available.\n\nWould you like to update now?",
                        caption: Program.Name,
                        button: MessageBoxButton.YesNo,
                        icon: MessageBoxImage.Question,
                        options: MessageBoxOptions.DefaultDesktopOnly);

                    if (result == MessageBoxResult.Yes)
                    {
                        await updater.DownloadUpdatesAsync(newVersion);
                        updater.ApplyUpdatesAndRestart(newVersion);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    messageBoxText: $"An unhandled exception occurred while checking for updates: {ex}",
                    caption: Program.Name,
                    button: MessageBoxButton.OK,
                    icon: MessageBoxImage.Warning,
                    options: MessageBoxOptions.DefaultDesktopOnly);
            }
        }

        private static string GetSemVer2OrDefault()
        {
            try
            {
                string strV = Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyFileVersionAttribute>()
                    ?.Version;

                if (string.IsNullOrWhiteSpace(strV))
                    return "0.0.0";

                var v = new Version(strV);
                return $"{v.Major}.{v.Minor}.{v.Build}";
            }
            catch
            {
                return "0.0.0";
            }
        }

        [LibraryImport("kernel32.dll")]
        private static partial EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        [Flags]
        public enum EXECUTION_STATE : uint
        {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_SYSTEM_REQUIRED = 0x00000001
            // Legacy flag, should not be used.
            // ES_USER_PRESENT = 0x00000004
        }

        [LibraryImport("powrprof.dll")]
        private static partial uint PowerSetActiveScheme(IntPtr userRootPowerKey, ref Guid schemeGuid);

        [LibraryImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static partial uint TimeBeginPeriod(uint uMilliseconds);

        #endregion
    }
}

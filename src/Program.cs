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
using LoneEftDmaRadar.UI;
using LoneEftDmaRadar.Web.TarkovDev;
using Microsoft.Extensions.DependencyInjection;
using Silk.NET.Input.Glfw;
using Silk.NET.Windowing.Glfw;
using System.Diagnostics.CodeAnalysis;
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
            try
            {
                VelopackApp.Build().Run();
                GlfwWindowing.RegisterPlatform();
                GlfwInput.RegisterPlatform();
                GlfwWindowing.Use();
                _mutex = new Mutex(true, MUTEX_ID, out bool singleton);
                if (!singleton)
                    throw new InvalidOperationException("The application is already running.");
                Config = EftDmaConfig.Load();
                ServiceProvider = BuildServiceProvider();
                HttpClientFactory = ServiceProvider.GetRequiredService<IHttpClientFactory>();
                SetHighPerformanceMode();
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
                var updater = new UpdateManager(
                    source: new GithubSource(
                        repoUrl: "https://github.com/lone-dma/Lone-EFT-DMA-Radar",
                        accessToken: null,
                        prerelease: false));
                if (updater.IsInstalled)
                {
                    _ = Task.Run(() => CheckForUpdatesAsync(updater)); // Initialize continuations on the thread pool
                }
            }
            catch (Exception ex)
            {
                HandleFatalError(ex);
            }
        }

        static void Main()
        {
            try
            {
                RadarWindow.Initialize();
                RadarWindow.Run();
            }
            catch (Exception ex)
            {
                HandleFatalError(ex);
            }
        }

        #region Boilerplate

        [DoesNotReturn]
        private static void HandleFatalError(Exception ex)
        {
            string error = $"FATAL ERROR -> {ex}";
            MessageBox.Show(error, Name, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxOptions.DefaultDesktopOnly);
            Environment.FailFast(error);
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
            Config.Save();
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
            if (SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_DISPLAY_REQUIRED) == 0)
                Logging.WriteLine($"WARNING: Unable to set Thread Execution State. This may cause performance issues. ERROR {Marshal.GetLastWin32Error()}");
            Guid highPerformanceGuid = new("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
            if (PowerSetActiveScheme(IntPtr.Zero, ref highPerformanceGuid) != 0)
                Logging.WriteLine($"WARNING: Unable to set High Performance Power Plan. This may cause performance issues. ERROR {Marshal.GetLastWin32Error()}");
            if (TimeBeginPeriod(5) != 0)
                Logging.WriteLine($"WARNING: Unable to set timer resolution to 5ms. This may cause performance issues. ERROR {Marshal.GetLastWin32Error()}");
            if (AvSetMmThreadCharacteristicsW("Games", out _) == 0)
                Logging.WriteLine($"WARNING: Unable to set Multimedia thread characteristics to 'Games'. This may cause performance issues. ERROR {Marshal.GetLastWin32Error()}");
        }

        private static async Task CheckForUpdatesAsync(UpdateManager updater)
        {
            try
            {
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

        [LibraryImport("kernel32.dll", SetLastError = true)]
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

        [LibraryImport("avrt.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
        private static partial IntPtr AvSetMmThreadCharacteristicsW(string taskName, out uint taskIndex);

        [LibraryImport("powrprof.dll", SetLastError = true)]
        private static partial uint PowerSetActiveScheme(IntPtr userRootPowerKey, ref Guid schemeGuid);

        [LibraryImport("winmm.dll", EntryPoint = "timeBeginPeriod", SetLastError = true)]
        private static partial uint TimeBeginPeriod(uint uMilliseconds);

        #endregion

        #region State Management

        private static volatile AppState _state = AppState.Initializing;
        /// <summary>
        /// Current Application State.
        /// </summary>
        public static AppState State => _state;

        /// <summary>
        /// Updates the current application state.
        /// </summary>
        /// <param name="newState">New state to be set.</param>
        /// <returns>Previous <see cref="AppState"/>.</returns>
        internal static AppState UpdateState(AppState newState)
        {
            return Interlocked.Exchange(ref _state, newState);
        }

        #endregion
    }
}

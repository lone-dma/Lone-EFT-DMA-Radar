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

global using EftDmaRadarLite.Common;
global using SDK;
global using SkiaSharp;
global using SkiaSharp.Views.Desktop;
global using System.Buffers;
global using System.Collections;
global using System.Collections.Concurrent;
global using System.ComponentModel;
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
global using System.Windows;
using EftDmaRadarLite.DMA;
using EftDmaRadarLite.Misc.Cache;
using EftDmaRadarLite.Tarkov.Data;
using EftDmaRadarLite.Tarkov.Data.ProfileApi.Providers;
using EftDmaRadarLite.UI.ColorPicker;
using EftDmaRadarLite.UI.Misc;
using EftDmaRadarLite.UI.Skia;
using EftDmaRadarLite.UI.Skia.Maps;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Versioning;
using System.Security.Authentication;
using System.Windows.Input;

[assembly: SupportedOSPlatform("Windows")]
[assembly: AssemblyVersion("1.0.*")]

namespace EftDmaRadarLite
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        internal const string Name = "EFT DMA Radar Lite";
        private const string MUTEX_ID = "0f908ff7-e614-6a93-60a3-cee36c9cea91";
        private static readonly Mutex _mutex;

        /// <summary>
        /// Path to the Configuration Folder in %AppData%
        /// </summary>
        public static DirectoryInfo ConfigPath { get; } =
            new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "eft-dma-radar-4e90"));
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
        /// <summary>
        /// TRUE if the application is currently using Dark Mode resources, otherwise FALSE for Light Mode.
        /// </summary>
        public static bool IsDarkMode { get; private set; }

        static App()
        {
            try
            {
                _mutex = new Mutex(true, MUTEX_ID, out bool singleton);
                if (!singleton)
                    throw new InvalidOperationException("The Application Is Already Running!");
                Config = EftDmaConfig.Load();
                ServiceProvider = BuildServiceProvider();
                HttpClientFactory = ServiceProvider.GetRequiredService<IHttpClientFactory>();
                SetHighPerformanceMode();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Name, MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);
                using var loading = new LoadingWindow();
                await ConfigureProgramAsync(loadingWindow: loading);
                MainWindow = new MainWindow();
                MainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Name, MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                Config.Save();
            }
            finally
            {
                base.OnExit(e);
            }
        }

        #region Boilerplate

        /// <summary>
        /// Configure Program Startup.
        /// </summary>
        private async Task ConfigureProgramAsync(LoadingWindow loadingWindow) =>
        await Task.Run(async () =>
        {
            await loadingWindow.ViewModel.UpdateProgressAsync(15, "Loading Tarkov.Dev Data...");
            await EftDataManager.ModuleInitAsync(loadingWindow);
            await loadingWindow.ViewModel.UpdateProgressAsync(35, "Loading Map Assets...");
            EftMapManager.ModuleInit();
            await loadingWindow.ViewModel.UpdateProgressAsync(50, "Starting DMA Connection...");
            MemoryInterface.ModuleInit();
            await loadingWindow.ViewModel.UpdateProgressAsync(75, "Loading Remaining Modules...");
            IsDarkMode = GetIsDarkMode();
            if (IsDarkMode)
            {
                SKPaints.PaintBitmap.ColorFilter = SKPaints.GetDarkModeColorFilter(0.7f);
                SKPaints.PaintBitmapAlpha.ColorFilter = SKPaints.GetDarkModeColorFilter(0.7f);
            }
            RuntimeHelpers.RunClassConstructor(typeof(LocalCache).TypeHandle);
            RuntimeHelpers.RunClassConstructor(typeof(ColorPickerViewModel).TypeHandle);
            await loadingWindow.ViewModel.UpdateProgressAsync(100, "Loading Completed!");
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        });

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Config.Save();
        }

        /// <summary>
        /// Sets up the Dependency Injection container for the application.
        /// </summary>
        /// <returns></returns>
        private static IServiceProvider BuildServiceProvider()
        {
            var services = new ServiceCollection();
            ConfigureHttpClientFactory(services);
            TarkovDevProvider.Configure(services);
            EftApiTechProvider.Configure(services);
            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Sets up the HttpClientFactory for the application.
        /// </summary>
        /// <param name="services"></param>
        private static void ConfigureHttpClientFactory(IServiceCollection services)
        {
            services.AddHttpClient("default", client =>
            {
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("identity"));
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler()
            {
                SslOptions = new()
                {
                    EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
                },
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.Brotli | DecompressionMethods.GZip | DecompressionMethods.Deflate
            })
            .AddStandardResilienceHandler(options =>
            {
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(100);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.SamplingDuration = options.AttemptTimeout.Timeout * 2;
            });
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
                Debug.WriteLine("WARNING: Unable to set High Performance Power Plan");
            const uint timerResolutionMs = 5;
            if (TimeBeginPeriod(timerResolutionMs) != 0)
                Debug.WriteLine($"WARNING: Unable to set timer resolution to {timerResolutionMs}ms. This may cause performance issues.");
        }

        /// <summary>
        /// Checks the current ResourceDictionaries to determine if Dark Mode or Light Mode is active.
        /// NOTE: Only works after App is initialized and resources are loaded.
        /// </summary>
        private static bool GetIsDarkMode()
        {
            try
            {
                foreach (var dict in Application.Current.Resources.MergedDictionaries)
                {
                    foreach (var inner in dict.MergedDictionaries)
                    {
                        if (inner.Source?.ToString() is string src)
                        {
                            if (src.Contains("/Theme/Dark.xaml", StringComparison.OrdinalIgnoreCase))
                                return true;
                            if (src.Contains("/Theme/Light.xaml", StringComparison.OrdinalIgnoreCase))
                                return false;
                        }
                    }
                }
            }
            catch { }
            // fallback: assume light if nothing matched
            return false;
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
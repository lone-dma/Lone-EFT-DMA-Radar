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
using EftDmaRadarLite.Tarkov.Player;
using EftDmaRadarLite.WebRadar.Data;
using EftDmaRadarLite.WebRadar.MessagePack;
using MessagePack;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Open.Nat;
using System.Net.Sockets;

namespace EftDmaRadarLite.WebRadar
{
    internal static class WebRadarServer
    {
        /// <summary>
        /// Password for this Server.
        /// </summary>
        public static string Password { get; } = Utilities.GetRandomPassword(10);

        /// <summary>
        /// Startup web server for Web Radar.
        /// </summary>
        /// <param name="ip">IP to bind to.</param>
        /// <param name="port">TCP Port to bind to.</param>
        /// <param name="tickRate">How often radar updates should be broadcast.</param>
        /// <param name="upnp">True if Port Forwarding should be setup via UPnP.</param>
        public static async Task StartAsync(string ip, int port, TimeSpan tickRate, bool upnp)
        {
            ThrowIfInvalidBindParameters(ip, port);
            if (upnp)
                await ConfigureUPnPAsync(port);
            var host = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseKestrel()
                    .ConfigureServices(services =>
                    {
                        services.AddSignalR()
                            .AddMessagePackProtocol(options =>
                            {
                                options.SerializerOptions = MessagePackSerializerOptions.Standard
                                    .WithSecurity(MessagePackSecurity.TrustedData)
                                    .WithCompression(MessagePackCompression.Lz4BlockArray)
                                    .WithResolver(ResolverGenerator.Instance);
                            });
                        services.AddCors(options =>
                        {
                            options.AddDefaultPolicy(builder =>
                            {
                                builder.AllowAnyOrigin()
                                        .AllowAnyHeader()
                                        .AllowAnyMethod()
                                        .SetIsOriginAllowedToAllowWildcardSubdomains();
                            });
                        });
                    })
                    .Configure(app =>
                    {
                        app.UseCors();
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapHub<RadarServerHub>("/hub/006d97cd-1ceb-4bc2-8cb1-3b8329ee3fdb");
                        });
                    })
                    .UseUrls($"http://{FormatIPForURL(ip)}:{port}");
            })
            .Build();

            await host.StartAsync();

            // Start the server worker
            _ = Task.Run(() => WorkerRoutineAsync(host, tickRate));
        }

        /// <summary>
        /// Get the External IP of the user running the Server.
        /// </summary>
        /// <returns>External WAN IP.</returns>
        public static async Task<string> GetExternalIPAsync()
        {
            try
            {
                return await QueryUPnPForIPAsync();
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        /// <summary>
        /// Web Radar Server Worker Thread.
        /// </summary>
        private static async Task WorkerRoutineAsync(IHost host, TimeSpan tickRate)
        {
            try
            {
                var update = new WebRadarUpdate();
                var hubContext = host.Services.GetRequiredService<IHubContext<RadarServerHub>>();
                using var timer = new PeriodicTimer(tickRate);
                while (await timer.WaitForNextTickAsync()) // Wait for specified interval to regulate Tick Rate
                {
                    try

                    {
                        if (Memory.InRaid && Memory.Players is IReadOnlyCollection<PlayerBase> players && players.Count > 0)
                        {
                            update.InGame = true;
                            update.MapID = Memory.MapID;
                            update.Players = players.Select(p => WebRadarPlayer.Create(p));
                        }
                        else
                        {
                            update.InGame = false;
                            update.MapID = null;
                            update.Players = null;
                        }
                        update.Version++;
                        await hubContext.Clients.All.SendAsync("RadarUpdate", update);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(MainWindow.Instance, $"WebRadarServer Worker Thread Crashed:\n{ex}", "Web Radar Server", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Checks if the specified IP Address / Port Number are valid, and throws an exception if they are invalid.
        /// Performs a TCP Bind Test.
        /// </summary>
        /// <param name="ip">IP to test bind.</param>
        /// <param name="port">Port to test bind.</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="Exception"></exception>
        private static void ThrowIfInvalidBindParameters(string ip, int port)
        {
            try
            {
                if (port is < 1024 or > 65535)
                    throw new ArgumentException("Invalid Port. We recommend using a Port between 50000-60000.");
                var ipObj = IPAddress.Parse(ip);
                using var socket = new Socket(ipObj.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.Bind(new IPEndPoint(ipObj, port));
                socket.Close();
            }
            catch (SocketException ex)
            {
                throw new InvalidOperationException($"Invalid Bind Parameters. Use a valid Bind IP (ex: 0.0.0.0), and a port number between 50000-60000.\n" +
                    $"SocketException: {ex.Message}");
            }
        }

        /// <summary>
        /// Formats an IP Host string for use in a URL.
        /// </summary>
        /// <param name="host">IP/Hostname to check/format.</param>
        /// <returns>Formatted IP, or original string if no formatting is needed.</returns>
        private static string FormatIPForURL(string host)
        {
            if (host is null)
                return null;
            if (IPAddress.TryParse(host, out var ip) && ip.AddressFamily is AddressFamily.InterNetworkV6)
                return $"[{host}]";
            return host;
        }

        /// <summary>
        /// Get the Nat Device for the local UPnP Service.
        /// </summary>
        /// <returns>Task with NatDevice object.</returns>
        private async static Task<NatDevice> GetNatDeviceAsync()
        {
            var dsc = new NatDiscoverer();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            return await dsc.DiscoverDeviceAsync(PortMapper.Upnp, cts);
        }

        /// <summary>
        /// Attempts to setup UPnP Port Forwarding for the specified port.
        /// </summary>
        /// <param name="port">Port to forward.</param>
        /// <returns>Task with result of operation.</returns>
        /// <exception cref="Exception"></exception>
        private static async Task ConfigureUPnPAsync(int port)
        {
            try
            {
                var upnp = await GetNatDeviceAsync();

                // Create New Mapping
                await upnp.CreatePortMapAsync(new Mapping(
                    protocol: Protocol.Tcp, 
                    privatePort: port, 
                    publicPort: port, 
                    lifetime: 86400, 
                    description: ""));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"ERROR Setting up UPnP: {ex.Message}");
            }
        }

        /// <summary>
        /// Lookup the External IP Address via UPnP.
        /// </summary>
        /// <returns>External IP Address.</returns>
        private static async Task<string> QueryUPnPForIPAsync()
        {
            var upnp = await GetNatDeviceAsync();
            var ip = await upnp.GetExternalIPAsync();
            return ip.ToString();
        }

        private sealed class RadarServerHub : Hub
        {
            public override async Task OnConnectedAsync()
            {
                var httpContext = Context.GetHttpContext();

                string password = httpContext?.Request?.Query?["password"].ToString() ?? "";
                if (password != Password)
                {
                    Context.Abort();
                    return;
                }

                await base.OnConnectedAsync();
            }
        }

    }
}

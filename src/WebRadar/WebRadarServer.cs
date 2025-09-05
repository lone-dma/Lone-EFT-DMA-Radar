﻿using eft_dma_radar.Misc;
using eft_dma_radar.Tarkov.Player;
using eft_dma_radar.WebRadar.Data;
using eft_dma_radar.WebRadar.MessagePack;
using MessagePack;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Open.Nat;
using System.Net.Sockets;

namespace eft_dma_radar.WebRadar
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
                            services.AddSignalR(options =>
                            {
                                options.MaximumReceiveMessageSize = 1024 * 128; // Set the maximum message size to 128KB
                            })
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

            host.Start();

            // Start the server worker
            _ = WorkerRoutineAsync(host, tickRate).ConfigureAwait(false);
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
        private static async Task WorkerRoutineAsync(IHost host, TimeSpan tickrate)
        {
            try
            {
                var update = new WebRadarUpdate();
                var hubContext = host.Services.GetRequiredService<IHubContext<RadarServerHub>>();
                using var timer = new PeriodicTimer(tickrate);
                while (await timer.WaitForNextTickAsync().ConfigureAwait(false)) // Wait for specified interval to regulate Tick Rate
                {
                    try
                    {
                        if (Memory.InRaid && Memory.Players is IReadOnlyCollection<PlayerBase> players && players.Count > 0)
                        {
                            update.InGame = true;
                            update.MapID = Memory.MapID;
                            update.Players = players.Select(p => WebRadarPlayer.CreateFromPlayer(p));
                        }
                        else
                        {
                            update.InGame = false;
                            update.MapID = null;
                            update.Players = null;
                        }
                        update.Version++;
                        await hubContext.Clients.All.SendAsync("RadarUpdate", update).ConfigureAwait(false);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WebRadarServer Worker Thread Crashed:\n{ex}", "Web Radar Server", MessageBoxButton.OK, MessageBoxImage.Error);
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
                throw new Exception($"Invalid Bind Parameters. Use your Radar PC's Local LAN IP (example: 192.168.1.100), and a port number between 50000-60000.\n" +
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
                await upnp.CreatePortMapAsync(new Mapping(Protocol.Tcp, port, port, 86400, "Eft Web Radar"));
            }
            catch (Exception ex)
            {
                throw new Exception($"ERROR Setting up UPnP: {ex.Message}");
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

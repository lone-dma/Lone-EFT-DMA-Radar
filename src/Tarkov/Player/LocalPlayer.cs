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

using EftDmaRadarLite.Tarkov.Data;
using EftDmaRadarLite.Unity;
using EftDmaRadarLite.Unity.Collections;
using EftDmaRadarLite.Unity.Structures;
using VmmSharpEx;
using VmmSharpEx.Scatter;

namespace EftDmaRadarLite.Tarkov.Player
{
    public sealed class LocalPlayer : ClientPlayer
    {
        public static ulong HandsController { get; private set; }
        /// <summary>
        /// All Items on the Player's WishList.
        /// </summary>
        public static IReadOnlySet<string> WishlistItems => _wishlistItems;
        private static readonly HashSet<string> _wishlistItems = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Spawn Point.
        /// </summary>
        public string EntryPoint { get; }
        /// <summary>
        /// Profile ID (if Player Scav).
        /// Used for Exfils.
        /// </summary>
        public string ProfileId { get; }
        /// <summary>
        /// Player name.
        /// </summary>
        public override string Name
        {
            get => "localPlayer";
            set { }
        }
        /// <summary>
        /// Player is Human-Controlled.
        /// </summary>
        public override bool IsHuman { get; }

        public LocalPlayer(ulong playerBase) : base(playerBase)
        {
            string classType = ObjectClass.ReadName(this);
            if (!(classType == "LocalPlayer" || classType == "ClientPlayer"))
                throw new ArgumentOutOfRangeException(nameof(classType));
            IsHuman = true;
            if (IsPmc)
            {
                var entryPtr = Memory.ReadPtr(Info + Offsets.PlayerInfo.EntryPoint);
                EntryPoint = Memory.ReadUnityString(entryPtr);
            }
            else if (IsScav)
            {
                var profileIdPtr = Memory.ReadPtr(Profile + Offsets.Profile.Id);
                ProfileId = Memory.ReadUnityString(profileIdPtr);
            }
        }

        /// <summary>
        /// Set the Player's WishList.
        /// </summary>
        public void RefreshWishlist(CancellationToken ct)
        {
            try
            {
                var wishlistManager = Memory.ReadPtr(Profile + Offsets.Profile.WishlistManager);
                var itemsPtr = Memory.ReadPtr(wishlistManager + Offsets.WishlistManager.Items);
                using var items = UnityDictionary<MongoID, int>.Create(itemsPtr, true);
                var wishlist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in items)
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        if (item.Key.StringID == 0)
                            continue;
                        string id = Memory.ReadUnityString(item.Key.StringID);
                        if (string.IsNullOrWhiteSpace(id))
                            continue;
                        wishlist.Add(id);
                    }
                    catch { }
                }
                foreach (var existing in _wishlistItems)
                {
                    ct.ThrowIfCancellationRequested();
                    if (!wishlist.Contains(existing))
                        _wishlistItems.Remove(existing);
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Wishlist] ERROR Refreshing: {ex}");
            }
        }

        /// <summary>
        /// Additional realtime reads for LocalPlayer.
        /// </summary>
        /// <param name="index"></param>
        public override void OnRealtimeLoop(ScatterReadIndex index, bool espRunning)
        {
            index.AddValueEntry<VmmPointer>(-11, HandsControllerAddr);
            index.Completed += (sender, x1) =>
            {
                if (x1.TryGetValue<VmmPointer>(-11, out var handsController))
                    LocalPlayer.HandsController = handsController;
            };
            base.OnRealtimeLoop(index, espRunning);
        }

        /// <summary>
        /// Get View Angles for LocalPlayer.
        /// </summary>
        /// <returns>View Angles (Vector2).</returns>
        public Vector2 GetViewAngles() =>
            Memory.ReadValue<Vector2>(RotationAddress, false);

        /// <summary>
        /// Checks if LocalPlayer is Aiming (ADS).
        /// </summary>
        /// <returns>True if aiming (ADS), otherwise False.</returns>
        public bool CheckIfADS()
        {
            try
            {
                return Memory.ReadValue<bool>(PWA + Offsets.ProceduralWeaponAnimation._isAiming, false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CheckIfADS() ERROR: {ex}");
                return false;
            }
        }
    }
}

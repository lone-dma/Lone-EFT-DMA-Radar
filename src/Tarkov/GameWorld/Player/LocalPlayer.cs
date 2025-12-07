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
using LoneEftDmaRadar.Tarkov.Unity;
using LoneEftDmaRadar.Tarkov.Unity.Collections;
using LoneEftDmaRadar.Tarkov.Unity.Structures;
using VmmSharpEx.Scatter;

namespace LoneEftDmaRadar.Tarkov.GameWorld.Player
{
    public sealed class LocalPlayer : ClientPlayer
    {
        /// <summary>
        /// All Items on the Player's WishList.
        /// </summary>
        public static IReadOnlySet<string> WishlistItems => _wishlistItems;
        private static readonly HashSet<string> _wishlistItems = new(StringComparer.OrdinalIgnoreCase);
        private UnityTransform _lookRaycastTransform;

        /// <summary>
        /// Local Player's 'Look' position.
        /// Useful for proper POV on Aimview,etc.
        /// </summary>
        /// <remarks>
        /// Will failover to root position if there is no Look Pos.
        /// </remarks>
        public Vector3 LookPosition => _lookRaycastTransform?.Position ?? this.Position;

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
        }

        private static DateTimeOffset _wishlistLast = DateTimeOffset.MinValue;
        /// <summary>
        /// Set the Player's WishList.
        /// </summary>
        public void RefreshWishlist(CancellationToken ct)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                if ((now - _wishlistLast).TotalSeconds < 10d)
                    return;
                var wishlistManager = Memory.ReadPtr(Profile + Offsets.Profile.WishlistManager);
                var itemsPtr = Memory.ReadPtr(wishlistManager + Offsets.WishlistManager._wishlistItems);
                using var items = UnityDictionary<MongoID, int>.Create(itemsPtr, true);
                using var wishlist = new PooledSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in items)
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        string id = item.Key.ReadString();
                        if (string.IsNullOrWhiteSpace(id))
                            continue;
                        wishlist.Add(id);
                    }
                    catch { throw; }
                }
                foreach (var existing in _wishlistItems)
                {
                    ct.ThrowIfCancellationRequested();
                    if (!wishlist.Contains(existing))
                        _wishlistItems.Remove(existing);
                }
                foreach (var newItem in wishlist)
                {
                    ct.ThrowIfCancellationRequested();
                    _wishlistItems.Add(newItem);
                }
                _wishlistLast = now;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Wishlist] ERROR Refreshing: {ex}");
            }
        }

        public override void OnRealtimeLoop(VmmScatter scatter)
        {
            try
            {
                if (App.Config.AimviewWidget.Enabled)
                {
                    _lookRaycastTransform ??= new UnityTransform(
                        transformInternal: Memory.ReadPtrChain(Memory.ReadPtr(this + Offsets.Player._playerLookRaycastTransform), true, 0x10),
                        useCache: false);
                    scatter.PrepareReadArray<UnityTransform.TrsX>(_lookRaycastTransform.VerticesAddr, _lookRaycastTransform.Count);
                    scatter.Completed += (sender, s) =>
                    {
                        try
                        {
                            if (s.ReadPooled<UnityTransform.TrsX>(_lookRaycastTransform.VerticesAddr, _lookRaycastTransform.Count) is IMemoryOwner<UnityTransform.TrsX> vertices)
                            {
                                using (vertices)
                                {
                                    _ = _lookRaycastTransform.UpdatePosition(vertices.Memory.Span);
                                }
                            }
                            else
                            {
                                throw new InvalidOperationException("Failed to set LookRaycastTransform pos.");
                            }
                        }
                        catch
                        {
                            _lookRaycastTransform = null;
                        }
                    };
                }
            }
            catch
            {
                _lookRaycastTransform = null;
            }
            finally
            {
                base.OnRealtimeLoop(scatter);
            }
        }

        public override void OnValidateTransforms(VmmScatter round1, VmmScatter round2)
        {
            try
            {
                if (App.Config.AimviewWidget.Enabled && _lookRaycastTransform is UnityTransform existing)
                {
                    round1.PrepareReadPtr(existing.TransformInternal + UnitySDK.UnityOffsets.TransformAccess_HierarchyOffset); // Transform Hierarchy
                    round1.Completed += (sender, s1) =>
                    {
                        if (s1.ReadPtr(existing.TransformInternal + UnitySDK.UnityOffsets.TransformAccess_HierarchyOffset, out var tra))
                        {
                            round2.PrepareReadPtr(tra + UnitySDK.UnityOffsets.Hierarchy_VerticesOffset); // Vertices Ptr
                            round2.Completed += (sender, s2) =>
                            {
                                if (s2.ReadPtr(tra + UnitySDK.UnityOffsets.Hierarchy_VerticesOffset, out var verticesPtr))
                                {
                                    if (existing.VerticesAddr != verticesPtr) // check if any addr changed
                                    {
                                        Debug.WriteLine($"WARNING - '_lookRaycastTransform' Transform has changed for LocalPlayer '{Name}'");
                                        var transform = new UnityTransform(existing.TransformInternal);
                                        _lookRaycastTransform = transform;
                                    }
                                }
                            };
                        }
                    };
                }
            }
            finally
            {
                base.OnValidateTransforms(round1, round2);
            }
        }
    }
}

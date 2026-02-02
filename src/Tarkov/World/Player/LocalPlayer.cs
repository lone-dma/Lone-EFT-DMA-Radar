/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
using Collections.Pooled;
using LoneEftDmaRadar.Tarkov.Unity.Collections;
using LoneEftDmaRadar.Tarkov.Unity.Structures;
using VmmSharpEx.Scatter;

namespace LoneEftDmaRadar.Tarkov.World.Player
{
    public sealed class LocalPlayer : ClientPlayer
    {
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
        public override string Name => "localPlayer";
        /// <summary>
        /// Player is Human-Controlled.
        /// </summary>
        public override bool IsHuman => true;

        public LocalPlayer(ulong playerBase) : base(playerBase)
        {
            string classType = ObjectClass.ReadName(this);
            if (!(classType == "LocalPlayer" || classType == "ClientPlayer"))
                throw new ArgumentOutOfRangeException(nameof(classType));
        }

        /// <summary>
        /// Check if the Raid has started for the LocalPlayer.
        /// Does not throw.
        /// </summary>
        /// <returns>True if the Raid has started, otherwise false. NULL if an error occurred.</returns>
        public bool? CheckIsRaidStarted()
        {
            try
            {
                ulong handsController = Memory.ReadPtr(this + Offsets.Player._handsController, false);
                string handsType = ObjectClass.ReadName(
                    objectClass: handsController,
                    useCache: false);
                ArgumentNullException.ThrowIfNull(handsType, nameof(handsType));
                if (!handsType.Contains("Controller"))
                    throw new ArgumentException("HandsController type invalid.", nameof(handsType));
                return handsType != "ClientEmptyHandsController";
            }
            catch (Exception ex)
            {
                Logging.WriteLine($"[LocalPlayer] ERROR Checking IsRaidStarted: {ex}");
                return null;
            }
        }

        public override void OnRealtimeLoop(VmmScatterManaged scatter)
        {
            try
            {
                if (Config.AimviewWidget.Enabled)
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

        public override void OnValidateTransforms(VmmScatterManaged round1, VmmScatterManaged round2)
        {
            try
            {
                if (Config.AimviewWidget.Enabled && _lookRaycastTransform is UnityTransform existing)
                {
                    round1.PrepareReadPtr(existing.TransformInternal + UnityOffsets.TransformAccess_HierarchyOffset); // Transform Hierarchy
                    round1.Completed += (sender, s1) =>
                    {
                        if (s1.ReadPtr(existing.TransformInternal + UnityOffsets.TransformAccess_HierarchyOffset, out var tra))
                        {
                            round2.PrepareReadPtr(tra + UnityOffsets.Hierarchy_VerticesOffset); // Vertices Ptr
                            round2.Completed += (sender, s2) =>
                            {
                                if (s2.ReadPtr(tra + UnityOffsets.Hierarchy_VerticesOffset, out var verticesPtr))
                                {
                                    if (existing.VerticesAddr != verticesPtr) // check if any addr changed
                                    {
                                        Logging.WriteLine($"WARNING - '_lookRaycastTransform' Transform has changed for LocalPlayer '{Name}'");
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

        #region Wishlist

        /// <summary>
        /// All TarkovDevItems on the Player's WishList.
        /// </summary>
        public static IReadOnlyDictionary<string, byte> WishlistItems => _wishlistItems;
        private static readonly ConcurrentDictionary<string, byte> _wishlistItems = new(StringComparer.OrdinalIgnoreCase);
        private static readonly RateLimiter _wishlistRL = new(TimeSpan.FromSeconds(10));

        /// <summary>
        /// Set the Player's WishList.
        /// </summary>
        public void RefreshWishlist(CancellationToken ct)
        {
            try
            {
                if (!_wishlistRL.TryEnter())
                    return;

                var wishlistManager = Memory.ReadPtr(Profile + Offsets.Profile.WishlistManager);
                var itemsPtr = Memory.ReadPtr(wishlistManager + Offsets.WishlistManager._wishlistItems);
                using var items = UnityDictionary<MongoID, int>.Create(itemsPtr);
                using var newWishlist = new PooledSet<string>(items.Count, StringComparer.OrdinalIgnoreCase);

                foreach (var item in items)
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        newWishlist.Add(item.Key.ReadString());
                    }
                    catch { }
                }

                foreach (var existing in _wishlistItems.Keys)
                {
                    if (!newWishlist.Contains(existing))
                        _wishlistItems.TryRemove(existing, out _);
                }

                foreach (var newItem in newWishlist)
                {
                    _wishlistItems.TryAdd(newItem, 0);
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Logging.WriteLine($"[Wishlist] ERROR Refreshing: {ex}");
            }
        }

        #endregion
    }
}


/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
using LoneEftDmaRadar.Tarkov.Unity.Collections;

namespace LoneEftDmaRadar.Tarkov.World.Explosives
{
    public sealed class ExplosivesManager : IReadOnlyCollection<IExplosiveItem>
    {
        private static readonly uint[] _toSyncObjects = [
            Offsets.GameWorld.SynchronizableObjectLogicProcessor,
            Offsets.SynchronizableObjectLogicProcessor._staticSynchronizableObjects];
        private readonly ulong _gameWorld;
        private readonly ConcurrentDictionary<ulong, IExplosiveItem> _explosives = new();

        public ExplosivesManager(ulong gameWorld)
        {
            _gameWorld = gameWorld;
        }

        /// <summary>
        /// Check for "hot" explosives in World if due.
        /// </summary>
        public void Refresh(CancellationToken ct)
        {
            GetGrenades(ct);
            GetTripwires(ct);
            var explosives = _explosives.Values;
            if (explosives.Count == 0)
            {
                return;
            }
            using var scatter = Memory.CreateScatter(VmmSharpEx.Options.VmmFlags.NOCACHE);
            foreach (var explosive in explosives)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    explosive.OnRefresh(scatter);
                }
                catch (Exception ex)
                {
                    Logging.WriteLine($"Error Refreshing Explosive @ 0x{explosive.Addr.ToString("X")}: {ex}");
                }
            }
            scatter.Execute();
        }

        private void GetGrenades(CancellationToken ct)
        {
            try
            {
                var grenades = Memory.ReadPtr(_gameWorld + Offsets.GameWorld.Grenades);
                var grenadesListPtr = Memory.ReadPtr(grenades + 0x18);
                using var grenadesList = UnityList<ulong>.Create(grenadesListPtr, false);
                foreach (var grenade in grenadesList)
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        _ = _explosives.GetOrAdd(
                            grenade,
                            addr => new Grenade(addr, _explosives));
                    }
                    catch (Exception ex)
                    {
                        Logging.WriteLine($"Error Processing Grenade @ 0x{grenade.ToString("X")}: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.WriteLine($"Grenades Error: {ex}");
            }
        }

        private void GetTripwires(CancellationToken ct)
        {
            try
            {
                var syncObjectsPtr = Memory.ReadPtrChain(_gameWorld, true, _toSyncObjects);
                using var syncObjects = UnityList<ulong>.Create(syncObjectsPtr, true);
                foreach (var syncObject in syncObjects)
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        var type = (Enums.SynchronizableObjectType)Memory.ReadValue<int>(syncObject + Offsets.SynchronizableObject.Type);
                        if (type is not Enums.SynchronizableObjectType.Tripwire)
                            continue;
                        _ = _explosives.GetOrAdd(
                            syncObject,
                            addr => new Tripwire(addr));
                    }
                    catch (Exception ex)
                    {
                        Logging.WriteLine($"Error Processing SyncObject @ 0x{syncObject.ToString("X")}: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.WriteLine($"Sync Objects Error: {ex}");
            }
        }

        #region IReadOnlyCollection

        public int Count => _explosives.Values.Count;
        public IEnumerator<IExplosiveItem> GetEnumerator() => _explosives.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion
    }
}

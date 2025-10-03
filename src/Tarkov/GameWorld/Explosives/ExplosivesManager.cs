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

using EftDmaRadarLite.Unity.Mono.Collections;

namespace EftDmaRadarLite.Tarkov.GameWorld.Explosives
{
    public sealed class ExplosivesManager : IReadOnlyCollection<IExplosiveItem>
    {
        private static readonly uint[] _toSyncObjects = new[] { Offsets.ClientLocalGameWorld.SynchronizableObjectLogicProcessor, Offsets.SynchronizableObjectLogicProcessor.SynchronizableObjects };
        private readonly ulong _localGameWorld;
        private readonly ConcurrentDictionary<ulong, IExplosiveItem> _explosives = new();

        public ExplosivesManager(ulong localGameWorld)
        {
            _localGameWorld = localGameWorld;
        }

        /// <summary>
        /// Check for "hot" explosives in LocalGameWorld if due.
        /// </summary>
        public void Refresh(CancellationToken ct)
        {
            GetGrenades(ct);
            GetTripwires(ct);
            GetMortarProjectiles(ct);
            var explosives = _explosives.Values;
            if (explosives.Count == 0)
            {
                return;
            }
            using var map = Memory.CreateScatterMap();  
            var rd1 = map.AddRound(useCache: false);
            int i = 0;
            foreach (var explosive in explosives)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    explosive.OnRefresh(rd1[i++]);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error Refreshing Explosive @ 0x{explosive.Addr.ToString("X")}: {ex}");
                }
            }
            map.Execute();
        }

        private void GetGrenades(CancellationToken ct)
        {
            try
            {
                var grenades = Memory.ReadPtr(_localGameWorld + Offsets.ClientLocalGameWorld.Grenades);
                var grenadesListPtr = Memory.ReadPtr(grenades + 0x18);
                using var grenadesList = MonoList<ulong>.Create(grenadesListPtr, false);
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
                        Debug.WriteLine($"Error Processing Grenade @ 0x{grenade.ToString("X")}: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Grenades Error: {ex}");
            }
        }

        private void GetTripwires(CancellationToken ct)
        {
            try
            {
                var syncObjectsPtr = Memory.ReadPtrChain(_localGameWorld, true, _toSyncObjects);
                using var syncObjects = MonoList<ulong>.Create(syncObjectsPtr, true);
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
                        Debug.WriteLine($"Error Processing SyncObject @ 0x{syncObject.ToString("X")}: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Sync Objects Error: {ex}");
            }
        }

        private void GetMortarProjectiles(CancellationToken ct)
        {
            try
            {
                var clientShellingController = Memory.ReadPtr(_localGameWorld + Offsets.ClientLocalGameWorld.ClientShellingController);
                var activeProjectilesPtr = Memory.ReadPtr(clientShellingController + Offsets.ClientShellingController.ActiveClientProjectiles);
                using var activeProjectiles = MonoDictionary<int, ulong>.Create(activeProjectilesPtr, true);
                foreach (var activeProjectile in activeProjectiles)
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        _ = _explosives.GetOrAdd(
                            activeProjectile.Value,
                            addr => new MortarProjectile(addr, _explosives));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error Processing Mortar Projectile @ 0x{activeProjectile.Value.ToString("X")}: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Mortar Projectiles Error: {ex}");
            }
        }

        #region IReadOnlyCollection

        public int Count => _explosives.Values.Count;
        public IEnumerator<IExplosiveItem> GetEnumerator() => _explosives.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion
    }
}
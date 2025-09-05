using EftDmaRadarLite.Misc;
using EftDmaRadarLite.Unity.Collections;

namespace EftDmaRadarLite.Tarkov.GameWorld.Explosives
{
    public sealed class ExplosivesManager : IReadOnlyCollection<IExplosiveItem>
    {
        private static readonly uint[] _toSyncObjects = new[] { Offsets.ClientLocalGameWorld.SynchronizableObjectLogicProcessor, Offsets.SynchronizableObjectLogicProcessor.SynchronizableObjects };
        private readonly ulong _localGameWorld;
        private readonly ConcurrentDictionary<ulong, IExplosiveItem> _explosives = new();
        private ulong _grenadesBase;

        public ExplosivesManager(ulong localGameWorld)
        {
            _localGameWorld = localGameWorld;
        }

        private void Init()
        {
            var grenadesPtr = Memory.ReadPtr(_localGameWorld + Offsets.ClientLocalGameWorld.Grenades, false);
            _grenadesBase = Memory.ReadPtr(grenadesPtr + 0x18, false);
        }

        /// <summary>
        /// Check for "hot" explosives in LocalGameWorld if due.
        /// </summary>
        public void Refresh(CancellationToken ct)
        {
            foreach (var explosive in _explosives.Values)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    explosive.Refresh();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error Refreshing Explosive @ 0x{explosive.Addr.ToString("X")}: {ex}");
                }
            }
            GetGrenades(ct);
            GetTripwires(ct);
            GetMortarProjectiles(ct);
        }

        private void GetGrenades(CancellationToken ct)
        {
            try
            {
                if (_grenadesBase == 0x0)
                {
                    Init();
                }
                using var allGrenades = new UnityList<ulong>(_grenadesBase, false);
                foreach (var grenadeAddr in allGrenades)
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        if (!_explosives.ContainsKey(grenadeAddr))
                        {
                            var grenade = new Grenade(grenadeAddr, _explosives);
                            _explosives[grenade] = grenade;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error Processing Grenade @ 0x{grenadeAddr.ToString("X")}: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                _grenadesBase = 0x0;
                Debug.WriteLine($"Grenades Error: {ex}");
            }
        }

        private void GetTripwires(CancellationToken ct)
        {
            try
            {
                var syncObjectsPtr = Memory.ReadPtrChain(_localGameWorld, _toSyncObjects);
                using var syncObjects = new UnityList<ulong>(syncObjectsPtr, true);
                foreach (var syncObject in syncObjects)
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        var type = (Enums.SynchronizableObjectType)Memory.ReadValue<int>(syncObject + Offsets.SynchronizableObject.Type);
                        if (type is not Enums.SynchronizableObjectType.Tripwire)
                            continue;
                        if (!_explosives.ContainsKey(syncObject))
                        {
                            var tripwire = new Tripwire(syncObject);
                            _explosives[tripwire] = tripwire;
                        }
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
                var clientShellingController = Memory.ReadValue<ulong>(_localGameWorld + Offsets.ClientLocalGameWorld.ClientShellingController);
                if (clientShellingController != 0x0)
                {
                    var activeProjectilesPtr = Memory.ReadValue<ulong>(clientShellingController + Offsets.ClientShellingController.ActiveClientProjectiles);
                    if (activeProjectilesPtr != 0x0)
                    {
                        using var activeProjectiles = new UnityDictionary<int, ulong>(activeProjectilesPtr, true);
                        foreach (var activeProjectile in activeProjectiles)
                        {
                            ct.ThrowIfCancellationRequested();
                            if (activeProjectile.Value == 0x0)
                                continue;
                            try
                            {
                                if (!_explosives.ContainsKey(activeProjectile.Value))
                                {
                                    var mortarProjectile = new MortarProjectile(activeProjectile.Value, _explosives);
                                    _explosives[mortarProjectile] = mortarProjectile;
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error Processing Mortar Projectile @ 0x{activeProjectile.Value.ToString("X")}: {ex}");
                            }
                        }
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
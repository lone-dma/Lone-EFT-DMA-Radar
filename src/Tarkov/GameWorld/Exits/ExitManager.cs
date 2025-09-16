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
using EftDmaRadarLite.Unity.Collections;
using VmmSharpEx.Scatter;

namespace EftDmaRadarLite.Tarkov.GameWorld.Exits
{
    /// <summary>
    /// List of PMC/Scav 'Exits' in Local Game World and their position/status.
    /// </summary>
    public sealed class ExitManager : IReadOnlyCollection<IExitPoint>
    {
        private readonly ulong _localGameWorld;
        private readonly bool _isPMC;
        private IReadOnlyList<IExitPoint> _exits;

        public ExitManager(ulong localGameWorld, bool isPMC)
        {
            _localGameWorld = localGameWorld;
            _isPMC = isPMC;
        }

        /// <summary>
        /// Initialize ExfilManager.
        /// </summary>
        private void Init()
        {
            var list = new List<IExitPoint>();
            var exfilController = Memory.ReadPtr(_localGameWorld + Offsets.ClientLocalGameWorld.ExfilController, false);
            /// Regular Exfils
            var exfilArrOffset = _isPMC ?
                Offsets.ExfilController.ExfiltrationPointArray : Offsets.ExfilController.ScavExfiltrationPointArray;
            var exfilPoints = Memory.ReadPtr(exfilController + exfilArrOffset, false);
            using var exfils = UnityArray<ulong>.Create(exfilPoints, false);
            ArgumentOutOfRangeException.ThrowIfZero(exfils.Count, nameof(exfils));
            foreach (var exfilAddr in exfils)
            {
                var exfil = new Exfil(exfilAddr, _isPMC);
                list.Add(exfil);
            }
            /// Secret Exfils
            var secretExfilPoints = Memory.ReadValue<ulong>(exfilController + Offsets.ExfilController.SecretExfiltrationPointArray, false);
            if (secretExfilPoints.IsValidVirtualAddress())
            {
                using var secretExfils = UnityArray<ulong>.Create(secretExfilPoints, false);
                foreach (var secretExfil in secretExfils)
                {
                    var exfil = new SecretExfil(secretExfil);
                    list.Add(exfil);
                }
            }
            /// Transits
            var transitController = Memory.ReadPtr(_localGameWorld + Offsets.ClientLocalGameWorld.TransitController, false);
            var transitsPtr = Memory.ReadPtr(transitController + Offsets.TransitController.TransitPoints, false);
            using var transits = UnityDictionary<ulong, ulong>.Create(transitsPtr, false);
            foreach (var dTransit in transits)
            {
                var transit = new TransitPoint(dTransit.Value);
                list.Add(transit);
            }

            _exits = list; // update readonly ref
        }

        /// <summary>
        /// Updates exfil statuses.
        /// </summary>
        public void Refresh()
        {
            try
            {
                if (_exits is null) // Initialize
                    Init();
                ArgumentNullException.ThrowIfNull(_exits, nameof(_exits));
                using var map = Memory.GetScatterMap();
                var round1 = map.AddRound();
                for (int ix = 0; ix < _exits.Count; ix++)
                {
                    int i = ix;
                    var entry = _exits[i];
                    if (entry is Exfil exfil)
                    {
                        round1[i].AddValueEntry<int>(0, exfil + Offsets.Exfil._status);
                        round1[i].Completed += (sender, index) =>
                        {
                            if (index.TryGetValue<int>(0, out var status))
                            {
                                exfil.Update((Enums.EExfiltrationStatus)status);
                            }
                        };
                    }
                }
                map.Execute();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ExitManager] Refresh Error: {ex}");
            }
        }

        #region IReadOnlyCollection

        public int Count => _exits?.Count ?? 0;
        public IEnumerator<IExitPoint> GetEnumerator() => _exits?.GetEnumerator() ?? Enumerable.Empty<IExitPoint>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion
    }
}
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

namespace LoneEftDmaRadar.Tarkov.GameWorld.Exits
{
    /// <summary>
    /// List of PMC/Scav 'Exits' in Local Game World and their position/status.
    /// </summary>
    public sealed class ExitManager : IReadOnlyCollection<IExitPoint>
    {
        private readonly IReadOnlyList<IExitPoint> _exits;

        public ExitManager(string mapId, bool isPMC)
        {
            var list = new List<IExitPoint>();
            if (TarkovDataManager.MapData.TryGetValue(mapId, out var map))
            {
                var filteredExfils = isPMC ?
                    map.Extracts.Where(x => x.IsShared || x.IsPmc) :
                    map.Extracts.Where(x => !x.IsPmc);
                foreach (var exfil in filteredExfils)
                {
                    list.Add(new Exfil(exfil));
                }
                foreach (var transit in map.Transits)
                {
                    list.Add(new TransitPoint(transit));
                }
            }

            _exits = list;
        }

        #region IReadOnlyCollection

        public int Count => _exits?.Count ?? 0;
        public IEnumerator<IExitPoint> GetEnumerator() => _exits?.GetEnumerator() ?? Enumerable.Empty<IExitPoint>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion
    }
}
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

using MessagePack;

namespace EftDmaRadarLite.WebRadar.Data
{
    [MessagePackObject]
    public sealed class WebRadarUpdate
    {
        /// <summary>
        /// Update version (used for ordering).
        /// </summary>
        [Key(0)]
        public ulong Version { get; set; } = 0;
        /// <summary>
        /// True if In-Game, otherwise False.
        /// </summary>
        [Key(1)]
        public bool InGame { get; set; } = false;
        /// <summary>
        /// Contains the Map ID of the current map.
        /// </summary>
        [Key(2)]
        public string MapID { get; set; } = null;
        /// <summary>
        /// All Players currently on the map.
        /// </summary>
        [Key(3)]
        public IEnumerable<WebRadarPlayer> Players { get; set; } = null;
    }
}

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

using System.Collections.Frozen;

namespace LoneArenaDmaRadar.Arena
{
    /// <summary>
    /// Contains Static Game Data.
    /// </summary>
    internal static class StaticGameData
    {
        /// <summary>
        /// All Map Names by their Map ID.
        /// </summary>
        public static FrozenDictionary<string, string> MapNames { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["default"] = "default",
            ["Arena_RailwayStation"] = "Skybridge",
            ["Arena_AirPit"] = "Air pit",
            ["Arena_equator_TDM_02"] = "Equator",
            ["Arena_Bowl"] = "Bowl",
            ["Arena_saw"] = "Sawmill",
            ["Arena_Bay5"] = "Bay 5",
            ["Arena_AutoService"] = "Chop Shop",
            ["Arena_Yard"] = "Yard",
            ["Arena_Prison"] = "Fort",
            ["Arena_Iceberg"] = "Iceberg"
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }
}

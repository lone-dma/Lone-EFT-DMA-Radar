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
using LoneEftDmaRadar.Tarkov.GameWorld.Loot;

namespace LoneEftDmaRadar.UI.Loot
{
    /// <summary>
    /// Enumerable Loot Filter Class.
    /// </summary>
    internal static class LootFilter
    {
        public static string SearchString;
        public static bool ShowMeds;
        public static bool ShowFood;
        public static bool ShowBackpacks;

        /// <summary>
        /// Creates a loot filter based on current Loot Filter settings.
        /// </summary>
        /// <returns>Loot Filter Predicate.</returns>
        public static Predicate<LootItem> Create()
        {
            var search = SearchString?.Trim();
            bool usePrices = string.IsNullOrEmpty(search);
            if (usePrices)
            {
                Predicate<LootItem> p = x => // Default Predicate
                {
                    return (x.IsRegularLoot || x.IsValuableLoot || x.IsImportant || x.IsWishlisted) ||
                                (ShowBackpacks && x.IsBackpack) ||
                                (ShowMeds && x.IsMeds) ||
                                (ShowFood && x.IsFood);
                };
                return item =>
                {
                    if (item is LootAirdrop)
                    {
                        return true;
                    }
                    if (item is LootCorpse)
                    {
                        return true;
                    }
                    if (p(item))
                    {
                        if (item is LootContainer container)
                        {
                            container.SetFilter(p);
                        }
                        return true;
                    }
                    return false;
                };
            }
            else // Loot Search
            {
                using var names = search!.Split(',').Select(a => a.Trim()).ToPooledList();
                Predicate<LootItem> p = x => // Search Predicate
                {
                    return names.Any(a => x.Name.Contains(a, StringComparison.OrdinalIgnoreCase));
                };
                return item =>
                {
                    if (item is LootAirdrop)
                    {
                        return true;
                    }
                    if (item.ContainsSearchPredicate(p))
                    {
                        if (item is LootContainer container)
                        {
                            container.SetFilter(p);
                        }
                        return true;
                    }
                    return false;
                };
            }
        }
    }
}
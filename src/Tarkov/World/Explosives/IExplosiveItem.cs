/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
using LoneEftDmaRadar.Tarkov.Unity;
using LoneEftDmaRadar.UI.Maps;
using VmmSharpEx.Scatter;

namespace LoneEftDmaRadar.Tarkov.World.Explosives
{
    public interface IExplosiveItem : IWorldEntity, IMapEntity
    {
        /// <summary>
        /// Base address of the explosive item.
        /// </summary>
        ulong Addr { get; }
        /// <summary>
        /// Sync the state of the explosive item.
        /// </summary>
        void OnRefresh(VmmScatter scatter);
    }
}


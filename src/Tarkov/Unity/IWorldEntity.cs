/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
namespace LoneEftDmaRadar.Tarkov.Unity
{
    /// <summary>
    /// Defines an Entity that has a 3D World Position.
    /// </summary>
    public interface IWorldEntity
    {
        /// <summary>
        /// Entity's Unity Position in Game World.
        /// </summary>
        ref readonly Vector3 Position { get; }
    }
}


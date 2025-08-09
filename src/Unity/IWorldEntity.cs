using System.Numerics;

namespace eft_dma_radar.Unity
{
    /// <summary>
    /// Defines an Entity that has a 3D GameWorld Position.
    /// </summary>
    public interface IWorldEntity
    {
        /// <summary>
        /// Entity's Unity Position in Local Game World.
        /// </summary>
        ref Vector3 Position { get; }
    }
}

using eft_dma_radar.UI.Skia.Maps;
using eft_dma_radar.Unity;

namespace eft_dma_radar.Tarkov.GameWorld.Explosives
{
    public interface IExplosiveItem : IWorldEntity, IMapEntity
    {
        /// <summary>
        /// Base address of the explosive item.
        /// </summary>
        ulong Addr { get; }
        /// <summary>
        /// True if the explosive is in an active state, otherwise False.
        /// </summary>
        bool IsActive { get; }
        /// <summary>
        /// Refresh the state of the explosive item.
        /// </summary>
        void Refresh();
    }
}

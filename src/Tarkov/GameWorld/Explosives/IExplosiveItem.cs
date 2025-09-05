using EftDmaRadarLite.UI.Skia.Maps;
using EftDmaRadarLite.Unity;

namespace EftDmaRadarLite.Tarkov.GameWorld.Explosives
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

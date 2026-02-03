using eft_dma_radar.UI.Radar;
using eft_dma_radar.UI.Skia.Maps;
using eft_dma_radar.Unity;

namespace eft_dma_radar.Tarkov.GameWorld.Exits
{
    /// <summary>
    /// Defines a contract for a point that can be used to exit the map.
    /// </summary>
    public interface IExitPoint : IWorldEntity, IMapEntity, IMouseoverEntity
    {
    }
}

using EftDmaRadarLite.UI.Radar;
using EftDmaRadarLite.UI.Skia.Maps;
using EftDmaRadarLite.Unity;

namespace EftDmaRadarLite.Tarkov.GameWorld.Exits
{
    /// <summary>
    /// Defines a contract for a point that can be used to exit the map.
    /// </summary>
    public interface IExitPoint : IWorldEntity, IMapEntity, IMouseoverEntity
    {
    }
}

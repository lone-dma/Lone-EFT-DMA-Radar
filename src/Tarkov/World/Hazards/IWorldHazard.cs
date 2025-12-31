using LoneEftDmaRadar.Tarkov.Unity;
using LoneEftDmaRadar.UI.Skia;

namespace LoneEftDmaRadar.Tarkov.World.Hazards
{
    /// <summary>
    /// Defines an interface for in-game world hazards.
    /// </summary>
    public interface IWorldHazard : IWorldEntity, IMouseoverEntity
    {
        /// <summary>
        /// Description of the hazard/type.
        /// </summary>
        string HazardType { get; }
    }
}

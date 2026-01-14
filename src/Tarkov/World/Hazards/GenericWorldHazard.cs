using LoneEftDmaRadar.Maps;
using LoneEftDmaRadar.Misc;
using LoneEftDmaRadar.Tarkov.Unity;
using LoneEftDmaRadar.Tarkov.World.Player;
using LoneEftDmaRadar.UI.Skia;

namespace LoneEftDmaRadar.Tarkov.World.Hazards
{
    public class GenericWorldHazard : IWorldHazard
    {
        [JsonPropertyName("hazardType")]
        public string HazardType { get; set; }

        [JsonPropertyName("position")]
        public Vector3 Position { get; set; }

        [JsonIgnore]
        public Vector2 MouseoverPosition { get; set; }

        [JsonIgnore]
        ref readonly Vector3 IWorldEntity.Position => throw new NotImplementedException();

        public void Draw(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            var hazardZoomedPos = this.Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams);
            MouseoverPosition = hazardZoomedPos.AsVector2();
            hazardZoomedPos.DrawHazardMarker(canvas);
        }

        public void DrawMouseover(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams).DrawMouseoverText(canvas, $"Hazard: {HazardType ?? "Unknown"}");
        }
    }
}

using LoneEftDmaRadar.Misc;
using LoneEftDmaRadar.Tarkov.GameWorld.Player;
using LoneEftDmaRadar.Tarkov.Unity;
using LoneEftDmaRadar.UI.Radar.Maps;
using LoneEftDmaRadar.UI.Skia;

namespace LoneEftDmaRadar.Tarkov.GameWorld.Hazards
{
    public class GenericWorldHazard : IWorldHazard
    {
        [JsonIgnore]
        private Vector3 _position;
        [JsonPropertyName("hazardType")]
        public string HazardType { get; set; }

        [JsonPropertyName("position")]
        public TarkovDataManager.PositionElement PositionJson { get; set; }

        [JsonIgnore]
        public Vector2 MouseoverPosition { get; set; }

        [JsonIgnore]
        ref readonly Vector3 Position
        {
            get
            {
                if (_position == default)
                {
                    _position = PositionJson.AsVector3();
                }
                return ref _position;
            }
        }

        [JsonIgnore]
        ref readonly Vector3 IWorldEntity.Position => ref Position;

        public void Draw(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            var mineZoomedPos = this.Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams);
            MouseoverPosition = mineZoomedPos.AsVector2();
            mineZoomedPos.DrawHazardMarker(canvas);
        }

        public void DrawMouseover(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams).DrawMouseoverText(canvas, $"Hazard: {HazardType ?? "Unknown"}");
        }
    }
}

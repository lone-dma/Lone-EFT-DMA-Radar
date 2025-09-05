using EftDmaRadarLite.Tarkov.Player;
using EftDmaRadarLite.UI.Radar;
using EftDmaRadarLite.Unity;
using EftDmaRadarLite.UI.Skia;
using EftDmaRadarLite.Misc;
using EftDmaRadarLite.UI.Skia.Maps;
using EftDmaRadarLite.Tarkov.Data;

namespace EftDmaRadarLite.Tarkov.GameWorld.Exits
{
    public sealed class TransitPoint : IExitPoint, IWorldEntity, IMapEntity, IMouseoverEntity
    {
        public static implicit operator ulong(TransitPoint x) => x._addr;
        private static readonly uint[] _transformInternalChain =
{
            ObjectClass.MonoBehaviourOffset, MonoBehaviour.GameObjectOffset, GameObject.ComponentsOffset, 0x8
        };

        public TransitPoint(ulong baseAddr)
        {
            _addr = baseAddr;

            var parameters = Memory.ReadPtr(baseAddr + Offsets.TransitPoint.parameters, false);
            var locationPtr = Memory.ReadPtr(parameters + Offsets.TransitParameters.location, false);
            var location = Memory.ReadUnityString(locationPtr, 128, false);
            if (StaticGameData.MapNames.TryGetValue(location, out string destinationMapName))
            {
                Name = $"Transit to {destinationMapName}";
            }
            else
            {
                Name = "Transit";
            }
            var transformInternal = Memory.ReadPtrChain(baseAddr, _transformInternalChain, false);
            try
            {
                _position = new UnityTransform(transformInternal).UpdatePosition();
            }
            catch (ArgumentOutOfRangeException) // Fixes a bug on interchange
            {
                _position = new(0, -100, 0);
            }
        }

        private readonly ulong _addr;
        public string Name { get; }

        #region Interfaces

        private Vector3 _position;
        public ref Vector3 Position => ref _position;
        public Vector2 MouseoverPosition { get; set; }

        public void Draw(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            var heightDiff = Position.Y - localPlayer.Position.Y;
            var paint = GetPaint();
            var point = Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams);
            MouseoverPosition = new Vector2(point.X, point.Y);
            SKPaints.ShapeOutline.StrokeWidth = 2f;
            if (heightDiff > 1.85f) // exfil is above player
            {
                using var path = point.GetUpArrow(6.5f);
                canvas.DrawPath(path, SKPaints.ShapeOutline);
                canvas.DrawPath(path, paint);
            }
            else if (heightDiff < -1.85f) // exfil is below player
            {
                using var path = point.GetDownArrow(6.5f);
                canvas.DrawPath(path, SKPaints.ShapeOutline);
                canvas.DrawPath(path, paint);
            }
            else // exfil is level with player
            {
                float size = 4.75f * App.Config.UI.UIScale;
                canvas.DrawCircle(point, size, SKPaints.ShapeOutline);
                canvas.DrawCircle(point, size, paint);
            }
        }

        private static SKPaint GetPaint()
        {
            var localPlayer = Memory.LocalPlayer;
            if (!(localPlayer?.IsPmc ?? false))
                return SKPaints.PaintExfilInactive;
            return SKPaints.PaintExfilTransit;
        }

        public void DrawMouseover(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            List<string> lines = new(1)
            {
                Name
            };
            Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams).DrawMouseoverText(canvas, lines);
        }

        #endregion

    }
}

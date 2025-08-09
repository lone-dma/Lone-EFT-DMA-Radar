using eft_dma_radar.Misc;
using eft_dma_radar.Tarkov.Data;
using eft_dma_radar.Tarkov.Player;
using eft_dma_radar.UI.Radar;
using eft_dma_radar.UI.Skia;
using eft_dma_radar.UI.Skia.Maps;
using eft_dma_radar.Unity;

namespace eft_dma_radar.Tarkov.Quests
{
    /// <summary>
    /// Wraps a Mouseoverable Quest Location marker onto the Map GUI.
    /// </summary>
    public sealed class QuestLocation : IWorldEntity, IMapEntity, IMouseoverEntity
    {
        /// <summary>
        /// Name of this quest.
        /// </summary>
        public string Name { get; }

        public QuestLocation(string questID, string target, Vector3 position)
        {
            if (EftDataManager.TaskData.TryGetValue(questID, out var q))
                Name = q.Name;
            else
                Name = target;
            Position = position;
        }

        public void Draw(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            var point = Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams);
            MouseoverPosition = new Vector2(point.X, point.Y);
            var heightDiff = Position.Y - localPlayer.Position.Y;
            SKPaints.ShapeOutline.StrokeWidth = 2f;
            if (heightDiff > 1.45) // marker is above player
            {
                using var path = point.GetUpArrow();
                canvas.DrawPath(path, SKPaints.ShapeOutline);
                canvas.DrawPath(path, SKPaints.QuestHelperPaint);
            }
            else if (heightDiff < -1.45) // marker is below player
            {
                using var path = point.GetDownArrow();
                canvas.DrawPath(path, SKPaints.ShapeOutline);
                canvas.DrawPath(path, SKPaints.QuestHelperPaint);
            }
            else // marker is level with player
            {
                var squareSize = 8 * App.Config.UI.UIScale;
                canvas.DrawRect(point.X, point.Y,
                    squareSize, squareSize, SKPaints.ShapeOutline);
                canvas.DrawRect(point.X, point.Y,
                    squareSize, squareSize, SKPaints.QuestHelperPaint);
            }
        }

        public Vector2 MouseoverPosition { get; set; }

        public void DrawMouseover(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            string[] lines = new[] { Name };
            Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams).DrawMouseoverText(canvas, lines);
        }

        private Vector3 _position;
        public ref Vector3 Position => ref _position;
    }
}

using Collections.Pooled;
using LoneEftDmaRadar.Tarkov.GameWorld.Player;
using LoneEftDmaRadar.Tarkov.Unity;
using LoneEftDmaRadar.UI.Radar.Maps;
using LoneEftDmaRadar.UI.Skia;

namespace LoneEftDmaRadar.Tarkov.GameWorld.Quests
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
        public QuestObjectiveType Type { get; }
        public Vector2 MouseoverPosition { get; set; }

        private readonly Vector3 _position;
        public ref readonly Vector3 Position => ref _position;

        public QuestLocation(string questID, string target, Vector3 position)
        {
            QuestObjectiveType foundType = QuestObjectiveType.Unknown;
            // Resolve quest name and objective type (if available).
            if (TarkovDataManager.TaskData.TryGetValue(questID, out var q))
            {
                Name = q.Name ?? target;

                // Attempt to find the objective that corresponds to 'target' and extract its Type.
                try
                {
                    if (q.Objectives is not null)
                    {
                        var obj = q.Objectives.FirstOrDefault(o =>
                            !string.IsNullOrEmpty(o.Id) && string.Equals(o.Id, target, StringComparison.OrdinalIgnoreCase)
                            || (o.MarkerItem?.Id is not null && string.Equals(o.MarkerItem.Id, target, StringComparison.OrdinalIgnoreCase))
                            || (o.Zones?.Any(z => string.Equals(z.Id, target, StringComparison.OrdinalIgnoreCase)) == true)
                        );
                        foundType = obj?.Type ?? QuestObjectiveType.Unknown;
                    }
                }
                catch
                {
                    // Swallow any unexpected structure errors
                }

                Type = foundType;
            }
            else
            {
                // Fallback when TaskData doesn't contain the quest
                Name = target;
                Type = QuestObjectiveType.Unknown;
            }

            _position = position;
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
                canvas.DrawPath(path, SKPaints.PaintQuestZone);
            }
            else if (heightDiff < -1.45) // marker is below player
            {
                using var path = point.GetDownArrow();
                canvas.DrawPath(path, SKPaints.ShapeOutline);
                canvas.DrawPath(path, SKPaints.PaintQuestZone);
            }
            else // marker is level with player
            {
                var squareSize = 8 * App.Config.UI.UIScale;
                canvas.DrawRect(point.X, point.Y,
                    squareSize, squareSize, SKPaints.ShapeOutline);
                canvas.DrawRect(point.X, point.Y,
                    squareSize, squareSize, SKPaints.PaintQuestZone);
            }
        }

        public void DrawMouseover(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            using var lines = new PooledList<string>();
            lines.Add(Name);
            lines.Add($"Type: {Type.ToString()}");
            Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams).DrawMouseoverText(canvas, lines.Span);
        }
    }
}
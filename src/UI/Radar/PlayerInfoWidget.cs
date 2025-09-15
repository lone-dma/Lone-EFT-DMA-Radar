using EftDmaRadarLite.Tarkov.Player;
using EftDmaRadarLite.UI.Skia;
using EftDmaRadarLite.Tarkov.Data.TarkovMarket;
using SkiaSharp.Views.WPF;
using EftDmaRadarLite.Misc;

namespace EftDmaRadarLite.UI.Radar
{
    public sealed class PlayerInfoWidget : SKWidgetControl
    {
        /// <summary>
        /// Constructs a Player Info Overlay.
        /// </summary>
        public PlayerInfoWidget(SKGLElement parent, SKRect location, bool minimized, float scale)
            : base(parent, "Player Info", new SKPoint(location.Left, location.Top),
                new SKSize(location.Width, location.Height), scale, false)
        {
            Minimized = minimized;
            SetScaleFactor(scale);
        }


        public void Draw(SKCanvas canvas, PlayerBase localPlayer, IEnumerable<PlayerBase> players)
        {
            if (Minimized)
            {
                Draw(canvas);
                return;
            }

            var localPlayerPos = localPlayer.Position;
            var hostileCount = players.Count(x => x.IsHostileActive);
            var filteredPlayers = players.Where(x => x.IsHumanHostileActive)
                .OrderBy(x => Vector3.Distance(localPlayerPos, x.Position));
            var sb = new StringBuilder();
            sb.AppendFormat("{0,-21}", "Fac / Lvl / Name")
                .AppendFormat("{0,-5}", "Acct")
                .AppendFormat("{0,-5}", "K/D")
                .AppendFormat("{0,-6}", "Hours")
                .AppendFormat("{0,-6}", "Raids")
                .AppendFormat("{0,-6}", "S/R%")
                .AppendFormat("{0,-4}", "Grp")
                .AppendFormat("{0,-7}", "Value")
                .AppendFormat("{0,-16}", "In Hands")
                .AppendLine();
            foreach (var player in filteredPlayers)
            {
                var name = App.Config.UI.HideNames && player.IsHuman ? "<Hidden>" : player.Name;
                var faction = player.PlayerSide.ToString()[0];
                var hands = player.Hands?.DisplayString;
                var inHands = hands is not null ? hands : "--";
                string edition = "--";
                string level = "0";
                string kd = "--";
                string raidCount = "--";
                string survivePercent = "--";
                string hours = "--";
                if (player is ObservedPlayer observed)
                {
                    edition = observed.Profile?.Acct;
                    if (observed.Profile?.Level is int levelResult)
                        level = levelResult.ToString();
                    if (observed.Profile?.Overall_KD is float kdResult)
                        kd = kdResult.ToString("n1");
                    if (observed.Profile?.RaidCount is int raidCountResult)
                        raidCount = Utilities.FormatNumberKM(raidCountResult);
                    if (observed.Profile?.SurvivedRate is float survivedResult)
                        survivePercent = survivedResult.ToString("n1");
                    if (observed.Profile?.Hours is int hoursResult)
                        hours = Utilities.FormatNumberKM(hoursResult);
                }
                var grp = player.GroupID != -1 ? player.GroupID.ToString() : "--";
                var focused = player.IsFocused ? "*" : null;
                sb.AppendFormat("{0,-21}", $"{focused}{faction}{level}:{name}");
                sb.AppendFormat("{0,-5}", edition)
                    .AppendFormat("{0,-5}", kd)
                    .AppendFormat("{0,-6}", hours)
                    .AppendFormat("{0,-6}", raidCount)
                    .AppendFormat("{0,-6}", survivePercent)
                    .AppendFormat("{0,-4}", grp)
                    .AppendFormat("{0,-7}", $"{Utilities.FormatNumberKM(player.Gear?.Value ?? 0)}")
                    .AppendFormat("{0,-16}", $"{inHands}")
                    .AppendLine();
            }

            var data = sb.ToString().Split(Environment.NewLine);

            var lineSpacing = SKFonts.InfoWidgetFont.Spacing;
            var maxLength = data.Max(x => SKFonts.InfoWidgetFont.MeasureText(x));
            var pad = 2.5f * ScaleFactor;
            Size = new SKSize(maxLength + pad, data.Length * lineSpacing);
            Location = Location; // Bounds check
            Draw(canvas); // Draw backer
            var drawPt = new SKPoint(ClientRectangle.Left + pad, ClientRectangle.Top + lineSpacing / 2 + pad);
            canvas.DrawText(
                $"Hostile Count: {hostileCount}",
                drawPt,
                SKTextAlign.Left,
                SKFonts.InfoWidgetFont,
                SKPaints.TextPlayersOverlay); // draw line text
            drawPt.Y += lineSpacing;
            foreach (var line in data) // Draw tooltip text
            {
                if (string.IsNullOrEmpty(line?.Trim()))
                    continue;
                canvas.DrawText(
                    line,
                    drawPt,
                    SKTextAlign.Left,
                    SKFonts.InfoWidgetFont,
                    SKPaints.TextPlayersOverlay); // draw line text

                drawPt.Y += lineSpacing;
            }
        }

        public override void SetScaleFactor(float newScale)
        {
            base.SetScaleFactor(newScale);
        }
    }
}
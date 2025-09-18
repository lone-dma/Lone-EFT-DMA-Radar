/*
 * EFT DMA Radar Lite
 * Brought to you by Lone (Lone DMA)
 * 
MIT License

Copyright (c) 2025 Lone DMA

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 *
*/

using EftDmaRadarLite.Tarkov.Player;
using EftDmaRadarLite.UI.Skia;
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

            const int W_FAC_LVL_NAME = -21;
            const int W_ACCT = -5;
            const int W_KD = -6;
            const int W_HOURS = -6;
            const int W_RAIDS = -6;
            const int W_SURVIVE = -7;
            const int W_GROUP = -4;
            const int W_VALUE = -7;
            const int W_HANDS = -16;

            static void AppendRow(StringBuilder sb,
                (int w, string v) c1,
                (int w, string v) c2,
                (int w, string v) c3,
                (int w, string v) c4,
                (int w, string v) c5,
                (int w, string v) c6,
                (int w, string v) c7,
                (int w, string v) c8,
                (int w, string v) c9)
            {
                sb.AppendFormat($"{{0,{c1.w}}}", c1.v)
                  .AppendFormat($"{{0,{c2.w}}}", c2.v)
                  .AppendFormat($"{{0,{c3.w}}}", c3.v)
                  .AppendFormat($"{{0,{c4.w}}}", c4.v)
                  .AppendFormat($"{{0,{c5.w}}}", c5.v)
                  .AppendFormat($"{{0,{c6.w}}}", c6.v)
                  .AppendFormat($"{{0,{c7.w}}}", c7.v)
                  .AppendFormat($"{{0,{c8.w}}}", c8.v)
                  .AppendFormat($"{{0,{c9.w}}}", c9.v)
                  .AppendLine();
            }

            var sb = new StringBuilder(2048);

            // Header
            AppendRow(sb,
                (W_FAC_LVL_NAME, "Fac / Lvl / Name"),
                (W_ACCT, "Acct"),
                (W_KD, "K/D"),
                (W_HOURS, "Hours"),
                (W_RAIDS, "Raids"),
                (W_SURVIVE, "S/R%"),
                (W_GROUP, "Grp"),
                (W_VALUE, "Value"),
                (W_HANDS, "In Hands"));

            // Sort & filter
            var localPos = localPlayer.Position;
            var filteredPlayers = players
                .Where(p => p.IsHumanHostileActive)
                .OrderBy(p => Vector3.Distance(localPos, p.Position));

            foreach (var player in filteredPlayers)
            {
                string name = (App.Config.UI.HideNames && player.IsHuman) ? "<Hidden>" : player.Name;
                char faction = player.PlayerSide.ToString()[0];
                string inHands = player.Hands?.DisplayString ?? "--";

                // Defaults
                string edition = "--";
                string level = "0";
                string kd = "--";
                string raidCount = "--";
                string survivePercent = "--";
                string hours = "--";

                if (player is ObservedPlayer { Profile: { } profile })
                {
                    if (!string.IsNullOrEmpty(profile.Acct))
                        edition = profile.Acct;

                    if (profile.Level is int lvl)
                        level = lvl.ToString();

                    if (profile.Overall_KD is float kdVal)
                        kd = kdVal.ToString("n1");

                    if (profile.RaidCount is int rc)
                        raidCount = Utilities.FormatNumberKM(rc);

                    if (profile.SurvivedRate is float sr)
                        survivePercent = sr.ToString("n1");

                    if (profile.Hours is int hrs)
                        hours = Utilities.FormatNumberKM(hrs);
                }

                string grp = player.GroupID != -1 ? player.GroupID.ToString() : "--";
                string focused = player.IsFocused ? "*" : string.Empty;
                string facLvlName = $"{focused}{faction}{level}:{name}";
                string value = Utilities.FormatNumberKM(player.Gear?.Value ?? 0);

                AppendRow(sb,
                    (W_FAC_LVL_NAME, facLvlName),
                    (W_ACCT, edition),
                    (W_KD, kd),
                    (W_HOURS, hours),
                    (W_RAIDS, raidCount),
                    (W_SURVIVE, survivePercent),
                    (W_GROUP, grp),
                    (W_VALUE, value),
                    (W_HANDS, inHands));
            }

            // Split lines (trim empty last line if any)
            var lines = sb.ToString()
                          .Split(Environment.NewLine, StringSplitOptions.None)
                          .Where(l => !string.IsNullOrWhiteSpace(l))
                          .ToArray();

            var font = SKFonts.InfoWidgetFont;
            float lineSpacing = font.Spacing;
            float pad = 2.5f * ScaleFactor;
            float maxLength = 0f;

            foreach (var line in lines)
            {
                var len = font.MeasureText(line);
                if (len > maxLength) maxLength = len;
            }

            Size = new SKSize(maxLength + pad, lines.Length * lineSpacing);
            Draw(canvas); // Background/fram

            var drawPt = new SKPoint(
                ClientRectangle.Left + pad,
                ClientRectangle.Top + lineSpacing / 2 + pad);

            foreach (var line in lines)
            {
                canvas.DrawText(line,
                                drawPt,
                                SKTextAlign.Left,
                                font,
                                SKPaints.TextPlayersOverlay);
                drawPt.Y += lineSpacing;
            }
        }

        public override void SetScaleFactor(float newScale)
        {
            base.SetScaleFactor(newScale);
        }
    }
}
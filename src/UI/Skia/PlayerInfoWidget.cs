/*
 * Lone EFT DMA Radar
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

using Collections.Pooled;
using LoneEftDmaRadar.Misc;
using LoneEftDmaRadar.Tarkov.GameWorld.Player;
using LoneEftDmaRadar.Tarkov.GameWorld.Player.Helpers;
using SkiaSharp.Views.WPF;

namespace LoneEftDmaRadar.UI.Skia
{
    public sealed class PlayerInfoWidget : AbstractSKWidget
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


        public void Draw(SKCanvas canvas, AbstractPlayer localPlayer, IEnumerable<AbstractPlayer> players)
        {
            if (Minimized)
            {
                Draw(canvas);
                return;
            }

            static string MakeRow(string c1, string c2, string c3, string c4,
                                  string c5, string c6, string c7, string c8, string c9)
            {
                // known widths
                const int W1 = 21, W2 = 5, W3 = 6, W4 = 6, W5 = 6,
                          W6 = 6, W7 = 4, W8 = 7, W9 = 16;

                const int len = W1 + W2 + W3 + W4 + W5 + W6 + W7 + W8 + W9;

                return string.Create(len, (c1, c2, c3, c4, c5, c6, c7, c8, c9), static (span, cols) =>
                {
                    int pos = 0;
                    WriteAligned(span, ref pos, cols.c1, W1);
                    WriteAligned(span, ref pos, cols.c2, W2);
                    WriteAligned(span, ref pos, cols.c3, W3);
                    WriteAligned(span, ref pos, cols.c4, W4);
                    WriteAligned(span, ref pos, cols.c5, W5);
                    WriteAligned(span, ref pos, cols.c6, W6);
                    WriteAligned(span, ref pos, cols.c7, W7);
                    WriteAligned(span, ref pos, cols.c8, W8);
                    WriteAligned(span, ref pos, cols.c9, W9);
                });
            }

            static void WriteAligned(Span<char> span, ref int pos, string value, int width)
            {
                int padding = width - value.Length;
                if (padding < 0) padding = 0;

                // write the value left-aligned
                value.AsSpan(0, Math.Min(value.Length, width))
                     .CopyTo(span.Slice(pos));

                // pad the rest with spaces
                span.Slice(pos + value.Length, padding).Fill(' ');

                pos += width;
            }

            // Sort & filter
            var localPos = localPlayer.Position;
            using var filteredPlayers = players
                .Where(p => p.IsHumanHostileActive)
                .OrderBy(p => Vector3.Distance(localPos, p.Position))
                .ToPooledList();

            // Setup Frame and Draw Header
            var font = SKFonts.InfoWidgetFont;
            float pad = 2.5f * ScaleFactor;
            float maxLength = 0f;
            var drawPt = new SKPoint(
                ClientRectangle.Left + pad,
                ClientRectangle.Top + font.Spacing / 2 + pad);

            string header = MakeRow(
                "Fac / Lvl / Name", // c1
                "Acct",             // c2
                "K/D",              // c3
                "Hours",            // c4
                "Raids",            // c5
                "S/R%",             // c6
                "Grp",              // c7
                "Value",            // c8
                "In Hands");       // c9

            var len = font.MeasureText(header);
            if (len > maxLength) maxLength = len;

            Size = new SKSize(maxLength + pad, filteredPlayers.Count * font.Spacing);
            Draw(canvas); // Background/frame

            canvas.DrawText(header,
                drawPt,
                SKTextAlign.Left,
                font,
                SKPaints.TextPlayersOverlay);
            drawPt.Offset(0, font.Spacing);

            foreach (var player in filteredPlayers)
            {
                string name = (App.Config.UI.HideNames && player.IsHuman) ? "<Hidden>" : player.Name;
                char faction = player.PlayerSide.ToString()[0];
                string inHands = player.Hands?.DisplayString ?? "--";

                // Defaults
                string edition = null;
                string level = null;
                string kd = null;
                string raidCount = null;
                string survivePercent = null;
                string hours = null;

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
                string facLvlName = $"{faction}{level ?? "0"}:{name}";
                string value = Utilities.FormatNumberKM(player.Gear?.Value ?? 0);

                string line = MakeRow(
                    facLvlName,
                    edition ?? "--",
                    kd ?? "--",
                    hours ?? "--",
                    raidCount ?? "--",
                    survivePercent ?? "--",
                    grp,
                    value,
                    inHands);

                canvas.DrawText(line,
                    drawPt,
                    SKTextAlign.Left,
                    font,
                    GetTextPaint(player));
                drawPt.Offset(0, font.Spacing);
            }
        }

        private static SKPaint GetTextPaint(AbstractPlayer player)
        {
            if (player.IsFocused)
                return SKPaints.TextPlayersOverlayFocused;
            switch (player.Type)
            {
                case PlayerType.PMC:
                    return SKPaints.TextPlayersOverlayPMC;
                case PlayerType.PScav:
                    return SKPaints.TextPlayersOverlayPScav;
                case PlayerType.Streamer:
                    return SKPaints.TextPlayersOverlayStreamer;
                case PlayerType.SpecialPlayer:
                    return SKPaints.TextPlayersOverlaySpecial;
                default:
                    return SKPaints.TextPlayersOverlay;
            }
        }


        public override void SetScaleFactor(float newScale)
        {
            base.SetScaleFactor(newScale);
        }
    }
}
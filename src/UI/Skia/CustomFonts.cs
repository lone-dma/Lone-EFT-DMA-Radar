/*
 * Lone EFT DMA Radar - Copyright (c) 2026 Lone DMA
 * Licensed under GNU AGPLv3. See https://www.gnu.org/licenses/agpl-3.0.html
 */
using LoneEftDmaRadar.Misc;

namespace LoneEftDmaRadar.UI.Skia
{
    internal static class CustomFonts
    {
        /// <summary>
        /// Default font with Chinese support (falls back to Neo Sans Std Regular if no Chinese font found)
        /// </summary>
        public static SKTypeface NeoSansStdRegular { get; }

        static CustomFonts()
        {
            try
            {
                // Try to load Chinese font from system first
                string[] chineseFontPaths = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "msyh.ttc"),    // 微软雅黑
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "msyhbd.ttc"), // 微软雅黑粗体
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "simhei.ttf"), // 黑体
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "simsun.ttc"), // 宋体
                };

                SKTypeface chineseTypeface = null;
                foreach (var fontPath in chineseFontPaths)
                {
                    if (File.Exists(fontPath))
                    {
                        chineseTypeface = SKTypeface.FromFile(fontPath);
                        if (chineseTypeface is not null)
                            break;
                    }
                }

                if (chineseTypeface is not null)
                {
                    NeoSansStdRegular = chineseTypeface;
                }
                else
                {
                    // Fall back to embedded font (English only)
                    byte[] neoSansStdRegular;
                    using (var stream = Utilities.OpenResource("LoneEftDmaRadar.Resources.NeoSansStdRegular.otf"))
                    {
                        neoSansStdRegular = new byte[stream!.Length];
                        stream.ReadExactly(neoSansStdRegular);
                    }
                    NeoSansStdRegular = SKTypeface.FromStream(new MemoryStream(neoSansStdRegular, false));
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("ERROR Loading Custom Fonts!", ex);
            }
        }
    }
}


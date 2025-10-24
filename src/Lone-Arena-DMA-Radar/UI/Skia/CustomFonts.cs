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

namespace LoneArenaDmaRadar.UI.Skia
{
    internal static class CustomFonts
    {
        /// <summary>
        /// Neo Sans Std Regular
        /// </summary>
        public static SKTypeface NeoSansStdRegular { get; }

        static CustomFonts()
        {
            try
            {
                byte[] neoSansStdRegular;
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("LoneArenaDmaRadar.NeoSansStdRegular.otf"))
                {
                    neoSansStdRegular = new byte[stream!.Length];
                    stream.ReadExactly(neoSansStdRegular);
                }
                NeoSansStdRegular = SKTypeface.FromStream(new MemoryStream(neoSansStdRegular, false));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("ERROR Loading Custom Fonts!", ex);
            }
        }
    }
}

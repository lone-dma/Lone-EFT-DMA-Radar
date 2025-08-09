namespace eft_dma_radar.UI.Skia
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
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("eft_dma_radar.NeoSansStdRegular.otf"))
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

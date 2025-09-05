namespace EftDmaRadarLite.UI.Skia
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
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("EftDmaRadarLite.NeoSansStdRegular.otf"))
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

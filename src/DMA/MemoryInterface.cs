global using static EftDmaRadarLite.DMA.MemoryInterface;
using VmmSharpEx.Scatter;

namespace EftDmaRadarLite.DMA
{
    internal static class MemoryInterface
    {
        /// <summary>
        /// Singleton Instance for use in this assembly.
        /// </summary>
        public static MemDMA Memory { get; private set; }

        /// <summary>
        /// Initialize the Memory Interface.
        /// </summary>
        public static void ModuleInit()
        {
            ScatterReadMap.MaxReadSize = (int)MemDMA.MAX_READ_SIZE;
            Memory = new MemDMA();
            Debug.WriteLine("DMA Initialized!");
        }
    }
}

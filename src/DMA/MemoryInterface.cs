global using static eft_dma_radar.DMA.MemoryInterface;
using VmmSharpEx.Scatter;

namespace eft_dma_radar.DMA
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

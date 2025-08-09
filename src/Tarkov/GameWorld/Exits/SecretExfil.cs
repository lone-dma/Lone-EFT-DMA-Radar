using eft_dma_radar.UI.Skia;

namespace eft_dma_radar.Tarkov.GameWorld.Exits
{
    public sealed class SecretExfil : Exfil
    {
        public override EStatus Status => EStatus.Pending;
        public SecretExfil(ulong baseAddr) : base(baseAddr, false)
        {
        }

        public override void Update(Enums.EExfiltrationStatus status) { } // No need to update status for SecretExfil, always pending

        public override SKPaint GetPaint()
        {
            return SKPaints.PaintExfilPending;
        }
    }
}

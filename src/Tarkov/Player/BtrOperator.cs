using VmmSharpEx.Scatter;

namespace EftDmaRadarLite.Tarkov.Player
{
    /// <summary>
    /// BTR Bot Operator.
    /// </summary>
    public sealed class BtrOperator : ObservedPlayer
    {
        private readonly ulong _btrView;
        private Vector3 _position;

        public override ref Vector3 Position
        {
            get => ref _position;
        }
        public override string Name
        {
            get => "BTR";
            set { }
        }
        public BtrOperator(ulong btrView, ulong playerBase) : base(playerBase)
        {
            _btrView = btrView;
            Type = PlayerType.AIRaider;
        }

        /// <summary>
        /// Set the position of the BTR.
        /// Give this function it's own unique Index.
        /// </summary>
        /// <param name="index">Scatter read index to read off of.</param>
        public override void OnRealtimeLoop(ScatterReadIndex index, bool espRunning)
        {
            index.AddValueEntry<Vector3>(0, _btrView + Offsets.BTRView._targetPosition);
            index.Completed += (sender, x1) =>
            {
                if (x1.TryGetValue<Vector3>(0, out var position))
                    _position = position;
            };
        }
    }
}

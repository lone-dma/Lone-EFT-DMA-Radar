namespace EftDmaRadarLite.Unity
{
    public static class UnityTransformExtensions
    {
        private static readonly Vector3 _left = new Vector3(-1, 0, 0);
        private static readonly Vector3 _right = new(1, 0, 0);
        private static readonly Vector3 _up = new(0, 1, 0);
        private static readonly Vector3 _down = new(0, -1, 0);
        private static readonly Vector3 _forward = new(0, 0, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Left(this Quaternion q) =>
            q.Multiply(_left);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Right(this Quaternion q) =>
            q.Multiply(_right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Up(this Quaternion q) =>
            q.Multiply(_up);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Down(this Quaternion q) =>
            q.Multiply(_down);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Forward(this Quaternion q) =>
            q.Multiply(_forward);

        /// <summary>
        /// Convert Local Direction to World Direction.
        /// </summary>
        /// <param name="localDirection">Local Direction.</param>
        /// <returns>World Direction.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 TransformDirection(this Quaternion q, Vector3 localDirection)
        {
            return q.Multiply(localDirection);
        }

        /// <summary>
        /// Convert World Direction to Local Direction.
        /// </summary>
        /// <param name="worldDirection">World Direction.</param>
        /// <returns>Local Direction.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 InverseTransformDirection(this Quaternion q, Vector3 worldDirection)
        {
            return Quaternion.Conjugate(q).Multiply(worldDirection);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Multiply(this Quaternion q, Vector3 vector)
        {
            var m = Matrix4x4.CreateFromQuaternion(q);
            return Vector3.Transform(vector, m);
        }
    }
}

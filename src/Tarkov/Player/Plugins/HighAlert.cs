namespace eft_dma_radar.Tarkov.Player.Plugins
{
    internal static class HighAlert
    {
        /// <summary>
        /// True if <paramref name="source"/> is facing <paramref name="target"/>.
        /// </summary>
        public static bool IsFacingTarget(this PlayerBase source, PlayerBase target, float? maxDist = null)
        {
            Vector3 delta = target.Position - source.Position;

            if (maxDist is float m)
            {
                float maxDistSq = m * m;
                float distSq = Vector3.Dot(delta, delta);
                if (distSq > maxDistSq) return false;
            }

            float distance = delta.Length();
            if (distance <= 1e-6f)
                return true;

            Vector3 fwd = RotationToDirection(source.Rotation);

            float cosAngle = Vector3.Dot(fwd, delta) / distance;

            const float A = 31.3573f;
            const float B = 3.51726f;
            const float C = 0.626957f;
            const float D = 15.6948f;

            float x = MathF.Abs(C - D * distance);
            float angleDeg = A - B * MathF.Log(MathF.Max(x, 1e-6f));
            if (angleDeg < 1f) angleDeg = 1f;
            if (angleDeg > 179f) angleDeg = 179f;

            float cosThreshold = MathF.Cos(angleDeg * (MathF.PI / 180f));
            return cosAngle >= cosThreshold;
        }

        public static Vector3 RotationToDirection(Vector2 rotation)
        {
            float yaw = rotation.X * (MathF.PI / 180f);
            float pitch = rotation.Y * (MathF.PI / 180f);

            float cp = MathF.Cos(pitch);
            float sp = MathF.Sin(pitch);
            float sy = MathF.Sin(yaw);
            float cy = MathF.Cos(yaw);

            var dir = new Vector3(
                cp * sy,
               -sp,
                cp * cy
            );

            float lenSq = Vector3.Dot(dir, dir);
            if (lenSq > 0f && MathF.Abs(lenSq - 1f) > 1e-4f)
            {
                float invLen = 1f / MathF.Sqrt(lenSq);
                dir *= invLen;
            }
            return dir;
        }
    }
}

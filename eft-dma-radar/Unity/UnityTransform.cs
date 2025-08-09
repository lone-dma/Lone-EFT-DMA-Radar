using eft_dma_radar.Misc;
using eft_dma_radar.Misc.Pools;
using SkiaSharp;

namespace eft_dma_radar.Unity
{
    public sealed class UnityTransform
    {
        private const int MAX_ITERATIONS = 4000;
        private readonly bool _useCache;
        private readonly ReadOnlyMemory<int> _indices;

        private Vector3 _position;
        /// <summary>
        /// Unity World Position for this Transform.
        /// </summary>
        public ref Vector3 Position => ref _position;

        public UnityTransform(ulong transformInternal, bool useCache = false)
        {
            /// Constructor
            TransformInternal = transformInternal;
            _useCache = useCache;

            var ta = Memory.ReadValue<TransformAccess>(transformInternal + UnityOffsets.TransformInternal.TransformAccess, useCache);
            Index = ta.Index;
            HierarchyAddr = ta.Hierarchy;
            var transformHierarchy = Memory.ReadValue<TransformHierarchy>(HierarchyAddr, useCache);
            IndicesAddr = transformHierarchy.Indices;
            VerticesAddr = transformHierarchy.Vertices;
            /// Populate Indices once for the Life of the Transform.
            _indices = ReadIndices();
        }
        
        private ReadOnlySpan<int> Indices
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _indices.Span;
        }
        public ulong TransformInternal { get; }
        private ulong HierarchyAddr { get; }
        private ulong IndicesAddr { get; }
        public ulong VerticesAddr { get; }
        public int Index { get; }

        #region Transform Methods

        /// <summary>
        /// Update Transform's World Position.
        /// </summary>
        /// <returns>Ref to World Position</returns>
        public ref Vector3 UpdatePosition(SharedArray<TrsX> vertices = null)
        {
            SharedArray<TrsX> standaloneVerticesLease = null;
            try
            {
                vertices ??= standaloneVerticesLease = ReadVertices();

                var worldPos = vertices[Index].t;
                int index = Indices[Index];
                int iterations = 0;
                while (index >= 0)
                {
                    ArgumentOutOfRangeException.ThrowIfGreaterThan(iterations++, MAX_ITERATIONS, nameof(iterations));
                    var parent = vertices[index];

                    worldPos = parent.q.Multiply(worldPos);
                    worldPos *= parent.s;
                    worldPos += parent.t;

                    index = Indices[index];
                }

                worldPos.ThrowIfAbnormal();
                _position = worldPos;
                return ref _position;
            }
            finally
            {
                standaloneVerticesLease?.Return();
            }
        }

        /// <summary>
        /// Get Transform's World Rotation.
        /// </summary>
        /// <returns>World Rotation</returns>
        public Quaternion GetRotation(SharedArray<TrsX> vertices = null)
        {
            SharedArray<TrsX> standaloneVertices = null;
            try
            {
                vertices ??= standaloneVertices = ReadVertices();

                var worldRot = vertices[Index].q;
                int index = Indices[Index];
                int iterations = 0;
                while (index >= 0)
                {
                    ArgumentOutOfRangeException.ThrowIfGreaterThan(iterations++, MAX_ITERATIONS, nameof(iterations));
                    var parent = vertices[index];

                    worldRot = parent.q * worldRot;

                    index = Indices[index];
                }

                worldRot.ThrowIfAbnormal();
                return worldRot;
            }
            finally
            {
                standaloneVertices?.Return();
            }
        }

        /// <summary>
        /// Get Transform's Root World Position.
        /// </summary>
        /// <returns>Root World Position</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 GetRootPosition()
        {
            Vector3 rootPos = Memory.ReadValue<TrsX>(HierarchyAddr + TransformHierarchy.RootPositionOffset, _useCache).t;
            rootPos.ThrowIfAbnormal();
            return rootPos;
        }

        /// <summary>
        /// Get Transform's Local Position.
        /// </summary>
        /// <returns>Local Position</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 GetLocalPosition()
        {
            return Memory.ReadValue<TrsX>(VerticesAddr + (uint)Index * (uint)SizeChecker<TrsX>.Size, _useCache).t;
        }

        /// <summary>
        /// Get Transform's Local Scale.
        /// </summary>
        /// <returns>Local Scale</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 GetLocalScale()
        {
            return Memory.ReadValue<TrsX>(VerticesAddr + (uint)Index * (uint)SizeChecker<TrsX>.Size, _useCache).s;
        }
        /// <summary>
        /// Get Transform's Local Rotation.
        /// </summary>
        /// <returns>Local Rotation</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Quaternion GetLocalRotation()
        {
            return Memory.ReadValue<TrsX>(VerticesAddr + (uint)Index * (uint)SizeChecker<TrsX>.Size, _useCache).q;
        }


        /// <summary>
        /// Convert from Local Point to World Point.
        /// </summary>
        /// <param name="localPoint">Local Point</param>
        /// <returns>World Point.</returns>
        public Vector3 TransformPoint(Vector3 localPoint, SharedArray<TrsX> vertices = null)
        {
            SharedArray<TrsX> standaloneVertices = null;
            try
            {
                vertices ??= standaloneVertices = ReadVertices();

                var worldPos = localPoint;
                int index = Index;
                int iterations = 0;
                while (index >= 0)
                {
                    ArgumentOutOfRangeException.ThrowIfGreaterThan(iterations++, MAX_ITERATIONS, nameof(iterations));
                    var parent = vertices[index];

                    worldPos *= parent.s;
                    worldPos = parent.q.Multiply(worldPos);
                    worldPos += parent.t;

                    index = Indices[index];
                }

                worldPos.ThrowIfAbnormal();
                return worldPos;
            }
            finally
            {
                standaloneVertices?.Return();
            }
        }

        /// <summary>
        /// Convert from World Point to Local Point.
        /// </summary>
        /// <param name="worldPoint">World Point</param>
        /// <returns>Local Point</returns>
        public Vector3 InverseTransformPoint(Vector3 worldPoint, SharedArray<TrsX> vertices = null)
        {
            SharedArray<TrsX> standaloneVertices = null;
            try
            {
                vertices ??= standaloneVertices = ReadVertices();

                var worldPos = vertices[Index].t;
                var worldRot = vertices[Index].q;

                Vector3 localScale = vertices[Index].s;

                int index = Indices[Index];
                int iterations = 0;
                while (index >= 0)
                {
                    ArgumentOutOfRangeException.ThrowIfGreaterThan(iterations++, MAX_ITERATIONS, nameof(iterations));
                    var parent = vertices[index];

                    worldPos = parent.q.Multiply(worldPos);
                    worldPos *= parent.s;
                    worldPos += parent.t;

                    worldRot = parent.q * worldRot;

                    index = Indices[index];
                }

                var local = Quaternion.Conjugate(worldRot).Multiply(worldPoint - worldPos);
                return local / localScale;
            }
            finally
            {
                standaloneVertices?.Return();
            }
        }
        #endregion

        #region Structures
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private readonly ref struct TransformAccess
        {
            public readonly ulong Hierarchy;
            public readonly int Index;
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        public readonly ref struct TransformHierarchy
        {
            [FieldOffset(0x18)]
            public readonly ulong Vertices;
            [FieldOffset(0x20)]
            public readonly ulong Indices;

            public const uint RootPositionOffset = 0x90;
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 48)]
        public readonly struct TrsX
        {
            [FieldOffset(0x0)]
            public readonly Vector3 t;
            // pad 0x4
            [FieldOffset(0x10)]
            public readonly Quaternion q;
            [FieldOffset(0x20)]
            public readonly Vector3 s;
            // pad 0x4
        }
        #endregion

        #region ReadMem
        /// <summary>
        /// Read Indices for this Transform.
        /// NOTE: Indices does not need to be updated for the life of the transform.
        /// </summary>
        private int[] ReadIndices()
        {
            var indices = new int[Index + 1];
            Memory.ReadBuffer(IndicesAddr, indices.AsSpan(), _useCache);
            return indices;
        }

        /// <summary>
        /// Read Updated Vertices for this Transform.
        /// </summary>
        public SharedArray<TrsX> ReadVertices()
        {
            var vertices = SharedArray<TrsX>.Get(Index + 1);
            try
            {
                Memory.ReadBuffer(VerticesAddr, vertices.Span, _useCache);
            }
            catch
            {
                vertices.Return();
                throw;
            }
            return vertices;
        }
        #endregion

    }
}
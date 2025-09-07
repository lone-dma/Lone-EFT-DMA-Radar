using EftDmaRadarLite.Tarkov.GameWorld;
using EftDmaRadarLite.Unity;

namespace EftDmaRadarLite.Tarkov.Player
{
    /// <summary>
    /// Contains abstractions for drawing Player Skeletons.
    /// </summary>
    public sealed class Skeleton
    {
        private const int JOINTS_COUNT = 26;
        private static readonly SKPoint[] _espWidgetBuffer = new SKPoint[JOINTS_COUNT];
        /// <summary>
        /// All Skeleton Bones.
        /// </summary>
        public static ReadOnlyMemory<Bones> AllSkeletonBones { get; } = Enum.GetValues<SkeletonBones>().Cast<Bones>().ToArray();

        private readonly Dictionary<Bones, UnityTransform> _bones;
        private readonly PlayerBase _player;

        /// <summary>
        /// Skeleton Root Transform.
        /// </summary>
        public UnityTransform Root { get; private set; }

        /// <summary>
        /// All Transforms for this Skeleton (including Root).
        /// </summary>
        public IReadOnlyDictionary<Bones, UnityTransform> Bones => _bones;

        public Skeleton(PlayerBase player, Action<Bones, Span<uint>> getTransformChainFunc)
        {
            _player = player;
            Span<uint> tiOffsets = stackalloc uint[PlayerBase.TransformInternalChainCount];
            getTransformChainFunc(Unity.Bones.HumanBase, tiOffsets);
            var tiRoot = Memory.ReadPtrChain(player.Base, true, tiOffsets);
            Root = new UnityTransform(tiRoot);
            _ = Root.UpdatePosition();
            var bones = new Dictionary<Bones, UnityTransform>(AllSkeletonBones.Length + 1)
            {
                [EftDmaRadarLite.Unity.Bones.HumanBase] = Root
            };
            foreach (var bone in AllSkeletonBones.Span)
            {
                getTransformChainFunc(bone, tiOffsets);
                var tiBone = Memory.ReadPtrChain(player.Base, true, tiOffsets);
                bones[bone] = new UnityTransform(tiBone);
            }
            _bones = bones;
        }

        /// <summary>
        /// Reset the Transform for this player.
        /// </summary>
        /// <param name="bone"></param>
        public void ResetTransform(Bones bone)
        {
            Debug.WriteLine($"Attempting to get new {bone} Transform for Player '{_player.Name}'...");
            var transform = new UnityTransform(_bones[bone].TransformInternal);
            _bones[bone] = transform;
            if (bone is EftDmaRadarLite.Unity.Bones.HumanBase)
                Root = transform;
            Debug.WriteLine($"[OK] New {bone} Transform for Player '{_player.Name}'");
        }

        /// <summary>
        /// Updates the static ESP Widget Buffer with the current Skeleton Bone Screen Coordinates.<br />
        /// See <see cref="Skeleton._espWidgetBuffer"/><br />
        /// NOT THREAD SAFE!
        /// </summary>
        /// <param name="scaleX">X Scale Factor.</param>
        /// <param name="scaleY">Y Scale Factor.</param>
        /// <returns>True if successful, otherwise False.</returns>
        public bool UpdateESPWidgetBuffer(float scaleX, float scaleY, out SKPoint[] buffer)
        {
            buffer = default;
            if (!CameraManager.WorldToScreen(ref _bones[Unity.Bones.HumanSpine2].Position, out var midTorsoScreen, true, true))
                return false;
            if (!CameraManager.WorldToScreen(ref _bones[Unity.Bones.HumanHead].Position, out var headScreen))
                return false;
            if (!CameraManager.WorldToScreen(ref _bones[Unity.Bones.HumanNeck].Position, out var neckScreen))
                return false;
            if (!CameraManager.WorldToScreen(ref _bones[Unity.Bones.HumanLCollarbone].Position, out var leftCollarScreen))
                return false;
            if (!CameraManager.WorldToScreen(ref _bones[Unity.Bones.HumanRCollarbone].Position, out var rightCollarScreen))
                return false;
            if (!CameraManager.WorldToScreen(ref _bones[Unity.Bones.HumanLPalm].Position, out var leftHandScreen))
                return false;
            if (!CameraManager.WorldToScreen(ref _bones[Unity.Bones.HumanRPalm].Position, out var rightHandScreen))
                return false;
            if (!CameraManager.WorldToScreen(ref _bones[Unity.Bones.HumanSpine3].Position, out var upperTorsoScreen))
                return false;
            if (!CameraManager.WorldToScreen(ref _bones[Unity.Bones.HumanSpine1].Position, out var lowerTorsoScreen))
                return false;
            if (!CameraManager.WorldToScreen(ref _bones[Unity.Bones.HumanPelvis].Position, out var pelvisScreen))
                return false;
            if (!CameraManager.WorldToScreen(ref _bones[Unity.Bones.HumanLFoot].Position, out var leftFootScreen))
                return false;
            if (!CameraManager.WorldToScreen(ref _bones[Unity.Bones.HumanRFoot].Position, out var rightFootScreen))
                return false;
            if (!CameraManager.WorldToScreen(ref _bones[Unity.Bones.HumanLThigh2].Position, out var leftKneeScreen))
                return false;
            if (!CameraManager.WorldToScreen(ref _bones[Unity.Bones.HumanRThigh2].Position, out var rightKneeScreen))
                return false;
            if (!CameraManager.WorldToScreen(ref _bones[Unity.Bones.HumanLForearm2].Position, out var leftElbowScreen))
                return false;
            if (!CameraManager.WorldToScreen(ref _bones[Unity.Bones.HumanRForearm2].Position, out var rightElbowScreen))
                return false;
            int index = 0;
            var center = CameraManager.ViewportCenter;
            // Head to left foot
            ScaleAimviewPoint(headScreen, ref _espWidgetBuffer[index++]);
            ScaleAimviewPoint(neckScreen, ref _espWidgetBuffer[index++]);
            ScaleAimviewPoint(neckScreen, ref _espWidgetBuffer[index++]);
            ScaleAimviewPoint(upperTorsoScreen, ref _espWidgetBuffer[index++]);
            ScaleAimviewPoint(upperTorsoScreen, ref _espWidgetBuffer[index++]);
            ScaleAimviewPoint(midTorsoScreen, ref _espWidgetBuffer[index++]);
            ScaleAimviewPoint(midTorsoScreen, ref _espWidgetBuffer[index++]);
            ScaleAimviewPoint(lowerTorsoScreen, ref _espWidgetBuffer[index++]);
            ScaleAimviewPoint(lowerTorsoScreen, ref _espWidgetBuffer[index++]);
            ScaleAimviewPoint(pelvisScreen, ref _espWidgetBuffer[index++]);
            ScaleAimviewPoint(pelvisScreen, ref _espWidgetBuffer[index++]);
            ScaleAimviewPoint(leftKneeScreen, ref _espWidgetBuffer[index++]);
            ScaleAimviewPoint(leftKneeScreen, ref _espWidgetBuffer[index++]);
            ScaleAimviewPoint(leftFootScreen, ref _espWidgetBuffer[index++]);
            // Pelvis to right foot
            ScaleAimviewPoint(pelvisScreen, ref _espWidgetBuffer[index++]);
            ScaleAimviewPoint(rightKneeScreen, ref _espWidgetBuffer[index++]);
            ScaleAimviewPoint(rightKneeScreen, ref _espWidgetBuffer[index++]);
            ScaleAimviewPoint(rightFootScreen, ref _espWidgetBuffer[index++]);
            // Left collar to left hand
            ScaleAimviewPoint(leftCollarScreen, ref _espWidgetBuffer[index++]);
            ScaleAimviewPoint(leftElbowScreen, ref _espWidgetBuffer[index++]);
            ScaleAimviewPoint(leftElbowScreen, ref _espWidgetBuffer[index++]);
            ScaleAimviewPoint(leftHandScreen, ref _espWidgetBuffer[index++]);
            // Right collar to right hand
            ScaleAimviewPoint(rightCollarScreen, ref _espWidgetBuffer[index++]);
            ScaleAimviewPoint(rightElbowScreen, ref _espWidgetBuffer[index++]);
            ScaleAimviewPoint(rightElbowScreen, ref _espWidgetBuffer[index++]);
            ScaleAimviewPoint(rightHandScreen, ref _espWidgetBuffer[index++]);
            buffer = _espWidgetBuffer;
            return true;

            void ScaleAimviewPoint(SKPoint original, ref SKPoint result)
            {
                result.X = original.X * scaleX;
                result.Y = original.Y * scaleY;
            }
        }

        /// <summary>
        /// All Skeleton Bones for ESP Drawing.
        /// </summary>
        public enum SkeletonBones : uint
        {
            Head = EftDmaRadarLite.Unity.Bones.HumanHead,
            Neck = EftDmaRadarLite.Unity.Bones.HumanNeck,
            UpperTorso = EftDmaRadarLite.Unity.Bones.HumanSpine3,
            MidTorso = EftDmaRadarLite.Unity.Bones.HumanSpine2,
            LowerTorso = EftDmaRadarLite.Unity.Bones.HumanSpine1,
            LeftShoulder = EftDmaRadarLite.Unity.Bones.HumanLCollarbone,
            RightShoulder = EftDmaRadarLite.Unity.Bones.HumanRCollarbone,
            LeftElbow = EftDmaRadarLite.Unity.Bones.HumanLForearm2,
            RightElbow = EftDmaRadarLite.Unity.Bones.HumanRForearm2,
            LeftHand = EftDmaRadarLite.Unity.Bones.HumanLPalm,
            RightHand = EftDmaRadarLite.Unity.Bones.HumanRPalm,
            Pelvis = EftDmaRadarLite.Unity.Bones.HumanPelvis,
            LeftKnee = EftDmaRadarLite.Unity.Bones.HumanLThigh2,
            RightKnee = EftDmaRadarLite.Unity.Bones.HumanRThigh2,
            LeftFoot = EftDmaRadarLite.Unity.Bones.HumanLFoot,
            RightFoot = EftDmaRadarLite.Unity.Bones.HumanRFoot
        }
    }
}
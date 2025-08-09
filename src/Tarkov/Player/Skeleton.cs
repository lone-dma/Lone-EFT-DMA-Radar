using eft_dma_radar.Tarkov.GameWorld;
using eft_dma_radar.Unity;

namespace eft_dma_radar.Tarkov.Player
{
    /// <summary>
    /// Contains abstractions for drawing Player Skeletons.
    /// </summary>
    public sealed class Skeleton
    {
        private const int JOINTS_COUNT = 26;

        /// <summary>
        /// Bones Buffer for ESP Widget.
        /// </summary>
        public static readonly SKPoint[] ESPWidgetBuffer = new SKPoint[JOINTS_COUNT];
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

        public Skeleton(PlayerBase player, Func<Bones, uint[]> getTransformChainFunc)
        {
            _player = player;
            var tiRoot = Memory.ReadPtrChain(player.Base, getTransformChainFunc(Unity.Bones.HumanBase));
            Root = new UnityTransform(tiRoot);
            _ = Root.UpdatePosition();
            var bones = new Dictionary<Bones, UnityTransform>(AllSkeletonBones.Length + 1)
            {
                [eft_dma_radar.Unity.Bones.HumanBase] = Root
            };
            foreach (var bone in AllSkeletonBones.Span)
            {
                var tiBone = Memory.ReadPtrChain(player.Base, getTransformChainFunc(bone));
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
            if (bone is eft_dma_radar.Unity.Bones.HumanBase)
                Root = transform;
            Debug.WriteLine($"[OK] New {bone} Transform for Player '{_player.Name}'");
        }

        /// <summary>
        /// Updates the static ESP Widget Buffer with the current Skeleton Bone Screen Coordinates.<br />
        /// See <see cref="Skeleton.ESPWidgetBuffer"/><br />
        /// NOT THREAD SAFE!
        /// </summary>
        /// <param name="scaleX">X Scale Factor.</param>
        /// <param name="scaleY">Y Scale Factor.</param>
        /// <returns>True if successful, otherwise False.</returns>
        public bool UpdateESPWidgetBuffer(float scaleX, float scaleY)
        {
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
            ScaleAimviewPoint(headScreen, ref ESPWidgetBuffer[index++]);
            ScaleAimviewPoint(neckScreen, ref ESPWidgetBuffer[index++]);
            ScaleAimviewPoint(neckScreen, ref ESPWidgetBuffer[index++]);
            ScaleAimviewPoint(upperTorsoScreen, ref ESPWidgetBuffer[index++]);
            ScaleAimviewPoint(upperTorsoScreen, ref ESPWidgetBuffer[index++]);
            ScaleAimviewPoint(midTorsoScreen, ref ESPWidgetBuffer[index++]);
            ScaleAimviewPoint(midTorsoScreen, ref ESPWidgetBuffer[index++]);
            ScaleAimviewPoint(lowerTorsoScreen, ref ESPWidgetBuffer[index++]);
            ScaleAimviewPoint(lowerTorsoScreen, ref ESPWidgetBuffer[index++]);
            ScaleAimviewPoint(pelvisScreen, ref ESPWidgetBuffer[index++]);
            ScaleAimviewPoint(pelvisScreen, ref ESPWidgetBuffer[index++]);
            ScaleAimviewPoint(leftKneeScreen, ref ESPWidgetBuffer[index++]);
            ScaleAimviewPoint(leftKneeScreen, ref ESPWidgetBuffer[index++]);
            ScaleAimviewPoint(leftFootScreen, ref ESPWidgetBuffer[index++]);
            // Pelvis to right foot
            ScaleAimviewPoint(pelvisScreen, ref ESPWidgetBuffer[index++]);
            ScaleAimviewPoint(rightKneeScreen, ref ESPWidgetBuffer[index++]);
            ScaleAimviewPoint(rightKneeScreen, ref ESPWidgetBuffer[index++]);
            ScaleAimviewPoint(rightFootScreen, ref ESPWidgetBuffer[index++]);
            // Left collar to left hand
            ScaleAimviewPoint(leftCollarScreen, ref ESPWidgetBuffer[index++]);
            ScaleAimviewPoint(leftElbowScreen, ref ESPWidgetBuffer[index++]);
            ScaleAimviewPoint(leftElbowScreen, ref ESPWidgetBuffer[index++]);
            ScaleAimviewPoint(leftHandScreen, ref ESPWidgetBuffer[index++]);
            // Right collar to right hand
            ScaleAimviewPoint(rightCollarScreen, ref ESPWidgetBuffer[index++]);
            ScaleAimviewPoint(rightElbowScreen, ref ESPWidgetBuffer[index++]);
            ScaleAimviewPoint(rightElbowScreen, ref ESPWidgetBuffer[index++]);
            ScaleAimviewPoint(rightHandScreen, ref ESPWidgetBuffer[index++]);
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
            Head = eft_dma_radar.Unity.Bones.HumanHead,
            Neck = eft_dma_radar.Unity.Bones.HumanNeck,
            UpperTorso = eft_dma_radar.Unity.Bones.HumanSpine3,
            MidTorso = eft_dma_radar.Unity.Bones.HumanSpine2,
            LowerTorso = eft_dma_radar.Unity.Bones.HumanSpine1,
            LeftShoulder = eft_dma_radar.Unity.Bones.HumanLCollarbone,
            RightShoulder = eft_dma_radar.Unity.Bones.HumanRCollarbone,
            LeftElbow = eft_dma_radar.Unity.Bones.HumanLForearm2,
            RightElbow = eft_dma_radar.Unity.Bones.HumanRForearm2,
            LeftHand = eft_dma_radar.Unity.Bones.HumanLPalm,
            RightHand = eft_dma_radar.Unity.Bones.HumanRPalm,
            Pelvis = eft_dma_radar.Unity.Bones.HumanPelvis,
            LeftKnee = eft_dma_radar.Unity.Bones.HumanLThigh2,
            RightKnee = eft_dma_radar.Unity.Bones.HumanRThigh2,
            LeftFoot = eft_dma_radar.Unity.Bones.HumanLFoot,
            RightFoot = eft_dma_radar.Unity.Bones.HumanRFoot
        }
    }
}
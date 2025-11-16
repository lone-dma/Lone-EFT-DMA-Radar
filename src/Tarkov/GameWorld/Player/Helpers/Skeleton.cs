/*
 * Lone EFT DMA Radar
 * Brought to you by Lone (Lone DMA)
 * 
MIT License

Copyright (c) 2025 Lone DMA

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 *
*/

using LoneEftDmaRadar.Tarkov.GameWorld.Camera;
using LoneEftDmaRadar.Tarkov.Unity.Structures;

namespace LoneEftDmaRadar.Tarkov.GameWorld.Player.Helpers
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
        private readonly AbstractPlayer _player;

        /// <summary>
        /// Skeleton Root Transform.
        /// </summary>
        public UnityTransform Root { get; private set; }

        /// <summary>
        /// All Transforms for this Skeleton (including Root).
        /// </summary>
        public IReadOnlyDictionary<Bones, UnityTransform> Bones => _bones;

        public Skeleton(AbstractPlayer player, ulong transformInternal)
        {
            _player = player;
            //Span<uint> tiOffsets = stackalloc uint[AbstractPlayer.TransformInternalChainCount];
            //getTransformChainFunc(Unity.Structures.Bones.HumanBase, tiOffsets);
            Root = new UnityTransform(transformInternal);
            _ = Root.UpdatePosition();
            var bones = new Dictionary<Bones, UnityTransform>(AllSkeletonBones.Length + 1);
            bones[Unity.Structures.Bones.HumanBase] = Root;
            //{
            //    [Unity.Structures.Bones.HumanBase] = Root
            //};
            //foreach (var bone in AllSkeletonBones.Span)
            //{
            //    getTransformChainFunc(bone, tiOffsets);
            //    var tiBone = Memory.ReadPtrChain(player.Base, true, tiOffsets);
            //    bones[bone] = new UnityTransform(tiBone);
            //}
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
            if ((bone is Unity.Structures.Bones.HumanBase))
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
            if (!CameraManager.WorldToScreen(in _bones[Unity.Structures.Bones.HumanSpine2].Position, out var midTorsoScreen, true, true))
                return false;
            if (!CameraManager.WorldToScreen(in _bones[Unity.Structures.Bones.HumanHead].Position, out var headScreen))
                return false;
            if (!CameraManager.WorldToScreen(in _bones[Unity.Structures.Bones.HumanNeck].Position, out var neckScreen))
                return false;
            if (!CameraManager.WorldToScreen(in _bones[Unity.Structures.Bones.HumanLCollarbone].Position, out var leftCollarScreen))
                return false;
            if (!CameraManager.WorldToScreen(in _bones[Unity.Structures.Bones.HumanRCollarbone].Position, out var rightCollarScreen))
                return false;
            if (!CameraManager.WorldToScreen(in _bones[Unity.Structures.Bones.HumanLPalm].Position, out var leftHandScreen))
                return false;
            if (!CameraManager.WorldToScreen(in _bones[Unity.Structures.Bones.HumanRPalm].Position, out var rightHandScreen))
                return false;
            if (!CameraManager.WorldToScreen(in _bones[Unity.Structures.Bones.HumanSpine3].Position, out var upperTorsoScreen))
                return false;
            if (!CameraManager.WorldToScreen(in _bones[Unity.Structures.Bones.HumanSpine1].Position, out var lowerTorsoScreen))
                return false;
            if (!CameraManager.WorldToScreen(in _bones[Unity.Structures.Bones.HumanPelvis].Position, out var pelvisScreen))
                return false;
            if (!CameraManager.WorldToScreen(in _bones[Unity.Structures.Bones.HumanLFoot].Position, out var leftFootScreen))
                return false;
            if (!CameraManager.WorldToScreen(in _bones[Unity.Structures.Bones.HumanRFoot].Position, out var rightFootScreen))
                return false;
            if (!CameraManager.WorldToScreen(in _bones[Unity.Structures.Bones.HumanLThigh2].Position, out var leftKneeScreen))
                return false;
            if (!CameraManager.WorldToScreen(in _bones[Unity.Structures.Bones.HumanRThigh2].Position, out var rightKneeScreen))
                return false;
            if (!CameraManager.WorldToScreen(in _bones[Unity.Structures.Bones.HumanLForearm2].Position, out var leftElbowScreen))
                return false;
            if (!CameraManager.WorldToScreen(in _bones[Unity.Structures.Bones.HumanRForearm2].Position, out var rightElbowScreen))
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
            Head = Unity.Structures.Bones.HumanHead,
            Neck = Unity.Structures.Bones.HumanNeck,
            UpperTorso = Unity.Structures.Bones.HumanSpine3,
            MidTorso = Unity.Structures.Bones.HumanSpine2,
            LowerTorso = Unity.Structures.Bones.HumanSpine1,
            LeftShoulder = Unity.Structures.Bones.HumanLCollarbone,
            RightShoulder = Unity.Structures.Bones.HumanRCollarbone,
            LeftElbow = Unity.Structures.Bones.HumanLForearm2,
            RightElbow = Unity.Structures.Bones.HumanRForearm2,
            LeftHand = Unity.Structures.Bones.HumanLPalm,
            RightHand = Unity.Structures.Bones.HumanRPalm,
            Pelvis = Unity.Structures.Bones.HumanPelvis,
            LeftKnee = Unity.Structures.Bones.HumanLThigh2,
            RightKnee = Unity.Structures.Bones.HumanRThigh2,
            LeftFoot = Unity.Structures.Bones.HumanLFoot,
            RightFoot = Unity.Structures.Bones.HumanRFoot
        }
    }
}
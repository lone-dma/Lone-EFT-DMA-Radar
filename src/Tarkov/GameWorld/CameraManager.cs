/*
 * EFT DMA Radar Lite
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

using EftDmaRadarLite.DMA;
using EftDmaRadarLite.ESP;
using EftDmaRadarLite.Misc;
using EftDmaRadarLite.Tarkov.Player;
using EftDmaRadarLite.Unity;
using EftDmaRadarLite.Unity.Collections;
using EftDmaRadarLite.Unity.Structures;
using System.Drawing;
using VmmSharpEx;
using VmmSharpEx.Scatter;

namespace EftDmaRadarLite.Tarkov.GameWorld
{
    public sealed class CameraManager
    {
        private static ulong _opticCameraManagerField;

        /// <summary>
        /// FPS Camera (unscoped).
        /// </summary>
        public ulong FPSCamera { get; }
        /// <summary>
        /// Optic Camera (ads/scoped).
        /// </summary>
        public ulong OpticCamera { get; }
        /// <summary>
        /// True if Optic Camera is currently active.
        /// </summary>
        private bool OpticCameraActive => Memory.ReadValue<bool>(OpticCamera + MonoBehaviour.IsAddedOffset, false);

        public CameraManager()
        {
            var ocmContainer = Memory.ReadPtr(_opticCameraManagerField + Offsets.OpticCameraManagerContainer.Instance, false);
            var fps = Memory.ReadPtr(ocmContainer + Offsets.OpticCameraManagerContainer.FPSCamera, false);
            if (ObjectClass.ReadName(fps, 32, false) != "Camera")
                throw new ArgumentOutOfRangeException(nameof(fps));
            FPSCamera = Memory.ReadPtr(fps + 0x10, false);
            var ocm = Memory.ReadPtr(ocmContainer + Offsets.OpticCameraManagerContainer.OpticCameraManager, false);
            var optic = Memory.ReadPtr(ocm + Offsets.OpticCameraManager.Camera, false);
            if (ObjectClass.ReadName(optic, 32, false) != "Camera")
                throw new ArgumentOutOfRangeException(nameof(optic));
            OpticCamera = Memory.ReadPtr(optic + 0x10, false);
        }

        static CameraManager()
        {
            MemDMA.ProcessStarting += MemDMA_ProcessStarting;
            MemDMA.ProcessStopped += MemDMA_ProcessStopped;
        }

        private static void MemDMA_ProcessStarting(object sender, EventArgs e)
        {
            _opticCameraManagerField = MonoLib.MonoClass.Find("Assembly-CSharp", ClassNames.OpticCameraManagerContainer.ClassName, out _).GetStaticFieldData();
            _opticCameraManagerField.ThrowIfInvalidVirtualAddress(nameof(_opticCameraManagerField));
        }

        private static void MemDMA_ProcessStopped(object sender, EventArgs e)
        {
            _opticCameraManagerField = default;
        }

        /// <summary>
        /// Checks if the Optic Camera is active and there is an active scope zoom level greater than 1.
        /// </summary>
        /// <returns>True if scoped in, otherwise False.</returns>
        private bool CheckIfScoped(LocalPlayer localPlayer)
        {
            try
            {
                if (localPlayer is null)
                    return false;
                if (OpticCameraActive)
                {
                    var opticsPtr = Memory.ReadPtr(localPlayer.PWA + Offsets.ProceduralWeaponAnimation._optics);
                    using var optics = UnityList<VmmPointer>.Create(opticsPtr, true);
                    if (optics.Count > 0)
                    {
                        var pSightComponent = Memory.ReadPtr(optics[0] + Offsets.SightNBone.Mod);
                        var sightComponent = Memory.ReadValue<SightComponent>(pSightComponent);

                        if (sightComponent.ScopeZoomValue != 0f)
                            return sightComponent.ScopeZoomValue > 0f; // TODO: Scope fix, should work but may need to tweak
                        return sightComponent.GetZoomLevel() > 1f; // Make sure we're actually zoomed in
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CheckIfScoped() ERROR: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Executed on each Realtime Loop.
        /// </summary>
        /// <param name="index">Scatter read index dedicated to this object.</param>
        public void OnRealtimeLoop(ScatterReadIndex index, /* Can Be Null */ LocalPlayer localPlayer)
        {
            IsADS = localPlayer?.CheckIfADS() ?? false;
            IsScoped = IsADS && CheckIfScoped(localPlayer);
            ulong vmAddr = IsADS && IsScoped
                ? OpticCamera + UnitySDK.Camera.ViewMatrix
                : FPSCamera + UnitySDK.Camera.ViewMatrix;
            index.AddValueEntry<Matrix4x4>(0, vmAddr); // View Matrix
            index.Completed += (sender, x1) =>
            {
                ref Matrix4x4 vm = ref x1.GetValueRef<Matrix4x4>(0);
                if (!Unsafe.IsNullRef(ref vm))
                {
                    float zoom = App.Config.EspWidget.Zoom;
                    if (zoom > 1f)
                    {
                        var zoomMat = Matrix4x4.CreateScale(zoom, zoom, 1f);
                        vm *= zoomMat; // Apply Zoom
                    }
                    _viewMatrix.Update(ref vm);
                }
            };
            if (IsScoped)
            {
                index.AddValueEntry<float>(1, FPSCamera + UnitySDK.Camera.FOV); // FOV
                index.AddValueEntry<float>(2, FPSCamera + UnitySDK.Camera.AspectRatio); // Aspect
                index.Completed += (sender, x2) =>
                {
                    if (x2.TryGetValue<float>(1, out var fov))
                        _fov = fov;
                    if (x2.TryGetValue<float>(2, out var aspect))
                        _aspect = aspect;
                };
            }
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        private readonly struct SightComponent // (Type: EFT.InventoryLogic.SightComponent)

        {
            [FieldOffset((int)Offsets.SightComponent._template)] private readonly ulong pSightInterface;

            [FieldOffset((int)Offsets.SightComponent.ScopesSelectedModes)] private readonly ulong pScopeSelectedModes;

            [FieldOffset((int)Offsets.SightComponent.SelectedScope)] private readonly int SelectedScope;

            [FieldOffset((int)Offsets.SightComponent.ScopeZoomValue)] public readonly float ScopeZoomValue;

            public readonly float GetZoomLevel()
            {
                using var zoomArray = SightInterface.Zooms;
                if (SelectedScope >= zoomArray.Count || SelectedScope is < 0 or > 10)
                    return -1.0f;
                using var selectedScopeModes = UnityArray<int>.Create(pScopeSelectedModes, false);
                int selectedScopeMode = SelectedScope >= selectedScopeModes.Count ?
                    0 : selectedScopeModes[SelectedScope];
                ulong zoomAddr = zoomArray[SelectedScope] + UnityArray<float>.ArrBaseOffset + (uint)selectedScopeMode * 0x4;

                float zoomLevel = Memory.ReadValue<float>(zoomAddr, false);
                if (zoomLevel.IsNormalOrZero() && zoomLevel is >= 0f and < 100f)
                    return zoomLevel;

                return -1.0f;
            }

            public readonly SightInterface SightInterface =>
                Memory.ReadValue<SightInterface>(pSightInterface);
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        private readonly struct SightInterface // _template (Type: -.GInterfaceBB26)

        {
            [FieldOffset((int)Offsets.SightInterface.Zooms)] private readonly ulong pZooms;

            public readonly UnityArray<ulong> Zooms =>
                UnityArray<ulong>.Create(pZooms, true);
        }

        #region Static Interfaces

        private const int VIEWPORT_TOLERANCE = 800;
        private static readonly Lock _viewportSync = new();

        /// <summary>
        /// True if ESP is currently rendering.
        /// </summary>
        public static bool EspRunning { get; set; }
        /// <summary>
        /// Game Viewport (Monitor Coordinates).
        /// </summary>
        public static Rectangle Viewport { get; private set; }
        /// <summary>
        /// Center of Game Viewport.
        /// </summary>
        public static SKPoint ViewportCenter => new SKPoint(Viewport.Width / 2f, Viewport.Height / 2f);
        /// <summary>
        /// True if LocalPlayer's Optic Camera is active (scope).
        /// </summary>
        public static bool IsScoped { get; private set; }
        /// <summary>
        /// True if LocalPlayer is Aiming Down Sights (any sight/scope/irons).
        /// </summary>
        public static bool IsADS { get; private set; }

        private static float _fov;
        private static float _aspect;
        private static readonly ViewMatrix _viewMatrix = new();

        /// <summary>
        /// Update the Viewport Dimensions for Camera Calculations.
        /// </summary>
        public static void UpdateViewportRes()
        {
            lock (_viewportSync)
            {
                Viewport = new Rectangle(0, 0, App.Config.EspWidget.MonitorWidth, App.Config.EspWidget.MonitorHeight);
            }
        }


        /// <summary>
        /// Translates 3D World Positions to 2D Screen Positions.
        /// </summary>
        /// <param name="worldPos">Entity's world position.</param>
        /// <param name="scrPos">Entity's screen position.</param>
        /// <param name="onScreenCheck">Check if the screen positions are 'on screen'. Returns false if off screen.</param>
        /// <returns>True if successful, otherwise False.</returns>
        public static bool WorldToScreen(ref Vector3 worldPos, out SKPoint scrPos, bool onScreenCheck = false, bool useTolerance = false)
        {
            float w = Vector3.Dot(_viewMatrix.Translation, worldPos) + _viewMatrix.M44; // Transposed

            if (w < 0.098f)
            {
                scrPos = default;
                return false;
            }

            float x = Vector3.Dot(_viewMatrix.Right, worldPos) + _viewMatrix.M14; // Transposed
            float y = Vector3.Dot(_viewMatrix.Up, worldPos) + _viewMatrix.M24; // Transposed

            if (IsScoped)
            {
                float angleRadHalf = (MathF.PI / 180f) * _fov * 0.5f;
                float angleCtg = MathF.Cos(angleRadHalf) / MathF.Sin(angleRadHalf);

                x /= angleCtg * _aspect * 0.5f;
                y /= angleCtg * 0.5f;
            }

            var center = ViewportCenter;
            scrPos = new()
            {
                X = center.X * (1f + x / w),
                Y = center.Y * (1f - y / w)
            };
            if (onScreenCheck)
            {
                int left = useTolerance ? Viewport.Left - VIEWPORT_TOLERANCE : Viewport.Left;
                int right = useTolerance ? Viewport.Right + VIEWPORT_TOLERANCE : Viewport.Right;
                int top = useTolerance ? Viewport.Top - VIEWPORT_TOLERANCE : Viewport.Top;
                int bottom = useTolerance ? Viewport.Bottom + VIEWPORT_TOLERANCE : Viewport.Bottom;
                // Check if the screen position is within the screen boundaries
                if (scrPos.X < left || scrPos.X > right ||
                    scrPos.Y < top || scrPos.Y > bottom)
                {
                    scrPos = default;
                    return false;
                }
            }
            return true;
        }
        #endregion
    }
}
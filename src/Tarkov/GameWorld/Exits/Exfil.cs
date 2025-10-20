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

using LoneEftDmaRadar.Misc;
using LoneEftDmaRadar.Mono.Collections;
using LoneEftDmaRadar.Tarkov.Data;
using LoneEftDmaRadar.Tarkov.Player;
using LoneEftDmaRadar.UI.Radar.Maps;
using LoneEftDmaRadar.UI.Skia;
using LoneEftDmaRadar.Unity;
using LoneEftDmaRadar.Unity.Structures;

namespace LoneEftDmaRadar.Tarkov.GameWorld.Exits
{
    public class Exfil : IExitPoint, IWorldEntity, IMapEntity, IMouseoverEntity
    {
        public static implicit operator ulong(Exfil x) => x._addr;
        private static readonly uint[] _transformInternalChain =
{
            ObjectClass.MonoBehaviourOffset, MonoBehaviour.GameObjectOffset, GameObject.ComponentsOffset, 0x8
        };

        private readonly bool _isPMC;
        private HashSet<string> PmcEntries { get; } = new(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> ScavIds { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Exfil(ulong baseAddr, bool isPMC)
        {
            _addr = baseAddr;
            _isPMC = isPMC;
            var transformInternal = Memory.ReadPtrChain(baseAddr, false, _transformInternalChain);
            var namePtr = Memory.ReadPtrChain(baseAddr, true, Offsets.Exfil.Settings, Offsets.ExfilSettings.Name);
            Name = Memory.ReadUnicodeString(namePtr)?.Trim();
            if (string.IsNullOrEmpty(Name))
                Name = "default";
            // Lookup real map name (if possible)
            if (StaticGameData.ExfilNames.TryGetValue(Memory.MapID, out var mapExfils)
                && mapExfils.TryGetValue(Name, out var exfilName))
                Name = exfilName;
            _position = new UnityTransform(transformInternal).UpdatePosition();
        }

        private readonly ulong _addr;
        public string Name { get; }
        public virtual EStatus Status { get; private set; } = EStatus.Closed;

        /// <summary>
        /// Update Exfil Information/Status.
        /// </summary>
        public virtual void Update(Enums.EExfiltrationStatus status)
        {
            /// Update Status
            switch (status)
            {
                case Enums.EExfiltrationStatus.NotPresent:
                    Status = EStatus.Closed;
                    break;
                case Enums.EExfiltrationStatus.UncompleteRequirements:
                    Status = EStatus.Pending;
                    break;
                case Enums.EExfiltrationStatus.Countdown:
                    Status = EStatus.Open;
                    break;
                case Enums.EExfiltrationStatus.RegularMode:
                    Status = EStatus.Open;
                    break;
                case Enums.EExfiltrationStatus.Pending:
                    Status = EStatus.Pending;
                    break;
                case Enums.EExfiltrationStatus.AwaitsManualActivation:
                    Status = EStatus.Pending;
                    break;
                case Enums.EExfiltrationStatus.Hidden:
                    Status = EStatus.Pending;
                    break;
            }
            /// Update Entry Points
            if (_isPMC)
            {
                var entriesArrPtr = Memory.ReadPtr(_addr + Offsets.Exfil.EligibleEntryPoints);
                using var entriesArr = MonoArray<ulong>.Create(entriesArrPtr, true);
                foreach (var entryNamePtr in entriesArr)
                {
                    var entryName = Memory.ReadUnicodeString(entryNamePtr);
                    PmcEntries.Add(entryName);
                }
            }
            else // Scav Exfils
            {
                var eligibleIdsPtr = Memory.ReadPtr(_addr + Offsets.ScavExfil.EligibleIds);
                using var idsArr = MonoList<ulong>.Create(eligibleIdsPtr, true);
                foreach (var idPtr in idsArr)
                {
                    var idName = Memory.ReadUnicodeString(idPtr);
                    ScavIds.Add(idName);
                }
            }
        }

        #region Interfaces

        private readonly Vector3 _position;
        public ref readonly Vector3 Position => ref _position;
        public Vector2 MouseoverPosition { get; set; }

        public void Draw(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            var heightDiff = Position.Y - localPlayer.Position.Y;
            var paint = GetPaint();
            var point = Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams);
            MouseoverPosition = new Vector2(point.X, point.Y);
            SKPaints.ShapeOutline.StrokeWidth = 2f;
            if (heightDiff > 1.85f) // exfil is above player
            {
                using var path = point.GetUpArrow(6.5f);
                canvas.DrawPath(path, SKPaints.ShapeOutline);
                canvas.DrawPath(path, paint);
            }
            else if (heightDiff < -1.85f) // exfil is below player
            {
                using var path = point.GetDownArrow(6.5f);
                canvas.DrawPath(path, SKPaints.ShapeOutline);
                canvas.DrawPath(path, paint);
            }
            else // exfil is level with player
            {
                float size = 4.75f * App.Config.UI.UIScale;
                canvas.DrawCircle(point, size, SKPaints.ShapeOutline);
                canvas.DrawCircle(point, size, paint);
            }
        }

        public virtual SKPaint GetPaint()
        {
            var localPlayer = Memory.LocalPlayer;
            if (localPlayer is not null && localPlayer.IsPmc &&
                !PmcEntries.Contains(localPlayer.EntryPoint ?? "NULL"))
                return SKPaints.PaintExfilInactive;
            if (localPlayer is not null && localPlayer.IsScav &&
                !ScavIds.Contains(localPlayer.ProfileId))
                return SKPaints.PaintExfilInactive;
            switch (Status)
            {
                case EStatus.Open:
                    return SKPaints.PaintExfilOpen;
                case EStatus.Pending:
                    return SKPaints.PaintExfilPending;
                case EStatus.Closed:
                    return SKPaints.PaintExfilClosed;
                default:
                    return SKPaints.PaintExfilClosed;
            }
        }

        public void DrawMouseover(SKCanvas canvas, EftMapParams mapParams, LocalPlayer localPlayer)
        {
            var exfilName = Name;
            exfilName ??= "unknown";
            Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams).DrawMouseoverText(canvas, $"{exfilName} ({Status.ToString()})");
        }

        #endregion

        public enum EStatus
        {
            Open,
            Pending,
            Closed
        }
    }
}

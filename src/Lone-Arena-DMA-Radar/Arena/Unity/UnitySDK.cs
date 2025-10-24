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

namespace LoneArenaDmaRadar.Arena.Unity
{
    public readonly struct UnitySDK
    {
        public readonly struct ModuleBase
        {
            public const uint GameObjectManager = 0x1CF93E0; // to eft_dma_radar.GameObjectManager
            public const uint AllCameras = 0x1BF8BC0; // Lookup in IDA 's_AllCamera'
            public const uint InputManager = 0x1C91748;
            public const uint GfxDevice = 0x1CF9F48; // g_MainGfxDevice , Type GfxDeviceClient
        }
        public readonly struct UnityInputManager
        {
            public const uint CurrentKeyState = 0x60; // 0x50 + 0x8
        }
        public readonly struct TransformInternal
        {
            public const uint TransformAccess = 0x38; // to TransformHierarchy
        }
        public readonly struct TransformAccess
        {
            public const uint Vertices = 0x18; // MemList<TrsX>
            public const uint Indices = 0x20; // MemList<int>
        }

        public readonly struct Camera
        {
            // CopiableState struct begins at 0x40
            public const uint ViewMatrix = 0x100;
            public const uint FOV = 0x180;
            public const uint AspectRatio = 0x4F0;
        }

        public readonly struct GfxDeviceClient
        {
            public const uint Viewport = 0x25A0; // m_Viewport      RectT<int> ?
        }

    }
}

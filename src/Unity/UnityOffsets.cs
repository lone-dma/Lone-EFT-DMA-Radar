namespace EftDmaRadarLite.Unity
{
    public readonly struct UnityOffsets
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

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

namespace LoneEftDmaRadar.Tarkov.Unity
{
    public readonly struct UnitySDK
    {
        public readonly struct ShuffledOffsets
        {
            public const uint GameObjectManager = 0x1A1F2F8;

            public const uint GameObject_ObjectClassOffset = 0x50;
            public const uint GameObject_ComponentsOffset = 0x58;
            public const uint GameObject_NameOffset = 0x78;

            public const uint MonoBehaviour_ObjectClassOffset = 0x50;
            public const uint MonoBehaviour_GameObjectOffset = 0x58;

            public const uint TransformAccess_IndexOffset = 0x80;
            public const uint TransformAccess_HierarchyOffset = 0x78;

            public const uint Hierarchy_VerticesOffset = 0x80;
            public const uint Hierarchy_IndicesOffset = 0x50;
            public const uint Hierarchy_RootPositionOffset = 0xB0;

            public static readonly uint[] GameWorldChain =
            [
                0x48,
                0x18,
                0x28
            ];

            public static readonly uint[] TransformChain =
            [
                0x10,
                0x58,
                0x58,
                0x8,
                0x50,
                0x10
            ];
        }
    }
}

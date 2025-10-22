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

using LoneEftDmaRadar.Tarkov;

namespace LoneEftDmaRadar.UI.Radar.Maps
{
    /// <summary>
    /// Defines a .JSON Map Config File
    /// </summary>
    public sealed class EftMapConfig
    {
        /// <summary>
        /// Name of map (Ex: CUSTOMS)
        /// </summary>
        [JsonIgnore]
        public string Name =>
            StaticGameData.MapNames[MapID[0]].ToUpper();

        /// <summary>
        /// Map ID(s) for this Map.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("mapID")]
        public List<string> MapID { get; private set; }
        /// <summary>
        /// Bitmap 'X' Coordinate of map 'Origin Location' (where Unity X is 0).
        /// </summary>
        [JsonPropertyName("x")]
        public float X { get; set; }
        /// <summary>
        /// Bitmap 'Y' Coordinate of map 'Origin Location' (where Unity Y is 0).
        /// </summary>
        [JsonPropertyName("y")]
        public float Y { get; set; }
        /// <summary>
        /// Arbitrary scale value to align map scale between the Bitmap and Game Coordinates.
        /// </summary>
        [JsonPropertyName("scale")]
        public float Scale { get; set; }
        /// <summary>
        /// How much to scale up the original SVG Image.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("svgScale")]
        public float SvgScale { get; private set; }
        /// <summary>
        /// TRUE if the map drawing should not dim layers, otherwise FALSE if dimming is permitted.
        /// This is a global setting that applies to all layers.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("disableDimming")]
        public bool DisableDimming { get; private set; }
        /// <summary>
        /// Contains the Map Layers to load for the current Map Configuration.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("mapLayers")]
        public List<Layer> MapLayers { get; private set; }

        /// <summary>
        /// A single layer of a Multi-Layered Map.
        /// </summary>
        public sealed class Layer
        {
            /// <summary>
            /// Minimum height (Unity Y Coord) for this map layer.
            /// NULL: No minimum height.
            /// </summary>
            [JsonInclude]
            [JsonPropertyName("minHeight")]
            public float? MinHeight { get; private set; }
            /// <summary>
            /// Maximum height (Unity Y Coord) for this map layer.
            /// NULL: No maximum height.
            /// </summary>
            [JsonInclude]
            [JsonPropertyName("maxHeight")]
            public float? MaxHeight { get; private set; }
            /// <summary>
            /// TRUE if when this layer is in the foreground, the lower layers cannot be dimmed. Otherwise FALSE.
            /// </summary>
            [JsonInclude]
            [JsonPropertyName("cannotDimLowerLayers")]
            public bool CannotDimLowerLayers { get; private set; }
            /// <summary>
            /// Relative File path to this map layer's PNG Image.
            /// </summary>
            [JsonInclude]
            [JsonPropertyName("filename")]
            public string Filename { get; private set; }
        }
    }
}

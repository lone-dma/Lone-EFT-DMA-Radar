﻿/*
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

using MessagePack.Formatters;
using MessagePack;
using MessagePack.Resolvers;

namespace EftDmaRadarLite.WebRadar.MessagePack
{
    public static class ResolverGenerator
    {
        public static IFormatterResolver Instance { get; }

        static ResolverGenerator()
        {
            Instance = CompositeResolver.Create(StandardResolver.Instance, CustomResolver.Instance);
        }

        private sealed class CustomResolver : IFormatterResolver
        {
            public static CustomResolver Instance { get; } = new();

            private readonly Vector2Formatter _vector2Formatter = new();
            private readonly Vector3Formatter _vector3Formatter = new();

            public IMessagePackFormatter<T> GetFormatter<T>()
            {
                if (_vector2Formatter is IMessagePackFormatter<T> f1)
                    return f1;
                if (_vector3Formatter is IMessagePackFormatter<T> f2)
                    return f2;

                return null; // No match found
            }
        }
    }
}

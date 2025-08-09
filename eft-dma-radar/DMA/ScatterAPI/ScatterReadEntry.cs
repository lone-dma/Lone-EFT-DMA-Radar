using eft_dma_radar.Misc;
using eft_dma_radar.Misc.Pools;
using static eft_dma_radar.Unity.UnityTransform;

namespace eft_dma_radar.DMA.ScatterAPI
{
    public sealed class ScatterReadEntry<T> : IScatterEntry, IPooledObject
    {
        private static readonly bool _isValueType = !RuntimeHelpers.IsReferenceOrContainsReferences<T>();
        private T _result;
        /// <summary>
        /// Result for this read. Should only be called from the Index.
        /// Be sure to check the IsFailed flag.
        /// </summary>
        internal ref T Result => ref _result;
        /// <summary>
        /// Virtual Address to read from.
        /// </summary>
        public ulong Address { get; private set; }
        /// <summary>
        /// Count of bytes to read.
        /// </summary>
        public int CB { get; private set; }
        /// <summary>
        /// True if this read has failed, otherwise False.
        /// </summary>
        public bool IsFailed { get; set; }

        [Obsolete("You must lease this object via Lease()")]
        public ScatterReadEntry() { }

        /// <summary>
        /// Get a Scatter Read Entry from the Object Pool.
        /// </summary>
        /// <param name="address">Virtual Address to read from.</param>
        /// <param name="cb">Count of bytes to read.</param>
        /// <returns>Rented ScatterReadEntry <typeparamref name="T"/> instance.</returns>
        public static ObjectPoolLease<ScatterReadEntry<T>> Lease(ulong address, int cb, out ScatterReadEntry<T> value)
        {
            var lease = ObjectPoolLease<ScatterReadEntry<T>>.Create(out value);
            value.Configure(address, cb);
            return lease;
        }

        /// <summary>
        /// Configure this entry.
        /// </summary>
        /// <param name="address">Virtual Address to read from.</param>
        /// <param name="cb">Count of bytes to read.</param>
        private void Configure(ulong address, int cb)
        {
            Address = address;
            if (cb == 0 && _isValueType)
                cb = SizeChecker<T>.Size;
            CB = cb;
        }

        /// <summary>
        /// Parse the memory buffer and set the result value.
        /// Only called internally via API.
        /// </summary>
        /// <param name="hScatter">Scatter read handle.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetResult(Vmmsharp.LeechCore.SCATTER_HANDLE hScatter)
        {
            try
            {
                if (_isValueType)
                    SetValueResult(hScatter);
                else
                    SetClassResult(hScatter);
            }
            catch
            {
                IsFailed = true;
            }
        }

        /// <summary>
        /// Set the Result from a Value Type.
        /// </summary>
        /// <param name="hScatter">Scatter read handle.</param>
        private unsafe void SetValueResult(Vmmsharp.LeechCore.SCATTER_HANDLE hScatter)
        {
            int cb = SizeChecker<T>.Size; // Also enforces type safety
#pragma warning disable CS8500
            fixed (void* pb = &_result)
            {
                var buffer = new Span<byte>(pb, cb);
                if (!ProcessBytes(hScatter, buffer))
                {
                    IsFailed = true;
                    return;
                }
            }
#pragma warning restore CS8500
            if (_result is MemPointer memPtrResult && !MemDMA.IsValidVirtualAddress(memPtrResult))
            {
                IsFailed = true;
            }
        }

        /// <summary>
        /// Set the Result from a Class Type.
        /// </summary>
        /// <param name="hScatter">Scatter read handle.</param>
        private void SetClassResult(Vmmsharp.LeechCore.SCATTER_HANDLE hScatter)
        {
            if (this is ScatterReadEntry<SharedArray<TrsX>> r1) // vertices
            {
                int size = SizeChecker<TrsX>.Size;
                ArgumentOutOfRangeException.ThrowIfNotEqual(CB % size, 0, nameof(CB));
                int count = CB / size;
                var vert = SharedArray<TrsX>.Get(count);
                if (!ProcessBytes(hScatter, vert.Span))
                {
                    vert.Return();
                    IsFailed = true;
                }
                else
                {
                    r1._result = vert;
                }
            }
            else if (this is ScatterReadEntry<UnicodeString> r2) // UTF-16
            {
                Span<byte> buffer = CB > 0x1000 ? new byte[CB] : stackalloc byte[CB];
                buffer.Clear();
                if (!ProcessBytes(hScatter, buffer))
                {
                    IsFailed = true;
                    return;
                }
                var nullIndex = buffer.FindUtf16NullTerminatorIndex();
                UnicodeString value = nullIndex >= 0 ?
                    Encoding.Unicode.GetString(buffer.Slice(0, nullIndex)) : Encoding.Unicode.GetString(buffer);
                r2._result = value;
            }
            else if (this is ScatterReadEntry<UTF8String> r3) // UTF-8
            {
                Span<byte> buffer = CB > 0x1000 ? new byte[CB] : stackalloc byte[CB];
                buffer.Clear();
                if (!ProcessBytes(hScatter, buffer))
                {
                    IsFailed = true;
                    return;
                }
                var nullIndex = buffer.IndexOf((byte)0);
                UTF8String value = nullIndex >= 0 ?
                    Encoding.UTF8.GetString(buffer.Slice(0, nullIndex)) : Encoding.UTF8.GetString(buffer);
                r3._result = value;
            }
            else
                throw new NotImplementedException($"Type {typeof(T)} not supported!");
        }

        /// <summary>
        /// Process the Scatter Read bytes into the result buffer.
        /// *Callers should verify buffer size*
        /// </summary>
        /// <typeparam name="TBuf">Buffer type</typeparam>
        /// <param name="hScatter">Scatter read handle.</param>
        /// <param name="bufferIn">Result buffer</param>
        /// <exception cref="Exception"></exception>
        private bool ProcessBytes<TBuf>(Vmmsharp.LeechCore.SCATTER_HANDLE hScatter, Span<TBuf> bufferIn)
            where TBuf : unmanaged
        {
            var buffer = MemoryMarshal.Cast<TBuf, byte>(bufferIn);
            uint pageOffset = MemDMA.BYTE_OFFSET(Address); // Get object offset from the page start address

            var bytesCopied = 0; // track number of bytes copied to ensure nothing is missed
            uint cb = Math.Min((uint)CB, 0x1000 - pageOffset); // bytes to read this page

            uint numPages =
                MemDMA.ADDRESS_AND_SIZE_TO_SPAN_PAGES(Address,
                    (uint)CB); // number of pages to read from (in case result spans multiple pages)
            ulong basePageAddr = MemDMA.PAGE_ALIGN(Address);

            for (int p = 0; p < numPages; p++)
            {
                ulong pageAddr = basePageAddr + 0x1000 * (uint)p; // get current page addr
                if (hScatter.Results.TryGetValue(pageAddr, out var scatter)) // retrieve page of mem needed
                {
                    scatter.Page
                        .Slice((int)pageOffset, (int)cb)
                        .CopyTo(buffer.Slice(bytesCopied, (int)cb)); // Copy bytes to buffer
                    bytesCopied += (int)cb;
                }
                else // read failed -> break
                    return false;

                cb = 0x1000; // set bytes to read next page
                if (bytesCopied + cb > CB) // partial chunk last page
                    cb = (uint)CB - (uint)bytesCopied;

                pageOffset = 0x0; // Next page (if any) should start at 0x0
            }

            if (bytesCopied != CB)
                return false;
            return true;
        }

        public void Return()
        {
            MyObjectPool<ScatterReadEntry<T>>.Instance.Return(this);
        }

        public bool TryReset()
        {
            if (_result is IDisposable disposable)
                disposable.Dispose();
            if (_result is IPooledObject pooled)
                pooled.Return();
            _result = default;
            Address = default;
            CB = default;
            IsFailed = default;
            return true;
        }
    }
}

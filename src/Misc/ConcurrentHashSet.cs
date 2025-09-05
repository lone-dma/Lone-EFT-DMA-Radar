namespace EftDmaRadarLite.Misc
{
    [JsonConverter(typeof(ConcurrentHashSetConverterFactory))]
    public class ConcurrentHashSet<T> : ICollection<T>, IReadOnlyCollection<T>, ISet<T>, IReadOnlySet<T>, IEnumerable<T>
    {
        private const byte PRESENT = 0;
        private readonly ConcurrentDictionary<T, byte> _dict;

        public ConcurrentHashSet() : this(null) { }

        public ConcurrentHashSet(IEqualityComparer<T> comparer)
        {
            _dict = new ConcurrentDictionary<T, byte>(Environment.ProcessorCount, 31, comparer);
        }

        public int Count => _dict.Count;
        public bool IsReadOnly => false;

        public bool Add(T item) => _dict.TryAdd(item, PRESENT);
        void ICollection<T>.Add(T item) => Add(item);

        public bool Remove(T item) => _dict.TryRemove(item, out _);
        public bool Contains(T item) => _dict.ContainsKey(item);
        public void Clear() => _dict.Clear();
        public void CopyTo(T[] array, int arrayIndex) => _dict.Keys.CopyTo(array, arrayIndex);

        public IEnumerator<T> GetEnumerator() => _dict.Keys.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void ExceptWith(IEnumerable<T> other)
        {
            ArgumentNullException.ThrowIfNull(other);
            foreach (var item in other)
                _dict.TryRemove(item, out _);
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            ArgumentNullException.ThrowIfNull(other);
            var otherSet = new HashSet<T>(other, _dict.Comparer);
            foreach (var item in _dict.Keys)
                if (!otherSet.Contains(item))
                    _dict.TryRemove(item, out _);
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            ArgumentNullException.ThrowIfNull(other);
            var otherSet = new HashSet<T>(other, _dict.Comparer);
            int c = Count;
            return c < otherSet.Count
                && _dict.Keys.All(otherSet.Contains);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            ArgumentNullException.ThrowIfNull(other);
            var otherSet = new HashSet<T>(other, _dict.Comparer);
            int c = Count;
            return c > otherSet.Count
                && otherSet.All(Contains);
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            ArgumentNullException.ThrowIfNull(other);
            var otherSet = new HashSet<T>(other, _dict.Comparer);
            return _dict.Keys.All(otherSet.Contains);
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            ArgumentNullException.ThrowIfNull(other);
            return other.All(Contains);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            ArgumentNullException.ThrowIfNull(other);
            return other.Any(Contains);
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            ArgumentNullException.ThrowIfNull(other);
            var otherSet = new HashSet<T>(other, _dict.Comparer);
            if (otherSet.Count != Count) return false;
            return otherSet.All(Contains);
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            ArgumentNullException.ThrowIfNull(other);
            foreach (var item in other)
            {
                if (!_dict.TryRemove(item, out _))
                    _dict.TryAdd(item, PRESENT);
            }
        }

        public void UnionWith(IEnumerable<T> other)
        {
            ArgumentNullException.ThrowIfNull(other);
            foreach (var item in other)
                _dict.TryAdd(item, PRESENT);
        }
    }

    internal sealed class ConcurrentHashSetConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) =>
            typeToConvert.IsGenericType
            && typeToConvert.GetGenericTypeDefinition() == typeof(ConcurrentHashSet<>);

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var itemType = typeToConvert.GetGenericArguments()[0];
            var converterType = typeof(ConcurrentHashSetConverter<>).MakeGenericType(itemType);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }

        private sealed class ConcurrentHashSetConverter<TItem> : JsonConverter<ConcurrentHashSet<TItem>>
        {
            public override ConcurrentHashSet<TItem> Read(
                ref Utf8JsonReader reader,
                Type typeToConvert,
                JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartArray)
                    throw new JsonException();

                var set = new ConcurrentHashSet<TItem>();
                var itemConv = (JsonConverter<TItem>)options.GetConverter(typeof(TItem));

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        return set;
                    var item = itemConv.Read(ref reader, typeof(TItem), options)!;
                    set.Add(item);
                }
                throw new JsonException("Unexpected end of JSON array");
            }

            public override void Write(
                Utf8JsonWriter writer,
                ConcurrentHashSet<TItem> value,
                JsonSerializerOptions options)
            {
                writer.WriteStartArray();
                var itemConv = (JsonConverter<TItem>)options.GetConverter(typeof(TItem));
                foreach (var item in value)
                    itemConv.Write(writer, item, options);
                writer.WriteEndArray();
            }
        }
    }
}

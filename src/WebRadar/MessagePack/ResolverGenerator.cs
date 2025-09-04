using MessagePack.Formatters;
using MessagePack;
using MessagePack.Resolvers;

namespace eft_dma_radar.WebRadar.MessagePack
{
    public static class ResolverGenerator
    {
        public static readonly IFormatterResolver Instance;

        static ResolverGenerator()
        {
            Instance = CompositeResolver.Create(StandardResolver.Instance, CustomResolver.Instance);
        }

        private sealed class CustomResolver : IFormatterResolver
        {
            public static readonly CustomResolver Instance = new();

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

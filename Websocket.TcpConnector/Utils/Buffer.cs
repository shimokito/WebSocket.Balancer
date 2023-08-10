using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace Websocket.Tcp.Server.Utils
{
    public static class Buffer
    {
        public static Buffer<T> RentBuffer<T>(this ArrayPool<T> arrayPool, int minLength)
        {
            return new Buffer<T>(arrayPool, minLength);
        }
    }

    public sealed class Buffer<T> : IDisposable
    {
        private bool _isRented = false;
        private bool _isDisposed = false;
        private readonly ArrayPool<T> _arrayPool;
        private readonly int _bufferSize;
        private T[]? _buffer;

        [MemberNotNullWhen(true, nameof(_isRented))]
        public T[] Data
        {
            get
            {
                ThrowIfDisposed();
                if (!_isRented)
                {
                    _isRented = true;
                    _buffer = _arrayPool.Rent(_bufferSize);
                }

                return _buffer!;
            }
        }

        internal Buffer(ArrayPool<T> pool, int bufferSize)
        {
            _arrayPool = pool;
            _bufferSize = bufferSize;
        }

        public void Clear(bool clearArray = false)
        {
            if (!_isRented)
                return;

            _arrayPool.Return(_buffer!, clearArray);
            _isRented = false;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            Clear(true);
            GC.SuppressFinalize(this);
        }

        public Memory<T> AsMemory()
        {
            return Data.AsMemory(0, _bufferSize);
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
        }
    }
}

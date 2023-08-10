namespace Websocket.Tcp.Proxy.Connections.Core.Utils
{
    internal struct SafeAtomicBoolean
    {
        private const int TrueValue = 1;
        private const int FalseValue = 0;

        public static readonly SafeAtomicBoolean True = new SafeAtomicBoolean(TrueValue);
        public static readonly SafeAtomicBoolean False = new SafeAtomicBoolean(FalseValue);

        private int _value;

        private SafeAtomicBoolean(int value)
        {
            _value = value;
        }

        public void SetTrue()
        {
            Interlocked.Exchange(ref _value, TrueValue);
        }

        public void SetFalse()
        {
            Interlocked.Exchange(ref _value, FalseValue);
        }

        public static implicit operator bool(SafeAtomicBoolean self)
        {
            return self._value == TrueValue;
        }
    }
}

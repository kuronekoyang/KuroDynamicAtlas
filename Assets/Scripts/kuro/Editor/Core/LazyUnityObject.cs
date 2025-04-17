using System;

namespace kuro
{
    public class LazyUnityObject<T> where T : UnityEngine.Object
    {
        private readonly Func<T> _createFunc;
        private bool _didInit;
        private T _value;

        public LazyUnityObject(Func<T> createFunc)
        {
            this._createFunc = createFunc;
        }

        public bool HasValue
        {
            get
            {
                if (!_didInit)
                    return false;
                if (!_value)
                    return false;
                return true;
            }
        }

        public T Value
        {
            get
            {
                EnsureValue();

                return _value;
            }
        }

        public void EnsureValue()
        {
            if (_didInit)
            {
                if (_value == null)
                    _didInit = false;
            }

            if (!_didInit)
            {
                _didInit = true;
                if (_createFunc != null)
                    _value = _createFunc();
            }
        }

        public void ClearValue()
        {
            _didInit = false;
            _value = null;
        }

        public static implicit operator T(LazyUnityObject<T> v) => v.Value;
    }
}
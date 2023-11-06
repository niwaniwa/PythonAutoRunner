using System;

namespace PythonAutoExecuter.PythonAutoExcuter
{
    public static class KadaiDelegateExtensions
    {
        public static IKadaiDelegate ToKadaiDelegate(this Func<object[], bool> action)
        {
            return new AnonymousKadaiDelegate(action);
        }
    }

    internal class AnonymousKadaiDelegate : IKadaiDelegate
    {
        private readonly Func<object[], bool> _action;

        public AnonymousKadaiDelegate(Func<object[], bool> action)
        {
            _action = action;
        }

        public bool Run(params object[] args)
        {
            return _action(args);
        }
    }
}
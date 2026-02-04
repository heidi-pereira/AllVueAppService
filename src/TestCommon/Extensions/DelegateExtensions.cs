using System;

namespace TestCommon.Extensions
{
    public static class DelegateExtensions
    {
        public static TDel Combine<TDel>(this TDel a, TDel b) where TDel : Delegate => (TDel) Delegate.Combine(a, b);
    }
}
using System;
using System.Runtime.CompilerServices;

namespace skysepehru.Core.PropertyRegistry
{
    /// <summary>
    /// Converts an arbitrary enum to the int key the property system stores internally.
    /// Zero-allocation (no boxing); requires the enum's underlying type to be int, which is
    /// the C# default. This is what lets the package stay agnostic of any concrete enum.
    /// </summary>
    internal static class EnumKey
    {
        public static int ToInt<TEnum>(TEnum value) where TEnum : unmanaged, Enum
        {
            return Unsafe.As<TEnum, int>(ref value);
        }

        public static TEnum ToEnum<TEnum>(int value) where TEnum : unmanaged, Enum
        {
            return Unsafe.As<int, TEnum>(ref value);
        }

        /// <summary>
        /// Guards against enums whose underlying type is not <see cref="int"/>. The int reinterpret
        /// above is only valid for 4-byte, int-backed enums; anything else (byte, short, long, ...)
        /// would read the wrong bytes and silently corrupt keys, so we reject it loudly instead.
        /// </summary>
        public static void EnsureInt32Backed<TEnum>() where TEnum : unmanaged, Enum
        {
            if (Enum.GetUnderlyingType(typeof(TEnum)) != typeof(int))
            {
                throw new NotSupportedException(
                    $"Enum '{typeof(TEnum)}' must be backed by System.Int32; " +
                    "the property system stores enum keys as int.");
            }
        }
    }
}

using NUnit.Framework;

namespace skysepehru.Core.PropertyRegistry.Tests.Editor.EnumKeyTests
{
    public class EnumKeyTests
    {
        private const int FiveValue = 5;
        private const int BigValue = 1_000_000;

        private enum Int32Enum { Zero, Five = FiveValue, Big = BigValue }
        private enum ByteEnum : byte { A = 1 }
        private enum ShortEnum : short { A = 1 }
        private enum LongEnum : long { A = 1 }

        [Test]
        public void ToInt_ReturnsUnderlyingValue()
        {
            Assert.That(EnumKey.ToInt(Int32Enum.Five), Is.EqualTo(FiveValue));
            Assert.That(EnumKey.ToInt(Int32Enum.Big), Is.EqualTo(BigValue));
        }

        [Test]
        public void ToEnum_IsTheInverseOfToInt()
        {
            Assert.That(EnumKey.ToEnum<Int32Enum>(FiveValue), Is.EqualTo(Int32Enum.Five));
            Assert.That(EnumKey.ToEnum<Int32Enum>(EnumKey.ToInt(Int32Enum.Big)), Is.EqualTo(Int32Enum.Big));
        }

        [Test]
        public void EnsureInt32Backed_DoesNotThrow_ForInt32Enum()
        {
            Assert.DoesNotThrow(EnumKey.EnsureInt32Backed<Int32Enum>);
        }

        [Test]
        public void EnsureInt32Backed_Throws_ForByteBackedEnum()
        {
            Assert.Catch(EnumKey.EnsureInt32Backed<ByteEnum>);
        }

        [Test]
        public void EnsureInt32Backed_Throws_ForShortBackedEnum()
        {
            Assert.Catch(EnumKey.EnsureInt32Backed<ShortEnum>);
        }

        [Test]
        public void EnsureInt32Backed_Throws_ForLongBackedEnum()
        {
            Assert.Catch(EnumKey.EnsureInt32Backed<LongEnum>);
        }
    }
}

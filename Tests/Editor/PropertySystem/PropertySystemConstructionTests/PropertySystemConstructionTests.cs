using System;
using NUnit.Framework;

namespace skysepehru.Core.PropertyRegistry.Tests.Editor.PropertySystemConstructionTests
{
    public class PropertySystemConstructionTests
    {
        [Test]
        public void Constructor_WithInt32Enums_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _ = new ConstructionTestPropertySystem());
        }

        [Test]
        public void Constructor_NonInt32Entity_Throws()
        {
            Assert.Catch(() => _ = new ConstructionTestPropertySystemEntityInt64PropertyInt32());
        }

        [Test]
        public void Constructor_NonInt32Property_Throws()
        {
            Assert.Catch(() => _ = new ConstructionTestPropertySystemEntityInt32PropertyInt64());
        }

        [Test]
        public void Constructor_NonInt32EntityAndProperty_Throws()
        {
            Assert.Catch(() => _ = new ConstructionTestPropertySystemEntityInt64PropertyInt64());
        }
    }
}
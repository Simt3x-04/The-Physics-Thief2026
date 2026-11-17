using NUnit.Framework;
using PhysicsThief.Player;
using PhysicsThief.Properties;

namespace PhysicsThief.Tests
{
    public class PropertyRulesTests
    {
        [Test]
        public void PastedValuesAreCappedLikeTheGdd()
        {
            Assert.AreEqual(1000f, PropertyRules.Clamp(AttributeType.Mass, 5000f), 0.001f);
            Assert.AreEqual(0.1f, PropertyRules.Clamp(AttributeType.Mass, 0f), 0.001f);
            Assert.AreEqual(50f, PropertyRules.Clamp(AttributeType.Velocity, 99f), 0.001f);
            Assert.AreEqual(500f, PropertyRules.Clamp(AttributeType.Temperature, 700f), 0.001f);
            Assert.AreEqual(2f, PropertyRules.Clamp(AttributeType.Magnetism, 3f), 0.001f);
            Assert.AreEqual(1f, PropertyRules.Clamp(AttributeType.Hardness, 0f), 0.001f);
        }

        [Test]
        public void ValuesInsideTheRangeAreUnchanged()
        {
            Assert.AreEqual(600f, PropertyRules.Clamp(AttributeType.Mass, 600f), 0.001f);
            Assert.AreEqual(0f, PropertyRules.Clamp(AttributeType.Temperature, 0f), 0.001f);
        }

        [Test]
        public void DefaultsMatchAmbientConditions()
        {
            Assert.AreEqual(20f, PropertyRules.DefaultValue(AttributeType.Temperature), 0.001f);
            Assert.AreEqual(0f, PropertyRules.DefaultValue(AttributeType.Velocity), 0.001f);
            Assert.AreEqual(0f, PropertyRules.DefaultValue(AttributeType.Magnetism), 0.001f);
        }

        [Test]
        public void CopyResultMessagesAreOnlyForFailures()
        {
            Assert.IsTrue(CopyResult.Success.ToMessage() == null);
            Assert.IsTrue(CopyResult.NoFreeSlot.ToMessage() != null);
            Assert.IsTrue(CopyResult.AttributeLocked.ToMessage() != null);
        }
    }
}

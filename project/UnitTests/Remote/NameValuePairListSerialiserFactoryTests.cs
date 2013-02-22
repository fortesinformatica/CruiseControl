﻿namespace ThoughtWorks.CruiseControl.UnitTests.Remote
{
    using NUnit.Framework;
    using ThoughtWorks.CruiseControl.Remote;

    [TestFixture]
    public class NameValuePairListSerialiserFactoryTests
    {
        #region Tests
        [Test]
        public void CreateGeneratesNewSerialiser()
        {
            var factory = new NameValuePairListSerialiserFactory();
            var serialiser = factory.Create(null, null);
            Assert.IsInstanceOf<NameValuePairSerialiser>(serialiser);
            var actualSerialiser = serialiser as NameValuePairSerialiser;
            Assert.IsTrue(actualSerialiser.IsList);
        }
        #endregion
    }
}

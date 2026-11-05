using FluentAssertions;
using NINA.StarMessenger.Processing;
using NUnit.Framework;

namespace NINA.StarMessenger.Tests.Processing
{
    [TestFixture]
    public class BortleScaleTests
    {
        [TestCase(22.5, 1)]
        [TestCase(21.99, 1)]
        [TestCase(21.98, 2)]
        [TestCase(21.89, 2)]
        [TestCase(21.88, 3)]
        [TestCase(21.69, 3)]
        [TestCase(21.68, 4)]
        [TestCase(20.49, 4)]
        [TestCase(20.48, 5)]
        [TestCase(19.50, 5)]
        [TestCase(19.49, 6)]
        [TestCase(18.94, 6)]
        [TestCase(18.93, 7)]
        [TestCase(18.38, 7)]
        [TestCase(18.37, 8)]
        [TestCase(17.80, 8)]
        [TestCase(17.79, 9)]
        [TestCase(15.0, 9)]
        public void FromSkyQuality_ReturnsBortleClass(double skyQuality, int expectedBortleClass)
        {
            BortleScale.FromSkyQuality(skyQuality).Should().Be(expectedBortleClass);
        }
    }
}

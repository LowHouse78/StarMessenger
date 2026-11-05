using FluentAssertions;
using Moq;
using NINA.Core.Model;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.StarMessenger.Processing;
using NINA.WPF.Base.Interfaces.Mediator;
using NUnit.Framework;

namespace NINA.StarMessenger.Tests.Processing
{
    [TestFixture]
    public class ImageSnapshotFactoryTests
    {
        private static ImageSavedEventArgs CreateEventArgs()
        {
            var statistics = new Mock<IImageStatistics>();
            statistics.SetupGet(s => s.Mean).Returns(1234.5678);
            statistics.SetupGet(s => s.Median).Returns(1200.111);
            statistics.SetupGet(s => s.StDev).Returns(55.5555);
            statistics.SetupGet(s => s.MedianAbsoluteDeviation).Returns(12.5);
            statistics.SetupGet(s => s.Min).Returns(100);
            statistics.SetupGet(s => s.Max).Returns(65535);

            var rms = new RMS { RA = 0.5, Dec = 0.25, Total = 0.6 };
            rms.SetScale(2);

            var metaData = new ImageMetaData();
            metaData.Image.Id = 42;
            metaData.Image.ImageType = "LIGHT";
            metaData.Image.ExposureStart = new DateTime(2026, 11, 4, 1, 0, 0, DateTimeKind.Local);
            metaData.Image.ExposureTime = 300;
            metaData.Image.RecordedRMS = rms;
            metaData.Focuser.Position = 12345;
            metaData.Focuser.Temperature = 4.567;
            metaData.WeatherData.Temperature = -1;
            metaData.Rotator.Position = 90.756;
            metaData.Target.Name = "M31";

            return new ImageSavedEventArgs
            {
                MetaData = metaData,
                Statistics = statistics.Object,
                StarDetectionAnalysis = new StarDetectionAnalysis { HFR = 2.3456, DetectedStars = 321 },
                PathToImage = new Uri(@"C:\Images\M31_0042.fits"),
                Filter = "Ha",
                Duration = 300
            };
        }

        [Test]
        public void Create_MapsAllValuesOfTheSavedImage()
        {
            var snapshot = ImageSnapshotFactory.Create(CreateEventArgs());

            snapshot.ImageId.Should().Be(42);
            snapshot.ImageType.Should().Be("LIGHT");
            snapshot.LocalPath.Should().Be(@"C:\Images\M31_0042.fits");

            snapshot.GetValue("Time").Should().Be(new DateTime(2026, 11, 4, 1, 5, 0));
            snapshot.GetValue("HFR").Should().Be(2.35);
            snapshot.GetValue("Stars").Should().Be(321);
            snapshot.GetValue("Mean").Should().Be(1234.57);
            snapshot.GetValue("Median").Should().Be(1200.11);
            snapshot.GetValue("StDev").Should().Be(55.56);
            snapshot.GetValue("MAD").Should().Be(12.5);
            snapshot.GetValue("Min").Should().Be(100d);
            snapshot.GetValue("Max").Should().Be(65535d);
            snapshot.GetValue("RMSTotal").Should().Be(1.2);
            snapshot.GetValue("RMSRa").Should().Be(1.0);
            snapshot.GetValue("RMSDec").Should().Be(0.5);
            snapshot.GetValue("FocuserPosition").Should().Be(12345);
            snapshot.GetValue("RotatorPosition").Should().Be(90.76);
            snapshot.GetValue("Filter").Should().Be("Ha");
            snapshot.GetValue("Duration").Should().Be(300d);
            snapshot.GetValue("Target").Should().BeOfType<string>().Which.Should().StartWith("M31");
        }

        [Test]
        public void Create_TemperatureFromFocuser()
        {
            ImageSnapshotFactory.Create(CreateEventArgs()).GetValue("Temperature").Should().Be(4.57);
        }

        [Test]
        public void Create_NoFocuserTemperature_TemperatureFromWeatherStation()
        {
            var e = CreateEventArgs();
            e.MetaData.Focuser.Temperature = double.NaN;

            ImageSnapshotFactory.Create(e).GetValue("Temperature").Should().Be(-1d);
        }

        [Test]
        public void Create_ValuesNotAvailable_AreLeftOut()
        {
            var e = CreateEventArgs();
            e.MetaData.Focuser.Temperature = double.NaN;
            e.MetaData.Focuser.Position = null;
            e.MetaData.WeatherData.Temperature = double.NaN;
            e.MetaData.Image.RecordedRMS = null;
            e.StarDetectionAnalysis = null;
            e.Filter = string.Empty;

            var snapshot = ImageSnapshotFactory.Create(e);

            snapshot.GetValue("Temperature").Should().BeNull();
            snapshot.GetValue("FocuserPosition").Should().BeNull();
            snapshot.GetValue("RotatorMechanicalPosition").Should().BeNull("the mechanical position is NaN");
            snapshot.GetValue("RMSTotal").Should().BeNull();
            snapshot.GetValue("RMSRa").Should().BeNull();
            snapshot.GetValue("RMSDec").Should().BeNull();
            snapshot.GetValue("HFR").Should().BeNull();
            snapshot.GetValue("Stars").Should().BeNull();
            snapshot.GetValue("Filter").Should().BeNull();
            snapshot.GetValue("SkyQuality").Should().BeNull("no weather device with sky quality connected");
            snapshot.GetValue("Bortle").Should().BeNull();
        }

        [Test]
        public void Create_SkyQualityOfWeatherDevice_AddsSkyQualityAndBortle()
        {
            var e = CreateEventArgs();
            e.MetaData.WeatherData.SkyQuality = 20.856;

            var snapshot = ImageSnapshotFactory.Create(e);

            snapshot.GetValue("SkyQuality").Should().Be(20.86);
            snapshot.GetValue("Bortle").Should().Be(4);
        }
    }
}

using FluentAssertions;
using NINA.StarMessenger.Communication.Pushover;
using NUnit.Framework;

namespace NINA.StarMessenger.Tests.Communication
{
    [TestFixture]
    public class PushoverResponseTests
    {
        [Test]
        public void Parse_SuccessResponseOfPushoverApi_IsAccepted()
        {
            // Response of https://api.pushover.net/1/messages.json for an accepted message (lower case property names)
            const string responseContent = "{\"status\":1,\"request\":\"647d2300-702c-4b38-8b2f-d56326ae460b\"}";

            var response = PushoverResponse.Parse(responseContent);

            response.Should().NotBeNull();
            response!.Status.Should().Be(1);
            response.Request.Should().Be("647d2300-702c-4b38-8b2f-d56326ae460b");
            response.IsAccepted.Should().BeTrue();
        }

        [Test]
        public void Parse_ErrorResponseOfPushoverApi_IsNotAccepted()
        {
            const string responseContent = "{\"user\":\"invalid\",\"errors\":[\"user identifier is invalid\"],\"status\":0,\"request\":\"5042853c-402d-4a18-abcb-168734a801de\"}";

            var response = PushoverResponse.Parse(responseContent);

            response!.IsAccepted.Should().BeFalse();
            response.Errors.Should().Equal("user identifier is invalid");
        }

        [Test]
        public void Parse_NoJson_ReturnsNull()
        {
            PushoverResponse.Parse("<html>Bad Gateway</html>").Should().BeNull();
        }
    }
}

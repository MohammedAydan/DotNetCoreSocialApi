using FluentAssertions;
using Xunit;

namespace Social.Tests
{
    public class SanityTests
    {
        [Fact]
        public void TestRunner_ShouldWorkCorrectly()
        {
            const bool isRunning = true;
            isRunning.Should().BeTrue();
        }
    }
}

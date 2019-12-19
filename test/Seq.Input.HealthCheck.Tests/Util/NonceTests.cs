using System.Collections.Generic;
using System.Linq;
using Seq.Input.HealthCheck.Util;
using Xunit;

namespace Seq.Input.HealthCheck.Tests.Util
{
    public class NonceTests
    {
        public static IEnumerable<object[]> CharacterCounts = Enumerable.Range(0, 100).Select(n => new [] { (object)n });

        [Theory]
        [MemberData(nameof(CharacterCounts))]
        public void NonceGeneratesCorrectCharacterCount(int count)
        {
            var nonce = Nonce.Generate(count);
            Assert.Equal(count, nonce.Length);
        }

        [Fact]
        public void NonceIsSufficientlyRandom()
        {
            var a = Nonce.Generate(8);

            // Not entirely scientific ;-)
            for (var i = 0; i < 100; ++i)
            {
                var b = Nonce.Generate(8);
                Assert.NotEqual(a, b);
            }
        }
    }
}

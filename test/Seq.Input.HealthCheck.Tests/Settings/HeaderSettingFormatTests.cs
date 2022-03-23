using System;
using Seq.Input.HealthCheck.Util;
using Xunit;

namespace Seq.Input.HealthCheck.Tests.Settings
{

    public class HeaderSettingFormatTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("Name")]
        [InlineData("Name Value")]
        [InlineData(":Value")]
        public void InvalidHeaderSettingsAreNotParsed(string setting)
        {
            Assert.Throws<ArgumentException>(() => HeaderSettingFormat.Parse(setting));
        }

        [Theory]
        [InlineData("Name: Value", "Name", "Value")]
        [InlineData("Name:Value", "Name", "Value")]
        [InlineData("Name: Value ", "Name", "Value")]
        [InlineData("Name:", "Name", "")]
        [InlineData("Dashed-Name: Value", "Dashed-Name", "Value")]
        [InlineData("Name: Value: Value", "Name", "Value: Value")]
        public void ValidHeaderSettingsAreParsed(string setting, string expectedName, string expectedValue)
        {
            var (name, value) = HeaderSettingFormat.Parse(setting);
            Assert.Equal(expectedName, name);
            Assert.Equal(expectedValue, value);
        }
    }
}
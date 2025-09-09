using SmartAlarm.Domain.Abstractions;
using System;
using SmartAlarm.Domain.Entities;
using Xunit;

namespace SmartAlarm.Domain.Tests.Entities
{
    public class HolidayPreferenceActionTests
    {
        #region Enum Value Tests

        [Fact]
        public void HolidayPreferenceAction_Should_Have_Expected_Values()
        {
            // Assert
            Assert.Equal(1, (int)HolidayPreferenceAction.Disable);
            Assert.Equal(2, (int)HolidayPreferenceAction.Delay);
            Assert.Equal(3, (int)HolidayPreferenceAction.Skip);
        }

        [Fact]
        public void HolidayPreferenceAction_Should_Have_All_Expected_Members()
        {
            // Arrange
            var expectedValues = new[] { "Disable", "Delay", "Skip" };
            var enumNames = Enum.GetNames(typeof(HolidayPreferenceAction));

            // Assert
            Assert.Equal(expectedValues.Length, enumNames.Length);
            foreach (var expectedValue in expectedValues)
            {
                Assert.Contains(expectedValue, enumNames);
            }
        }

        [Theory]
        [InlineData(HolidayPreferenceAction.Disable)]
        [InlineData(HolidayPreferenceAction.Delay)]
        [InlineData(HolidayPreferenceAction.Skip)]
        public void HolidayPreferenceAction_Should_Be_Defined_For_All_Values(HolidayPreferenceAction action)
        {
            // Act & Assert
            Assert.True(Enum.IsDefined(typeof(HolidayPreferenceAction), action));
        }

        [Fact]
        public void HolidayPreferenceAction_Should_Not_Have_Zero_Value()
        {
            // Assert - Enum should start from 1, not 0
            Assert.False(Enum.IsDefined(typeof(HolidayPreferenceAction), 0));
        }

        #endregion

        #region String Conversion Tests

        [Theory]
        [InlineData(HolidayPreferenceAction.Disable, "Disable")]
        [InlineData(HolidayPreferenceAction.Delay, "Delay")]
        [InlineData(HolidayPreferenceAction.Skip, "Skip")]
        public void HolidayPreferenceAction_ToString_Should_Return_Expected_String(
            HolidayPreferenceAction action, string expectedString)
        {
            // Act
            var result = action.ToString();

            // Assert
            Assert.Equal(expectedString, result);
        }

        [Theory]
        [InlineData("Disable", HolidayPreferenceAction.Disable)]
        [InlineData("Delay", HolidayPreferenceAction.Delay)]
        [InlineData("Skip", HolidayPreferenceAction.Skip)]
        public void HolidayPreferenceAction_Parse_Should_Return_Expected_Enum(
            string actionString, HolidayPreferenceAction expectedAction)
        {
            // Act
            var result = Enum.Parse<HolidayPreferenceAction>(actionString);

            // Assert
            Assert.Equal(expectedAction, result);
        }

        [Theory]
        [InlineData("disable")]
        [InlineData("DISABLE")]
        [InlineData("Disable")]
        public void HolidayPreferenceAction_Parse_Should_Be_Case_Insensitive_When_Specified(string actionString)
        {
            // Act
            var result = Enum.Parse<HolidayPreferenceAction>(actionString, true);

            // Assert
            Assert.Equal(HolidayPreferenceAction.Disable, result);
        }

        [Fact]
        public void HolidayPreferenceAction_Parse_Should_Throw_For_Invalid_String()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                Enum.Parse<HolidayPreferenceAction>("InvalidAction"));
        }

        #endregion

        #region TryParse Tests

        [Theory]
        [InlineData("Disable", true, HolidayPreferenceAction.Disable)]
        [InlineData("Delay", true, HolidayPreferenceAction.Delay)]
        [InlineData("Skip", true, HolidayPreferenceAction.Skip)]
        [InlineData("InvalidAction", false, default(HolidayPreferenceAction))]
        [InlineData("", false, default(HolidayPreferenceAction))]
        [InlineData(null, false, default(HolidayPreferenceAction))]
        public void HolidayPreferenceAction_TryParse_Should_Return_Expected_Result(
            string? actionString, bool expectedSuccess, HolidayPreferenceAction expectedAction)
        {
            // Act
            var success = Enum.TryParse<HolidayPreferenceAction>(actionString, out var result);

            // Assert
            Assert.Equal(expectedSuccess, success);
            Assert.Equal(expectedAction, result);
        }

        #endregion

        #region Integration with UserHolidayPreference Tests

        [Theory]
        [InlineData(HolidayPreferenceAction.Disable)]
        [InlineData(HolidayPreferenceAction.Delay)]
        [InlineData(HolidayPreferenceAction.Skip)]
        public void HolidayPreferenceAction_Should_Work_With_UserHolidayPreference(HolidayPreferenceAction action)
        {
            // Arrange
            var userId = Guid.NewGuid();
            var holidayId = Guid.NewGuid();
            var delayInMinutes = action == HolidayPreferenceAction.Delay ? 60 : (int?)null;

            // Act & Assert - Should not throw
            var preference = new UserHolidayPreference(Guid.NewGuid(), userId, holidayId, 
                true, action, delayInMinutes);
            
            Assert.Equal(action, preference.Action);
        }

        #endregion

        #region Enum Utility Tests

        [Fact]
        public void HolidayPreferenceAction_GetValues_Should_Return_All_Values()
        {
            // Act
            var values = Enum.GetValues<HolidayPreferenceAction>();

            // Assert
            Assert.Contains(HolidayPreferenceAction.Disable, values);
            Assert.Contains(HolidayPreferenceAction.Delay, values);
            Assert.Contains(HolidayPreferenceAction.Skip, values);
            Assert.Equal(3, values.Length);
        }

        [Fact]
        public void HolidayPreferenceAction_GetNames_Should_Return_All_Names()
        {
            // Act
            var names = Enum.GetNames<HolidayPreferenceAction>();

            // Assert
            Assert.Contains("Disable", names);
            Assert.Contains("Delay", names);
            Assert.Contains("Skip", names);
            Assert.Equal(3, names.Length);
        }

        #endregion
    }
}

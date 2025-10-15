using SmartAlarm.Domain.Abstractions;
using System;
using SmartAlarm.Domain.Entities;
using Xunit;

namespace SmartAlarm.Domain.Tests.Entities
{
    public class UserHolidayPreferenceTests
    {
        private readonly Guid _validUserId = Guid.NewGuid();
        private readonly Guid _validHolidayId = Guid.NewGuid();
        private readonly Holiday _testHoliday;

        public UserHolidayPreferenceTests()
        {
            _testHoliday = new Holiday(_validHolidayId, DateTime.Today, "Natal");
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_Should_Create_UserHolidayPreference_With_Valid_Parameters()
        {
            // Arrange
            var id = Guid.NewGuid();
            var isEnabled = true;
            var action = HolidayPreferenceAction.Disable;

            // Act
            var preference = new UserHolidayPreference(id, _validUserId, _validHolidayId, isEnabled, action);

            // Assert
            Assert.Equal(id, preference.Id);
            Assert.Equal(_validUserId, preference.UserId);
            Assert.Equal(_validHolidayId, preference.HolidayId);
            Assert.Equal(isEnabled, preference.IsEnabled);
            Assert.Equal(action, preference.Action);
            Assert.Null(preference.DelayInMinutes);
            Assert.True(preference.CreatedAt <= DateTime.UtcNow);
            Assert.Null(preference.UpdatedAt);
        }

        [Fact]
        public void Constructor_Should_Generate_New_Id_When_Empty_Guid_Provided()
        {
            // Act
            var preference = new UserHolidayPreference(Guid.Empty, _validUserId, _validHolidayId, 
                true, HolidayPreferenceAction.Disable);

            // Assert
            Assert.NotEqual(Guid.Empty, preference.Id);
        }

        [Fact]
        public void Constructor_Should_Create_Preference_With_Delay_Action()
        {
            // Arrange
            var delayInMinutes = 60;

            // Act
            var preference = new UserHolidayPreference(Guid.NewGuid(), _validUserId, _validHolidayId, 
                true, HolidayPreferenceAction.Delay, delayInMinutes);

            // Assert
            Assert.Equal(HolidayPreferenceAction.Delay, preference.Action);
            Assert.Equal(delayInMinutes, preference.DelayInMinutes);
        }

        [Theory]
        [InlineData(HolidayPreferenceAction.Disable)]
        [InlineData(HolidayPreferenceAction.Skip)]
        public void Constructor_Should_Allow_Null_DelayInMinutes_For_Non_Delay_Actions(HolidayPreferenceAction action)
        {
            // Act & Assert - Should not throw
            var preference = new UserHolidayPreference(Guid.NewGuid(), _validUserId, _validHolidayId, 
                true, action);
            
            Assert.Equal(action, preference.Action);
            Assert.Null(preference.DelayInMinutes);
        }

        #endregion

        #region Constructor Validation Tests

        [Fact]
        public void Constructor_Should_Throw_When_UserId_Is_Empty()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                new UserHolidayPreference(Guid.NewGuid(), Guid.Empty, _validHolidayId, 
                    true, HolidayPreferenceAction.Disable));
        }

        [Fact]
        public void Constructor_Should_Throw_When_HolidayId_Is_Empty()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                new UserHolidayPreference(Guid.NewGuid(), _validUserId, Guid.Empty, 
                    true, HolidayPreferenceAction.Disable));
        }

        [Fact]
        public void Constructor_Should_Throw_When_Action_Is_Invalid()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                new UserHolidayPreference(Guid.NewGuid(), _validUserId, _validHolidayId, 
                    true, (HolidayPreferenceAction)999));
        }

        [Fact]
        public void Constructor_Should_Throw_When_Delay_Action_Without_DelayInMinutes()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                new UserHolidayPreference(Guid.NewGuid(), _validUserId, _validHolidayId, 
                    true, HolidayPreferenceAction.Delay, null));
        }

        [Fact]
        public void Constructor_Should_Throw_When_Delay_Action_With_Zero_DelayInMinutes()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                new UserHolidayPreference(Guid.NewGuid(), _validUserId, _validHolidayId, 
                    true, HolidayPreferenceAction.Delay, 0));
        }

        [Fact]
        public void Constructor_Should_Throw_When_Delay_Action_With_Negative_DelayInMinutes()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                new UserHolidayPreference(Guid.NewGuid(), _validUserId, _validHolidayId, 
                    true, HolidayPreferenceAction.Delay, -30));
        }

        [Fact]
        public void Constructor_Should_Throw_When_Delay_Action_With_Excessive_DelayInMinutes()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                new UserHolidayPreference(Guid.NewGuid(), _validUserId, _validHolidayId, 
                    true, HolidayPreferenceAction.Delay, 1441)); // > 24 horas
        }

        [Theory]
        [InlineData(HolidayPreferenceAction.Disable, 30)]
        [InlineData(HolidayPreferenceAction.Skip, 60)]
        public void Constructor_Should_Throw_When_Non_Delay_Action_With_DelayInMinutes(
            HolidayPreferenceAction action, int delayInMinutes)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                new UserHolidayPreference(Guid.NewGuid(), _validUserId, _validHolidayId, 
                    true, action, delayInMinutes));
        }

        #endregion

        #region Enable/Disable Tests

        [Fact]
        public void Enable_Should_Set_IsEnabled_To_True_And_Update_Timestamp()
        {
            // Arrange
            var preference = new UserHolidayPreference(Guid.NewGuid(), _validUserId, _validHolidayId, 
                false, HolidayPreferenceAction.Disable);
            var originalUpdatedAt = preference.UpdatedAt;

            // Act
            preference.Enable();

            // Assert
            Assert.True(preference.IsEnabled);
            Assert.NotEqual(originalUpdatedAt, preference.UpdatedAt);
            Assert.True(preference.UpdatedAt <= DateTime.UtcNow);
        }

        [Fact]
        public void Disable_Should_Set_IsEnabled_To_False_And_Update_Timestamp()
        {
            // Arrange
            var preference = new UserHolidayPreference(Guid.NewGuid(), _validUserId, _validHolidayId, 
                true, HolidayPreferenceAction.Disable);
            var originalUpdatedAt = preference.UpdatedAt;

            // Act
            preference.Disable();

            // Assert
            Assert.False(preference.IsEnabled);
            Assert.NotEqual(originalUpdatedAt, preference.UpdatedAt);
            Assert.True(preference.UpdatedAt <= DateTime.UtcNow);
        }

        #endregion

        #region UpdateAction Tests

        [Fact]
        public void UpdateAction_Should_Update_Action_And_Timestamp()
        {
            // Arrange
            var preference = new UserHolidayPreference(Guid.NewGuid(), _validUserId, _validHolidayId, 
                true, HolidayPreferenceAction.Disable);
            var originalUpdatedAt = preference.UpdatedAt;

            // Act
            preference.UpdateAction(HolidayPreferenceAction.Skip);

            // Assert
            Assert.Equal(HolidayPreferenceAction.Skip, preference.Action);
            Assert.Null(preference.DelayInMinutes);
            Assert.NotEqual(originalUpdatedAt, preference.UpdatedAt);
            Assert.True(preference.UpdatedAt <= DateTime.UtcNow);
        }

        [Fact]
        public void UpdateAction_Should_Update_Action_With_Delay()
        {
            // Arrange
            var preference = new UserHolidayPreference(Guid.NewGuid(), _validUserId, _validHolidayId, 
                true, HolidayPreferenceAction.Disable);
            var delayInMinutes = 120;

            // Act
            preference.UpdateAction(HolidayPreferenceAction.Delay, delayInMinutes);

            // Assert
            Assert.Equal(HolidayPreferenceAction.Delay, preference.Action);
            Assert.Equal(delayInMinutes, preference.DelayInMinutes);
        }

        [Fact]
        public void UpdateAction_Should_Throw_When_Delay_Action_Without_DelayInMinutes()
        {
            // Arrange
            var preference = new UserHolidayPreference(Guid.NewGuid(), _validUserId, _validHolidayId, 
                true, HolidayPreferenceAction.Disable);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                preference.UpdateAction(HolidayPreferenceAction.Delay));
        }

        [Theory]
        [InlineData(HolidayPreferenceAction.Disable, 30)]
        [InlineData(HolidayPreferenceAction.Skip, 60)]
        public void UpdateAction_Should_Throw_When_Non_Delay_Action_With_DelayInMinutes(
            HolidayPreferenceAction action, int delayInMinutes)
        {
            // Arrange
            var preference = new UserHolidayPreference(Guid.NewGuid(), _validUserId, _validHolidayId, 
                true, HolidayPreferenceAction.Disable);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                preference.UpdateAction(action, delayInMinutes));
        }

        #endregion

        #region IsApplicableForDate Tests

        [Fact]
        public void IsApplicableForDate_Should_Return_True_When_Enabled_And_Holiday_Matches()
        {
            // Arrange
            var preference = new UserHolidayPreference(Guid.NewGuid(), _validUserId, _validHolidayId, 
                true, HolidayPreferenceAction.Disable);
            var testDate = DateTime.Today;

            // Act
            var result = preference.IsApplicableForDate(testDate, _testHoliday);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsApplicableForDate_Should_Return_False_When_Disabled()
        {
            // Arrange
            var preference = new UserHolidayPreference(Guid.NewGuid(), _validUserId, _validHolidayId, 
                false, HolidayPreferenceAction.Disable);
            var testDate = DateTime.Today;

            // Act
            var result = preference.IsApplicableForDate(testDate, _testHoliday);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsApplicableForDate_Should_Return_False_When_Holiday_Doesnt_Match()
        {
            // Arrange
            var otherHolidayId = Guid.NewGuid();
            var otherHoliday = new Holiday(otherHolidayId, DateTime.Today, "Ano Novo");
            var preference = new UserHolidayPreference(Guid.NewGuid(), _validUserId, _validHolidayId, 
                true, HolidayPreferenceAction.Disable);
            var testDate = DateTime.Today;

            // Act
            var result = preference.IsApplicableForDate(testDate, otherHoliday);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsApplicableForDate_Should_Return_False_When_Date_Doesnt_Match()
        {
            // Arrange
            var preference = new UserHolidayPreference(Guid.NewGuid(), _validUserId, _validHolidayId, 
                true, HolidayPreferenceAction.Disable);
            var testDate = DateTime.Today.AddDays(1); // Different date

            // Act
            var result = preference.IsApplicableForDate(testDate, _testHoliday);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region GetEffectiveDelayInMinutes Tests

        [Fact]
        public void GetEffectiveDelayInMinutes_Should_Return_Zero_For_Disable_Action()
        {
            // Arrange
            var preference = new UserHolidayPreference(Guid.NewGuid(), _validUserId, _validHolidayId, 
                true, HolidayPreferenceAction.Disable);

            // Act
            var result = preference.GetEffectiveDelayInMinutes();

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void GetEffectiveDelayInMinutes_Should_Return_Zero_For_Skip_Action()
        {
            // Arrange
            var preference = new UserHolidayPreference(Guid.NewGuid(), _validUserId, _validHolidayId, 
                true, HolidayPreferenceAction.Skip);

            // Act
            var result = preference.GetEffectiveDelayInMinutes();

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void GetEffectiveDelayInMinutes_Should_Return_Configured_Value_For_Delay_Action()
        {
            // Arrange
            var delayInMinutes = 90;
            var preference = new UserHolidayPreference(Guid.NewGuid(), _validUserId, _validHolidayId, 
                true, HolidayPreferenceAction.Delay, delayInMinutes);

            // Act
            var result = preference.GetEffectiveDelayInMinutes();

            // Assert
            Assert.Equal(delayInMinutes, result);
        }

        [Fact]
        public void GetEffectiveDelayInMinutes_Should_Return_Zero_For_Delay_Action_With_Null_DelayInMinutes()
        {
            // Arrange - Criando diretamente com reflection para simular estado inválido
            var preference = new UserHolidayPreference(Guid.NewGuid(), _validUserId, _validHolidayId, 
                true, HolidayPreferenceAction.Disable);
            
            // Usando reflection para simular estado inconsistente
            var actionField = typeof(UserHolidayPreference).GetField("<Action>k__BackingField", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            actionField?.SetValue(preference, HolidayPreferenceAction.Delay);

            // Act
            var result = preference.GetEffectiveDelayInMinutes();

            // Assert
            Assert.Equal(0, result);
        }

        #endregion

        #region Edge Cases and Boundary Tests

        [Theory]
        [InlineData(1)]
        [InlineData(60)]
        [InlineData(720)] // 12 horas
        [InlineData(1440)] // 24 horas
        public void Constructor_Should_Accept_Valid_DelayInMinutes_Boundaries(int delayInMinutes)
        {
            // Act & Assert - Should not throw
            var preference = new UserHolidayPreference(Guid.NewGuid(), _validUserId, _validHolidayId, 
                true, HolidayPreferenceAction.Delay, delayInMinutes);
            
            Assert.Equal(delayInMinutes, preference.DelayInMinutes);
        }

        [Fact]
        public void UpdateAction_Should_Clear_DelayInMinutes_When_Changing_From_Delay_To_Other_Action()
        {
            // Arrange
            var preference = new UserHolidayPreference(Guid.NewGuid(), _validUserId, _validHolidayId, 
                true, HolidayPreferenceAction.Delay, 60);

            // Act
            preference.UpdateAction(HolidayPreferenceAction.Disable);

            // Assert
            Assert.Equal(HolidayPreferenceAction.Disable, preference.Action);
            Assert.Null(preference.DelayInMinutes);
        }

        [Fact]
        public void Multiple_Updates_Should_Maintain_Consistency()
        {
            // Arrange
            var preference = new UserHolidayPreference(Guid.NewGuid(), _validUserId, _validHolidayId, 
                true, HolidayPreferenceAction.Disable);
            var originalCreatedAt = preference.CreatedAt;

            // Act
            preference.Disable();
            preference.UpdateAction(HolidayPreferenceAction.Delay, 30);
            preference.Enable();
            preference.UpdateAction(HolidayPreferenceAction.Skip);

            // Assert
            Assert.True(preference.IsEnabled);
            Assert.Equal(HolidayPreferenceAction.Skip, preference.Action);
            Assert.Null(preference.DelayInMinutes);
            Assert.Equal(originalCreatedAt, preference.CreatedAt); // CreatedAt should not change
            Assert.NotNull(preference.UpdatedAt);
        }

        #endregion
    }
}

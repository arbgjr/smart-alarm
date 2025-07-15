using System;
using SmartAlarm.Domain.Entities;
using Xunit;

namespace SmartAlarm.Domain.Tests.Entities
{
    public class ExceptionPeriodTests
    {
        private readonly Guid _validUserId = Guid.NewGuid();
        private readonly DateTime _validStartDate = DateTime.Today;
        private readonly DateTime _validEndDate = DateTime.Today.AddDays(7);

        #region Constructor Tests

        [Fact]
        public void Constructor_Should_Create_ExceptionPeriod_With_Valid_Parameters()
        {
            // Arrange
            var id = Guid.NewGuid();
            var name = "Férias de Verão";
            var description = "Período de férias de dezembro";
            var type = ExceptionPeriodType.Vacation;

            // Act
            var period = new ExceptionPeriod(id, name, _validStartDate, _validEndDate, type, _validUserId, description);

            // Assert
            Assert.Equal(id, period.Id);
            Assert.Equal(name, period.Name);
            Assert.Equal(description, period.Description);
            Assert.Equal(_validStartDate, period.StartDate);
            Assert.Equal(_validEndDate, period.EndDate);
            Assert.Equal(type, period.Type);
            Assert.Equal(_validUserId, period.UserId);
            Assert.True(period.IsActive);
            Assert.True(period.CreatedAt <= DateTime.UtcNow);
            Assert.Null(period.UpdatedAt);
        }

        [Fact]
        public void Constructor_Should_Generate_New_Id_When_Empty_Guid_Provided()
        {
            // Act
            var period = new ExceptionPeriod(Guid.Empty, "Test", _validStartDate, _validEndDate, 
                ExceptionPeriodType.Custom, _validUserId);

            // Assert
            Assert.NotEqual(Guid.Empty, period.Id);
        }

        [Fact]
        public void Constructor_Should_Create_ExceptionPeriod_Without_Description()
        {
            // Act
            var period = new ExceptionPeriod(Guid.NewGuid(), "Test", _validStartDate, _validEndDate, 
                ExceptionPeriodType.Custom, _validUserId);

            // Assert
            Assert.Null(period.Description);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_Should_Throw_ArgumentException_When_Name_Is_Invalid(string invalidName)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                new ExceptionPeriod(Guid.NewGuid(), invalidName, _validStartDate, _validEndDate, 
                    ExceptionPeriodType.Custom, _validUserId));
        }

        [Fact]
        public void Constructor_Should_Throw_ArgumentException_When_Name_Is_Too_Long()
        {
            // Arrange
            var longName = new string('a', 101);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                new ExceptionPeriod(Guid.NewGuid(), longName, _validStartDate, _validEndDate, 
                    ExceptionPeriodType.Custom, _validUserId));
        }

        [Fact]
        public void Constructor_Should_Throw_ArgumentException_When_StartDate_Is_After_EndDate()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(7);
            var endDate = DateTime.Today;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                new ExceptionPeriod(Guid.NewGuid(), "Test", startDate, endDate, 
                    ExceptionPeriodType.Custom, _validUserId));
        }

        [Fact]
        public void Constructor_Should_Throw_ArgumentException_When_StartDate_Equals_EndDate()
        {
            // Arrange
            var sameDate = DateTime.Today;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                new ExceptionPeriod(Guid.NewGuid(), "Test", sameDate, sameDate, 
                    ExceptionPeriodType.Custom, _validUserId));
        }

        [Fact]
        public void Constructor_Should_Throw_ArgumentException_When_UserId_Is_Empty()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                new ExceptionPeriod(Guid.NewGuid(), "Test", _validStartDate, _validEndDate, 
                    ExceptionPeriodType.Custom, Guid.Empty));
        }

        #endregion

        #region Activation Tests

        [Fact]
        public void Activate_Should_Set_IsActive_To_True_And_Update_UpdatedAt()
        {
            // Arrange
            var period = new ExceptionPeriod(Guid.NewGuid(), "Test", _validStartDate, _validEndDate, 
                ExceptionPeriodType.Custom, _validUserId);
            period.Deactivate(); // First deactivate

            // Act
            period.Activate();

            // Assert
            Assert.True(period.IsActive);
            Assert.NotNull(period.UpdatedAt);
            Assert.True(period.UpdatedAt <= DateTime.UtcNow);
        }

        [Fact]
        public void Deactivate_Should_Set_IsActive_To_False_And_Update_UpdatedAt()
        {
            // Arrange
            var period = new ExceptionPeriod(Guid.NewGuid(), "Test", _validStartDate, _validEndDate, 
                ExceptionPeriodType.Custom, _validUserId);

            // Act
            period.Deactivate();

            // Assert
            Assert.False(period.IsActive);
            Assert.NotNull(period.UpdatedAt);
            Assert.True(period.UpdatedAt <= DateTime.UtcNow);
        }

        #endregion

        #region Update Methods Tests

        [Fact]
        public void UpdateName_Should_Update_Name_And_UpdatedAt()
        {
            // Arrange
            var period = new ExceptionPeriod(Guid.NewGuid(), "Original", _validStartDate, _validEndDate, 
                ExceptionPeriodType.Custom, _validUserId);
            var newName = "Updated Name";

            // Act
            period.UpdateName(newName);

            // Assert
            Assert.Equal(newName, period.Name);
            Assert.NotNull(period.UpdatedAt);
        }

        [Fact]
        public void UpdateName_Should_Trim_Whitespace()
        {
            // Arrange
            var period = new ExceptionPeriod(Guid.NewGuid(), "Original", _validStartDate, _validEndDate, 
                ExceptionPeriodType.Custom, _validUserId);
            var newName = "  Updated Name  ";

            // Act
            period.UpdateName(newName);

            // Assert
            Assert.Equal("Updated Name", period.Name);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void UpdateName_Should_Throw_ArgumentException_When_Name_Is_Invalid(string invalidName)
        {
            // Arrange
            var period = new ExceptionPeriod(Guid.NewGuid(), "Original", _validStartDate, _validEndDate, 
                ExceptionPeriodType.Custom, _validUserId);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => period.UpdateName(invalidName));
        }

        [Fact]
        public void UpdateName_Should_Throw_ArgumentException_When_Name_Is_Too_Long()
        {
            // Arrange
            var period = new ExceptionPeriod(Guid.NewGuid(), "Original", _validStartDate, _validEndDate, 
                ExceptionPeriodType.Custom, _validUserId);
            var longName = new string('a', 101);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => period.UpdateName(longName));
        }

        [Fact]
        public void UpdateDescription_Should_Update_Description_And_UpdatedAt()
        {
            // Arrange
            var period = new ExceptionPeriod(Guid.NewGuid(), "Test", _validStartDate, _validEndDate, 
                ExceptionPeriodType.Custom, _validUserId);
            var newDescription = "New description";

            // Act
            period.UpdateDescription(newDescription);

            // Assert
            Assert.Equal(newDescription, period.Description);
            Assert.NotNull(period.UpdatedAt);
        }

        [Fact]
        public void UpdateDescription_Should_Set_Null_When_Empty_String_Provided()
        {
            // Arrange
            var period = new ExceptionPeriod(Guid.NewGuid(), "Test", _validStartDate, _validEndDate, 
                ExceptionPeriodType.Custom, _validUserId, "Original description");

            // Act
            period.UpdateDescription("");

            // Assert
            Assert.Null(period.Description);
        }

        [Fact]
        public void UpdateDescription_Should_Trim_Whitespace()
        {
            // Arrange
            var period = new ExceptionPeriod(Guid.NewGuid(), "Test", _validStartDate, _validEndDate, 
                ExceptionPeriodType.Custom, _validUserId);
            var newDescription = "  New description  ";

            // Act
            period.UpdateDescription(newDescription);

            // Assert
            Assert.Equal("New description", period.Description);
        }

        [Fact]
        public void UpdateDescription_Should_Throw_ArgumentException_When_Description_Is_Too_Long()
        {
            // Arrange
            var period = new ExceptionPeriod(Guid.NewGuid(), "Test", _validStartDate, _validEndDate, 
                ExceptionPeriodType.Custom, _validUserId);
            var longDescription = new string('a', 501);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => period.UpdateDescription(longDescription));
        }

        [Fact]
        public void UpdatePeriod_Should_Update_Dates_And_UpdatedAt()
        {
            // Arrange
            var period = new ExceptionPeriod(Guid.NewGuid(), "Test", _validStartDate, _validEndDate, 
                ExceptionPeriodType.Custom, _validUserId);
            var newStartDate = DateTime.Today.AddDays(10);
            var newEndDate = DateTime.Today.AddDays(20);

            // Act
            period.UpdatePeriod(newStartDate, newEndDate);

            // Assert
            Assert.Equal(newStartDate, period.StartDate);
            Assert.Equal(newEndDate, period.EndDate);
            Assert.NotNull(period.UpdatedAt);
        }

        [Fact]
        public void UpdatePeriod_Should_Throw_ArgumentException_When_StartDate_Is_After_EndDate()
        {
            // Arrange
            var period = new ExceptionPeriod(Guid.NewGuid(), "Test", _validStartDate, _validEndDate, 
                ExceptionPeriodType.Custom, _validUserId);
            var newStartDate = DateTime.Today.AddDays(20);
            var newEndDate = DateTime.Today.AddDays(10);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => period.UpdatePeriod(newStartDate, newEndDate));
        }

        [Fact]
        public void UpdateType_Should_Update_Type_And_UpdatedAt()
        {
            // Arrange
            var period = new ExceptionPeriod(Guid.NewGuid(), "Test", _validStartDate, _validEndDate, 
                ExceptionPeriodType.Custom, _validUserId);
            var newType = ExceptionPeriodType.Vacation;

            // Act
            period.UpdateType(newType);

            // Assert
            Assert.Equal(newType, period.Type);
            Assert.NotNull(period.UpdatedAt);
        }

        #endregion

        #region Business Logic Tests

        [Theory]
        [InlineData(0)] // Today (start date)
        [InlineData(3)] // Middle of period
        [InlineData(7)] // End date
        public void IsActiveOnDate_Should_Return_True_When_Date_Is_Within_Active_Period(int daysFromStart)
        {
            // Arrange
            var period = new ExceptionPeriod(Guid.NewGuid(), "Test", _validStartDate, _validEndDate, 
                ExceptionPeriodType.Custom, _validUserId);
            var testDate = _validStartDate.AddDays(daysFromStart);

            // Act
            var result = period.IsActiveOnDate(testDate);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(-1)] // Before start date
        [InlineData(8)]  // After end date
        public void IsActiveOnDate_Should_Return_False_When_Date_Is_Outside_Period(int daysFromStart)
        {
            // Arrange
            var period = new ExceptionPeriod(Guid.NewGuid(), "Test", _validStartDate, _validEndDate, 
                ExceptionPeriodType.Custom, _validUserId);
            var testDate = _validStartDate.AddDays(daysFromStart);

            // Act
            var result = period.IsActiveOnDate(testDate);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsActiveOnDate_Should_Return_False_When_Period_Is_Deactivated()
        {
            // Arrange
            var period = new ExceptionPeriod(Guid.NewGuid(), "Test", _validStartDate, _validEndDate, 
                ExceptionPeriodType.Custom, _validUserId);
            period.Deactivate();

            // Act
            var result = period.IsActiveOnDate(_validStartDate);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void OverlapsWith_Should_Return_True_When_Periods_Overlap()
        {
            // Arrange
            var period1 = new ExceptionPeriod(Guid.NewGuid(), "Period 1", 
                DateTime.Today, DateTime.Today.AddDays(10), 
                ExceptionPeriodType.Custom, _validUserId);

            var period2 = new ExceptionPeriod(Guid.NewGuid(), "Period 2", 
                DateTime.Today.AddDays(5), DateTime.Today.AddDays(15), 
                ExceptionPeriodType.Custom, _validUserId);

            // Act
            var result = period1.OverlapsWith(period2);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void OverlapsWith_Should_Return_False_When_Periods_Do_Not_Overlap()
        {
            // Arrange
            var period1 = new ExceptionPeriod(Guid.NewGuid(), "Period 1", 
                DateTime.Today, DateTime.Today.AddDays(5), 
                ExceptionPeriodType.Custom, _validUserId);

            var period2 = new ExceptionPeriod(Guid.NewGuid(), "Period 2", 
                DateTime.Today.AddDays(10), DateTime.Today.AddDays(15), 
                ExceptionPeriodType.Custom, _validUserId);

            // Act
            var result = period1.OverlapsWith(period2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void OverlapsWith_Should_Return_False_When_Other_Period_Is_Null()
        {
            // Arrange
            var period = new ExceptionPeriod(Guid.NewGuid(), "Test", _validStartDate, _validEndDate, 
                ExceptionPeriodType.Custom, _validUserId);

            // Act
            var result = period.OverlapsWith(null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetDurationInDays_Should_Return_Correct_Duration()
        {
            // Arrange
            var startDate = DateTime.Today;
            var endDate = DateTime.Today.AddDays(6); // 7 days total (inclusive)
            var period = new ExceptionPeriod(Guid.NewGuid(), "Test", startDate, endDate, 
                ExceptionPeriodType.Custom, _validUserId);

            // Act
            var duration = period.GetDurationInDays();

            // Assert
            Assert.Equal(7, duration);
        }

        [Fact]
        public void GetDurationInDays_Should_Return_1_For_Single_Day_Period()
        {
            // Arrange
            var sameDay = DateTime.Today;
            var nextDay = DateTime.Today.AddDays(1);
            var period = new ExceptionPeriod(Guid.NewGuid(), "Test", sameDay, nextDay, 
                ExceptionPeriodType.Custom, _validUserId);

            // Act
            var duration = period.GetDurationInDays();

            // Assert
            Assert.Equal(2, duration); // Start day + end day
        }

        #endregion

        #region Integration with Other Types Tests

        [Theory]
        [InlineData(ExceptionPeriodType.Vacation)]
        [InlineData(ExceptionPeriodType.Holiday)]
        [InlineData(ExceptionPeriodType.Travel)]
        [InlineData(ExceptionPeriodType.Maintenance)]
        [InlineData(ExceptionPeriodType.MedicalLeave)]
        [InlineData(ExceptionPeriodType.RemoteWork)]
        [InlineData(ExceptionPeriodType.Custom)]
        public void Constructor_Should_Accept_All_ExceptionPeriodTypes(ExceptionPeriodType type)
        {
            // Act
            var period = new ExceptionPeriod(Guid.NewGuid(), "Test", _validStartDate, _validEndDate, 
                type, _validUserId);

            // Assert
            Assert.Equal(type, period.Type);
        }

        #endregion
    }
}

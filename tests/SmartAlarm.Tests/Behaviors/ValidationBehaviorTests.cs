using SmartAlarm.Domain.Abstractions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using SmartAlarm.Application.Behaviors;
using Xunit;

namespace SmartAlarm.Tests.Behaviors
{
    /// <summary>
    /// Testes para o comportamento de validação do MediatR.
    /// </summary>
    public class ValidationBehaviorTests
    {
        public class TestRequest : IRequest<string>
        {
            public string Value { get; set; } = string.Empty;
        }

        public class TestRequestValidator : AbstractValidator<TestRequest>
        {
            public TestRequestValidator()
            {
                RuleFor(x => x.Value)
                    .NotEmpty()
                    .WithMessage("Value is required");
            }
        }

        [Fact]
        public async Task Handle_ShouldCallNext_WhenNoValidatorsExist()
        {
            // Arrange
            var validators = new List<IValidator<TestRequest>>();
            var behavior = new ValidationBehavior<TestRequest, string>(validators);
            var request = new TestRequest { Value = "test" };
            var nextCalled = false;

            RequestHandlerDelegate<string> next = () =>
            {
                nextCalled = true;
                return Task.FromResult("success");
            };

            // Act
            var result = await behavior.Handle(request, next, CancellationToken.None);

            // Assert
            Assert.True(nextCalled);
            Assert.Equal("success", result);
        }

        [Fact]
        public async Task Handle_ShouldCallNext_WhenValidationPasses()
        {
            // Arrange
            var validators = new List<IValidator<TestRequest>> { new TestRequestValidator() };
            var behavior = new ValidationBehavior<TestRequest, string>(validators);
            var request = new TestRequest { Value = "valid value" };
            var nextCalled = false;

            RequestHandlerDelegate<string> next = () =>
            {
                nextCalled = true;
                return Task.FromResult("success");
            };

            // Act
            var result = await behavior.Handle(request, next, CancellationToken.None);

            // Assert
            Assert.True(nextCalled);
            Assert.Equal("success", result);
        }

        [Fact]
        public async Task Handle_ShouldThrowValidationException_WhenValidationFails()
        {
            // Arrange
            var validators = new List<IValidator<TestRequest>> { new TestRequestValidator() };
            var behavior = new ValidationBehavior<TestRequest, string>(validators);
            var request = new TestRequest { Value = "" }; // Invalid value
            var nextCalled = false;

            RequestHandlerDelegate<string> next = () =>
            {
                nextCalled = true;
                return Task.FromResult("success");
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => behavior.Handle(request, next, CancellationToken.None));

            Assert.False(nextCalled);
            Assert.Contains("Value is required", exception.Errors.Select(e => e.ErrorMessage));
        }

        [Fact]
        public async Task Handle_ShouldCollectAllValidationErrors_WhenMultipleValidatorsFail()
        {
            // Arrange
            var validator1 = new Mock<IValidator<TestRequest>>();
            var validator2 = new Mock<IValidator<TestRequest>>();

            validator1.Setup(v => v.ValidateAsync(It.IsAny<IValidationContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(new[] 
                { 
                    new ValidationFailure("Value", "Error from validator 1") 
                }));

            validator2.Setup(v => v.ValidateAsync(It.IsAny<IValidationContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(new[] 
                { 
                    new ValidationFailure("Value", "Error from validator 2") 
                }));

            var validators = new List<IValidator<TestRequest>> { validator1.Object, validator2.Object };
            var behavior = new ValidationBehavior<TestRequest, string>(validators);
            var request = new TestRequest { Value = "test" };

            RequestHandlerDelegate<string> next = () => Task.FromResult("success");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(
                () => behavior.Handle(request, next, CancellationToken.None));

            Assert.Equal(2, exception.Errors.Count());
            Assert.Contains("Error from validator 1", exception.Errors.Select(e => e.ErrorMessage));
            Assert.Contains("Error from validator 2", exception.Errors.Select(e => e.ErrorMessage));
        }
    }
}
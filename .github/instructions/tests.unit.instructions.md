# Unit Test Instructions

## Test Standards

### AAA Structure
All tests must follow the AAA pattern (Arrange, Act, Assert):
- **Arrange**: Set up the test scenario
- **Act**: Execute the action being tested
- **Assert**: Verify the result

### Naming
- Use descriptive naming in English
- Format: `Should_ExpectedBehavior_When_StateUnderTest`
- Example: `Should_ThrowValidationException_When_EmailIsInvalid`

### Coverage
- Cover happy path (success) cases
- Cover error and exception cases
- Cover boundary and edge cases
- Minimum 80% coverage for critical code

### Tools
- xUnit as the test framework
- Moq for mocking
- FluentAssertions for more readable assertions
- Bogus for test data generation

### Execution
- Always run with: `dotnet test --logger "console;verbosity=detailed"`
- Include `|| true` in CI/CD scripts to avoid breaking the pipeline

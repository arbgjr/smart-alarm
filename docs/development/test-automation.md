# Automated Testing – Smart Alarm

## Testing Standards

- All critical cases covered with xUnit and Moq
- AAA (Arrange, Act, Assert) applied in all tests
- Tests are close to the implemented code (`tests/` folder)
- Minimum 80% coverage for critical code
- Tests for success, error, and edge cases

## Examples

- Tests for Handlers, Validators, Middlewares, and Repositories
- Integration tests for main endpoints
- Rollback and transaction tests (with note on SQLite in-memory limitations)

## Running the Tests

```powershell
# Run all tests
./tests/run-tests.ps1
```

## Coverage Status

- Minimum coverage achieved for Application and API
- Integration coverage in progress

---

**Status:** Automated tests documented and standardized.

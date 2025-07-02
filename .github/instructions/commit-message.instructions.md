---
applyTo: "**"
---
# ✍️ Commit Messages

## Format
```
<type>[scope]: <description>

[optional body]

[optional footer]
```

## 1. Header (Required)

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `refactor`: Refactoring without functional change
- `style`: Formatting, spaces, etc.
- `test`: Add/correct tests
- `docs`: Documentation changes
- `build`: Build system/dependencies
- `ci`: CI scripts
- `chore`: No source code change
- `perf`: Performance improvement

**Scope:**
- Area of code affected: `users`, `auth`, `frontend`, `api-core`, etc.

**Description:**
- Concise (≤ 50 characters)
- Present imperative ("add" not "added")
- Do not capitalize the first letter
- No period at the end

## 2. Body (Optional)
- Additional context (the "why")
- Separated from header by a blank line
- Maximum 72 characters per line

## 3. Footer (Optional)
- For Breaking Changes: `BREAKING CHANGE: description...`
- For Issues: `Refs: AB#12345`, `Closes: #42`

## Examples

**Simple:**
```
feat(users): add endpoint for CPF search
fix(auth): fix date parsing in JWT token
```

**With body:**
```
feat(auth): implement JWT authentication

Uses JWT Bearer tokens to protect endpoints.
Validation on every request.
```

**With Breaking Change:**
```
refactor(api): change error response format

BREAKING CHANGE: Error response structure changed.
Refs: AB#78910
```
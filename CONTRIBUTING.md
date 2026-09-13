# Contributing to OpenIddict.Management

Thank you for your interest in contributing to **OpenIddict.Management**! Community contributions help make identity and access management in .NET simpler and more robust for everyone.

Please take a moment to review this guide before getting started.

---

## Code of Conduct

We are committed to providing a welcoming, inclusive, and harassment-free environment for everyone. Please be respectful and constructive in issues, discussions, and pull requests.

---

## How Can I Contribute?

### 1. Reporting Bugs

If you discover a bug or unexpected behavior:
1. **Search existing issues** to make sure the issue hasn't already been reported.
2. **Open a new issue** with a descriptive title.
3. Include:
   - A clear description of the problem.
   - Steps to reproduce the issue.
   - The relevant framework version (.NET 8, 9, or 10) and database provider (SQLite, SQL Server, etc.).
   - Expected vs actual behavior.
   - Any stack traces or minimal code snippets.

### 2. Suggesting Enhancements & New Features

Have an idea for a new feature or improvement?
- Open an issue with the label `enhancement`.
- Describe the motivation, use case, and proposed API design.
- Discussing large features before writing code saves time and helps align on architecture.

### 3. Submitting Pull Requests

1. **Fork** the repository and clone your fork locally.
2. Create a topic branch from `main`:
   ```bash
   git checkout -b feature/my-new-feature
   ```
3. Make your changes and commit using [Conventional Commits](#commit-message-guidelines).
4. Ensure all tests pass and your code compiles cleanly across all target frameworks.
5. Push to your fork:
   ```bash
   git push origin feature/my-new-feature
   ```
6. Open a **Pull Request** against the `main` branch of `M1NG0LL/OpenIddict.Management`.

---

## Development Environment Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (builds and targets .NET 8, 9, and 10).
- Git.
- An IDE such as Visual Studio 2022 / 2026, JetBrains Rider, or VS Code with the C# Dev Kit extension.

### Building the Solution

Clone the repository and build:

```bash
# Clone the repository
git clone https://github.com/M1NG0LL/OpenIddict.Management.git
cd OpenIddict.Management

# Restore dependencies
dotnet restore

# Build the entire solution
dotnet build
```

### Running Tests

```bash
# Run all test suites
dotnet test
```

### Running the Sample Applications

Two sample applications are provided in the `samples/` directory:
- **`Sample.MinimalApi`**: Minimal API configuration with SQLite, management endpoints, and embedded dashboard.
- **`Sample.MvcApp`**: MVC configuration with full OpenIddict server and dashboard integration.

To run a sample:
```bash
dotnet run --project samples/Sample.MinimalApi/Sample.MinimalApi.csproj
```

---

## Coding Standards & Guidelines

### Multi-Targeting Compatibility
All packages in `src/` multi-target **`.NET 8`**, **`.NET 9`**, and **`.NET 10`**:
- Ensure any new APIs or code compile without warnings across all three targets.
- If you need framework-specific APIs, use preprocessor directives:
  ```csharp
  #if NET10_0_OR_GREATER
      // .NET 10 specific code
  #else
      // Fallback for .NET 8 / 9
  #endif
  ```

### Code Style
- Follow the settings defined in [`.editorconfig`](.editorconfig).
- **Nullable Reference Types**: Enabled across the entire solution (`<Nullable>enable</Nullable>`). Do not use non-null assertions (`!`) unless guaranteed safe.
- **File-scoped namespaces**: Use file-scoped namespaces (`namespace OpenIddict.Management.Core;`).
- **Implicit Usings**: Enabled; rely on common implicit imports and keep `using` statements minimal and ordered.

### Public API Documentation
- `<GenerateDocumentationFile>true</GenerateDocumentationFile>` is enabled for all library packages.
- All public types, interfaces, methods, and properties must include descriptive XML doc comments (`/// <summary>...`).

### Static Assets (Dashboard)
- Static assets (CSS, JS, SVG) for the admin dashboard belong in `src/OpenIddict.Management.Dashboard/wwwroot/`.
- Ensure CSS and JS remain vanilla and lightweight without unnecessary external dependencies.

---

## Commit Message Guidelines

We follow the [Conventional Commits](https://www.conventionalcommits.org/) specification:

```text
<type>(<scope>): <subject>
```

### Types:
- `feat`: A new feature
- `fix`: A bug fix
- `docs`: Documentation updates
- `style`: Code style/formatting changes (no production code change)
- `refactor`: Code change that neither fixes a bug nor adds a feature
- `perf`: Performance improvements
- `test`: Adding or correcting tests
- `chore`: Build process, package configuration, or auxiliary tool updates

### Examples:
- `feat(dashboard): add dynamic package version display in sidebar footer`
- `fix(core): handle null token revocation gracefully`
- `docs: update setup guide in README`

---

## Pull Request Checklist

Before submitting your PR, verify the following:
- [ ] Code compiles cleanly with zero warnings (`dotnet build`).
- [ ] All unit and integration tests pass (`dotnet test`).
- [ ] Multi-targeting compatibility is maintained (`net8.0`, `net9.0`, `net10.0`).
- [ ] Public APIs have XML documentation comments.
- [ ] Meaningful commit messages following Conventional Commits.
- [ ] The PR description clearly explains the changes and links any related issues.

---

## License

By contributing to **OpenIddict.Management**, you agree that your contributions will be licensed under the project's [MIT License](LICENSE).

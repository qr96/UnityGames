# Agent Guidelines for UnityGames Repository

This document outlines the essential guidelines and commands for agentic coding agents operating within the UnityGames repository. Adhering to these standards ensures consistency, maintainability, and efficient collaboration.

## 1. Build, Lint, and Test Commands

### 1.1 Build Commands
*   **Unity Editor Build:** The primary build process for this project is managed through the Unity Editor. Agents should assume that standard builds are initiated via the Unity Editor's build pipeline (File -> Build Settings).
*   **Command-Line Build (Advanced):** For automated or CI/CD environments, Unity supports command-line builds. Refer to the official Unity documentation for specific command-line arguments and configurations if such an advanced build is required. A typical command might look like:
    ```bash
    "C:\Program Files\Unity\Hub\Editor\[UnityVersion]\Editor\Unity.exe" -batchmode -nographics -quit -projectPath "C:\Users\Lee\source\repos\UnityGames\GameClient" -executeMethod BuildScript.PerformBuild
    ```
    (Note: `[UnityVersion]` and `BuildScript.PerformBuild` are placeholders and need to be adapted to the specific project setup.)

### 1.2 Linting and Code Analysis
*   **EditorConfig:** This repository uses `.editorconfig` for basic formatting rules. Agents should respect these rules:
    ```ini
    root = true

    [*]
    charset = utf-8
    ```
*   **C# Static Analysis:** For more comprehensive code quality checks, agents should adhere to common C# static analysis practices. Tools like Roslyn Analyzers (built into Visual Studio) or JetBrains Rider's code inspections are typically used. Agents should strive to write code that passes these checks without warnings.
*   **No specific linting command identified.** Agents should focus on writing clean code that adheres to the established style guidelines, and address any warnings reported by the IDE.

### 1.3 Test Commands
*   **Unity Test Runner:** This project is expected to use Unity's built-in Test Runner for unit and integration tests.
*   **Running All Tests:**
    1.  Open the Unity Editor.
    2.  Navigate to `Window -> General -> Test Runner`.
    3.  In the Test Runner window, click "Run All" for Play Mode or Edit Mode tests.
*   **Running a Single Test:**
    1.  Open the Unity Editor.
    2.  Navigate to `Window -> General -> Test Runner`.
    3.  In the Test Runner window, locate the desired test (e.g., by searching for its name).
    4.  Select the specific test or test class and click "Run Selected".
*   **Test File Location:** Tests are typically located in folders named `Tests` or `Editor` within the `Assets` directory, often structured to mirror the main project's folder hierarchy (e.g., `Assets/Scripts/MyFeature/Tests/MyFeatureTests.cs`).
*   **No specific command-line test execution script was identified.** Agents should primarily use the Unity Editor for test execution.

## 2. Code Style Guidelines (C#)

These guidelines are based on common C# best practices and are to be followed by all agents.

### 2.1 Imports (Using Directives)
*   `using` directives should be placed at the top of the file, outside the namespace declaration.
*   Sort `using` directives alphabetically, with `System` namespaces often grouped first, followed by other external libraries, and then project-specific namespaces.
*   Avoid unnecessary `using` directives.

### 2.2 Formatting
*   **Indentation:** Use 4 spaces for indentation. Tabs are not allowed.
*   **Braces:** Place opening braces `{` on a new line for classes, methods, properties, and control flow statements (e.g., `if`, `for`, `while`).
*   **Spacing:**
    *   Use a single space after keywords like `if`, `for`, `while`, `catch`.
    *   Use a single space around operators (`=`, `+`, `-`, `*`, `/`, `==`, etc.).
    *   No space before `(`, `)` for method calls.
*   **Line Length:** Strive for a maximum line length of 120 characters, breaking long lines for readability.

### 2.3 Types
*   **Implicit vs. Explicit:** Use `var` when the type is obvious from the right-hand side of the assignment. Otherwise, use explicit type declarations.
    ```csharp
    // Good
    var player = new Player();
    PlayerController controller = GetComponent<PlayerController>();

    // Avoid
    Player player = new Player(); // Type is obvious
    ```
*   **Nullable Reference Types:** If nullable reference types are enabled, use `?` appropriately for nullable types.

### 2.4 Naming Conventions
*   **PascalCase:**
    *   Classes, Structs, Enums, Interfaces, Delegates
    *   Public Methods, Properties, Events
    *   Constants (if not ALL_CAPS)
*   **camelCase:**
    *   Method parameters
    *   Local variables
    *   Private fields (consider `_camelCase` prefix for private fields)
*   **ALL_CAPS:**
    *   `const` fields (e.g., `public const int MAX_HEALTH = 100;`)

### 2.5 Error Handling
*   **Graceful Degradation:** Design code to fail gracefully where possible.
*   **`try-catch` Blocks:** Use `try-catch` blocks for handling expected exceptions and logging errors. Avoid catching general `Exception` unless absolutely necessary and rethrow if not fully handled.
*   **Logging:** Use Unity's `Debug.Log`, `Debug.LogError`, `Debug.LogWarning` for logging messages.
*   **Assertions:** Use `Debug.Assert` or similar for conditions that should never be false in a healthy codebase, to catch programming errors early.

### 2.6 Comments
*   **Purpose:** Use comments to explain *why* a particular piece of code exists, its design decisions, or complex algorithms, rather than *what* the code does (which should be evident from clear naming and structure).
*   **XML Documentation Comments:** Use XML documentation comments (`///`) for public API members (classes, methods, properties) to describe their purpose, parameters, and return values.

## 3. Cursor and Copilot Rules

No specific Cursor rules (`.cursor/rules/`, `.cursorrules`) or Copilot rules (`.github/copilot-instructions.md`) were found in this repository. Agents should rely on the general guidelines provided above and standard C# practices.

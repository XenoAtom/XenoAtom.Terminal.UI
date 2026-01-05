# XenoAtom.Terminal.UI instructions

## Overview

- In the `readme.md` file, you will find general information about the XenoAtom.Terminal.UI project.
- In the `doc/readme.md` file you will find the documentation for the library of XenoAtom.Terminal.UI.

## Project Structure

- In the `src/XenoAtom.Terminal.UI` folder you will find the code for the library of XenoAtom.Terminal.UI.
- In the `src/XenoAtom.Terminal.UI.Tests` folder you will find the unit tests for the library of XenoAtom.Terminal.UI.
- In the `samples` folder you will find sample applications demonstrating the usage of XenoAtom.Terminal.UI.

## Building and Testing

- To build the project, navigate to the `src` directory and run `dotnet build -c Release`.
- To run the unit tests, navigate to the `src` directory and run `dotnet test -c Release`.
- Ensure that all tests pass successfully before submitting any changes.
- Ensure that user guide documentation and top level readme are updated to reflect any changes made to the library.

## General Coding Instructions

- Follow the coding style and conventions used in the existing code base.
- Write clear and concise comments to explain the purpose and functionality of your code.
- Ensure that your code is well-structured and modular to facilitate maintenance and future enhancements.
- Adhere to best practices for error handling and input validation.
- Write unit tests for any new functionality you add to ensure code quality and reliability.
- Use meaningful variable and method names that accurately reflect their purpose.
- Avoid code duplication by reusing existing methods and classes whenever possible.

## Performance Considerations

- Ensure that the code is optimized for performance without sacrificing readability.
- Ensure that the code minimizes GC allocations where possible.
  - Use `Span<T>`/`ReadOnlySpan<T>` where appropriate to reduce memory allocations.

## Git Commit Instructions

- Write a concise and descriptive commit message that summarizes the changes made.
- Create a commit for each logical change or feature added to facilitate easier code review and tracking of changes.

## Resources

The following libraries and resources are relevant to help specify this project:

- `XenoAtom.Ansi` library: `C:\code\XenoAtom\XenoAtom.Ansi`, the library has a guidance agents.md at `C:\code\XenoAtom\XenoAtom.Ansi\AGENTS.md`
- `XenoAtom.Terminal` library: `C:\code\XenoAtom\XenoAtom.Terminal`, the library has a guidance agents.md at `C:\code\XenoAtom\XenoAtom.Terminal\AGENTS.md`
  - This library depends on `XenoAtom.Ansi`
- `XenoAtom.Collections` library: `C:\code\XenoAtom\XenoAtom.Collections`, used for internal collections handling that are faster than standard .NET collections.
  - It has `UnsafeDictionary` and `UnsafeList` that can be used for internal data structures.
  - These ares structs that can be put as a non read-only fields in other structs/classes.

The NuGet packages of these libraries will be used for the `XenoAtom.Terminal.UI.UI` project.

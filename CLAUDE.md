# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

MacroMate is a Dalamud plugin for Final Fantasy XIV that allows players to store and manage unlimited macros with advanced condition-based automation. The plugin allows binding macros to vanilla macro slots and can automatically swap macros based on game conditions (location, job, content, etc.).

## Build and Development Commands

### Building the Project
```bash
dotnet build MacroMate.sln
```

### Running Tests
```bash
dotnet test MacroMateTests/MacroMateTests.csproj
```

### Debug Build
```bash
dotnet build MacroMate.sln -c Debug
```

### Release Build
```bash
dotnet build MacroMate.sln -c Release
```

## Architecture Overview

### Core Components

- **Plugin.cs**: Main plugin entry point implementing IDalamudPlugin
- **MacroMate.cs**: Core business logic handling macro management and condition-based automation
- **Env.cs**: Environment initialization and dependency injection container

### Key Systems

1. **Condition System** (`MacroMate/Conditions/`)
   - Evaluates game state conditions to determine macro availability
   - Supports location, job, content, player status, and custom conditions
   - Uses expression evaluation for complex condition logic

2. **Macro Tree** (`MacroMate/MacroTree/`)
   - Hierarchical macro organization with groups and individual macros
   - Links macros to vanilla game macro slots
   - Handles macro execution and state management

3. **Serialization** (`MacroMate/Serialization/`)
   - XML-based configuration persistence
   - Versioned serialization for backward compatibility

4. **Subscription System** (`MacroMate/Subscription/`)
   - Allows importing macro sets from external sources
   - YAML-based manifest system for remote macro collections

5. **UI Windows** (`MacroMate/Windows/`)
   - ImGui-based interface for macro management
   - Drag-and-drop support for macro organization

### Dependencies

- **Dalamud.NET.Sdk**: FFXIV plugin framework
- **XSerializer**: XML serialization
- **YamlDotNet**: YAML processing for subscriptions
- **Markdig**: Markdown processing
- **Sprache**: Parser combinators for condition expressions
- **OneOf**: Discriminated unions
- **KTrie**: Efficient text searching

## Code Style and Conventions

- 4-space indentation
- PascalCase for public members, camelCase for private fields
- Comprehensive EditorConfig with ReSharper settings
- C# nullable reference types enabled
- Generator.Equals for value type equality

## Testing

The project uses xUnit for testing with Shouldly assertions. Tests are located in `MacroMateTests/` and can be run with standard dotnet test commands.

## Development Patterns

The `docs/dev/` folder contains development guides and patterns specific to this codebase. Always check this folder first when working on implementation tasks to see if there are relevant guides that explain the established patterns and procedures.

## Development Notes

- The plugin integrates deeply with FFXIV's game state through Dalamud APIs
- Condition evaluation runs on every framework tick for real-time responsiveness
- Macro linking/unlinking is managed automatically based on condition changes
- The UI uses ImGui with custom extensions for enhanced functionality
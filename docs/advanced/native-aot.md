# Native AOT Support

One of the standout features of WebForms Core is its first-class support for Native AOT (Ahead-of-Time) compilation.

## Introduction

Native AOT compiles your .NET application into a single, self-contained native executable. This results in significantly faster startup times and lower memory usage, making it ideal for containerized or serverless environments.

## How it works

Traditional WebForms relies heavily on reflection and runtime code generation, which are incompatible with Native AOT. WebForms Core solves this by:

1.  **Source Generators**: All the logic for creating control fields from `.aspx`/`.ascx` files is generated at compile time.
2.  **AOT-Friendly Serialization**: ViewState serialization is designed to avoid dynamic reflection where possible.
3.  **No `Type.GetType`**: References to controls and types are resolved at compile time.

## Benefits

- **Instant Startup**: No JIT compilation at runtime.
- **Smaller Footprint**: Only the code you actually use is included in the binary.
- **Direct Native Code**: Better performance for compute-intensive tasks.

## Limitations

When targeting Native AOT, you should avoid:
- Dynamic type loading with `Activator.CreateInstance`.
- Complex reflection-based logic in your custom controls.
- Third-party libraries that are not AOT-compatible.

## Getting Started with Native AOT

To enable Native AOT publishing, add the following to your `.csproj`:

```xml
<PropertyGroup>
    <PublishAot>true</PublishAot>
</PropertyGroup>
```

Then publish your application:

```bash
dotnet publish -c Release
```

> **Tip:** Code-only UI (without `.aspx` files) is the most AOT-friendly approach since it avoids the markup parsing step entirely. See [Code-only UI](code-only-ui.md) for details.

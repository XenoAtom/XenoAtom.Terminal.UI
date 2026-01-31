---
title: XenoAtom.Terminal.UI Specifications
---

# XenoAtom.Terminal.UI Specifications

This document contains specifications for the `XenoAtom.Terminal.UI` project.

All names used in this document can be considered as preliminary and subject to change, if we have better alternatives.

## Resources

The following libraries and resources are relevant to help specify this project:

- `XenoAtom.Ansi` library: `C:\code\XenoAtom\XenoAtom.Ansi`, the library has a guidance agents.md at `C:\code\XenoAtom\XenoAtom.Ansi\AGENTS.md`
- `XenoAtom.Terminal` library: `C:\code\XenoAtom\XenoAtom.Terminal`, the library has a guidance agents.md at `C:\code\XenoAtom\XenoAtom.Terminal\AGENTS.md`
  - This library depends on `XenoAtom.Ansi`
- `XenoAtom.Collections` library: `C:\code\XenoAtom\XenoAtom.Collections`, used for internal collections handling that are faster than standard .NET collections.
  - It has `UnsafeDictionary` and `UnsafeList` that can be used for internal data structures.
  - These ares structs that can be put as a non read-only fields in other structs/classes.

The NuGet packages of these libraries will be used for the `XenoAtom.Terminal.UI` project.

For inspiration:

- `XenoUI` prototype library: `C:\code\StackRift\XenoUI`
  - This is a prototype library for a UI framework (based on Vulkan/D3D12 rendering, so not terminal based) that has some concepts that could be relevant for this project
  - Source code of the library is at `C:\code\StackRift\XenoUI\src`
  - The Roslyn Source Generator used in the library is at `C:\code\StackRift\XenoUI\src\XenoUI.SourceGen`
  - The difference is that the `XenoUI` library was initially having a concept of `View` and `Visual` (built from the View), but for terminal UI we will simplify and have only `Visual` components.
  - XenoUI was using some hardcore optimizations like C# function pointers. In terminal UI, we will simplify the code to use regular delegates.

## Objectives

The main objectives for the `XenoAtom.Terminal.UI` project are:

- To create a terminal-based UI framework that leverages the capabilities of the `XenoAtom.Ansi` and `XenoAtom.Terminal` libraries.
- To implement a component-based architecture similar to the one in the `XenoUI` prototype, adapted for terminal environments.
  - The implementation will still be different and unique to terminal UIs. There will be also refinements and changes that will be described in more detail later.
- The library should support rendering of UI components, handling user input, and managing layout in a terminal context.
- The library should allow to build complex UI applications (e.g. in an alternate full screen) but also allow to output UI components inline in a terminal output stream.
  - For inline rendering this is similar to `Spectre.Console`, `Rich` (Python), and other similar libraries.
  - The library should unify both use cases (full screen and inline) under a single API and architecture.
- To provide a source generator (similar to the one in `XenoUI`) to simplify the creation of UI components and binding models.
- To provide a comprehensive set of built-in UI components (e.g. buttons, text inputs, lists, scroll-view etc.) that can be easily extended by users.
- To provide a new modern binding model that does not require complicated boilerplate code
- To provide an innovative component model that allow components to be rebuild locally without needing to re-build the entire UI tree.
- To ensure high performance and responsiveness, even in complex UIs.
- To provide thorough documentation and examples to help users get started with the library.
- To provide easy theming and styling options for UI components.
- To provide easy templating and layout management for arranging UI components.
- To provide easy animation capabilities for UI components.
- The library is not yet another XAML library or similar. The library should be closer to the SwiftUI paradigm, but we will see that we are going to innovate and create a unique approach that is not just copying existing paradigms.
- To provide an undo/redo system for UI state changes.
- To provide a UI that can both respond to keyboard input and mouse input (where supported by the terminal).
- To provide easy global commands (with keyboard shortcuts) within an application.
- To have comprehensive unit tests that will exercise all components and features (e.g. by rendering to a virtual terminal / InMemory terminal from `XenoAtom.Terminal` and verifying the output).

## Core Concepts

### Dispatcher

The `Dispatcher` will be responsible for managing the UI thread and ensuring that UI updates are performed on the correct thread.

We should only have a static `Dispatcher` instance for the entire application, but we should verify that all accesses to all properties and methods are done on the correct (main) thread. `VerifyAccess()` and `CheckAccess()` methods will be provided for this purpose.

A `DispatcherObject` base class will be provided for all objects that need to interact with the `Dispatcher`. This class will provide access to the `Dispatcher` instance and ensure that all operations are performed on the correct thread.

A `IDispatcherObject` interface will also be provided for cases where we cannot inherit from `DispatcherObject`.

The Dispatcher will provide methods for scheduling work on the UI thread, such as `Invoke`, `BeginInvoke`, and `InvokeAsync` as well as the run loop management methods (e.g. `Run()`, `Exit()`, etc.)

### Visual Tree

Logical and visual tree concepts will be similar to other UI frameworks, but adapted for terminal environments.

#### `Visual` Class

The base component will for visuals will be the `Visual` class. This class will provide basic properties and methods for rendering and layout.

The `Visual` class will provide support for:

- Accessing visual children (e.g. `protected int ChildrenCount {get; }` and `protected abstract Visual GetChild(int index)` methods)
- State/Flags management of the state of the component (e.g. dirty, needs layout, focused, etc.)
- Routing of routed events (e.g. keyboard, mouse, etc.)
- Layout and rendering methods

Similar to other UI frameworks, we might want to derive subclasses from `Visual` to provide more specialized functionality, such as `UIElement`, `FrameworkElement`, and `Control`. But we would like to limit the number of subclasses for the foundation to avoid unnecessary complexity.

#### `LayoutVisual` class

Inheriting from `Visual` (or another framework base), the `LayoutVisual` class will provide support for layout management and protocol. 

The layout protocol will be similar to other UI frameworks, with methods for measuring and arranging child components.

It should be closer SwiftUI layout protocol (but WPF/Avalonia layout can be also inspired), but we will need to adapt it for terminal environments.

All UI containers (e.g. panels, XStack/YStack panels, grids, etc.) will inherit from `LayoutVisual`.

### Binding Model

The binding model is unique to this project.

It resolves around the concept that when interacting with the property, the system both tracks automatically:

- Reading the property value (for dependency tracking)
- Writing the property value (for change notification)

Then when we evaluate different aspects of the UI, we track which properties were read, and we subscribe to change notifications for those properties.

When a property changes, we re-evaluate only the parts of the UI that depend on that property.

For example:

- If certain properties are read during the layout of the component, we re-measure/re-arrange only those components when the property changes.
- If certain properties are read during the rendering of the component, we re-render only those components when the property changes.
- If certain properties are read during the e.g. `Initialize()` or `Update()` of the component, we re-invoke only those methods when the property changes.

It means that when a property on a model is defined:

```csharp

    [Bindable]
    public partial bool IsActive {get; set;}
```

We generate internally a code similar to:

```csharp
// implicitly implement MyComponent.IBindings
partial class MyComponent : MyComponent.IBindings
{
    [global::System.Diagnostics.DebuggerBrowsable(global::System.Diagnostics.DebuggerBrowsableState.Never)]
    private bool _isActive;

    [global::System.CodeDom.Compiler.GeneratedCode("XenoAtom.Terminal.UI.SourceGen", "1.0.0")]
    public bool IsActive
    {
        get => BindingManager.Current.GetValue(ref _isActive, __IsActive__BindingAccessor);
        set => BindingManager.Current.SetValue(ref _isActive, value, __IsActive__BindingAccessor);
    }

    // Allow to access bindings via interface this.Bind.IsActive
    [global::System.CodeDom.Compiler.GeneratedCode("XenoAtom.Terminal.UI.SourceGen", "1.0.0")]
    // Access to bindings is provided via a C# extension member (this.Bind)

    [global::System.CodeDom.Compiler.GeneratedCode("XenoAtom.Terminal.UI.SourceGen", "1.0.0")]
    Binding<bool> IBindings.IsActive => new Binding<bool>(this, new Binding);

    [global::System.CodeDom.Compiler.GeneratedCode("XenoAtom.Terminal.UI.SourceGen", "1.0.0")]
    private unsafe class __IsActive__BindingAccessor : global::XenoAtom.Terminal.UI.BindingAccessor<bool>
    {
        [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
        public static global::XenoAtom.Terminal.UI.BindingAccessor<bool> Instance { get; } = new(nameof(IsActive), StaticGetter, StaticSetter);
    
        [global::System.Diagnostics.DebuggerNonUserCode]
        public static string StaticGetter(object obj) => ((MyComponent)obj).IsActive;
    
        [global::System.Diagnostics.DebuggerNonUserCode]
        public static void StaticSetter(object obj, bool value) => ((MyComponent)obj).IsActive = value;
    }

    [global::System.CodeDom.Compiler.GeneratedCode("XenoAtom.Terminal.UI.SourceGen", "1.0.0")]
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    public interface IBindings : BaseComponent.IBindings
    {
        Binding<bool> IsActive {get; }
    }

```

The `BindingManager` attached to a `Dispatcher` will track all property accesses and changes.

When a property is read, the `BindingManager` will record that the current evaluation context (e.g. layout, rendering, etc.) depends on that property.

When a property is written, the `BindingManager` will notify all evaluation contexts that depend on that property to re-evaluate.

The tracking can be enabled/disabled via `BindingManager.BeginTracking()` and `BindingManager.EndTracking()` methods.

The tracking should also allow/disallow write operations based on the context (e.g. disallow writes during rendering).

Implicitly we don't need to indicate that a property is used for layout or rendering. The system will track that automatically based on when the property is read.

The `Binding<T>` is a lightweight struct that represents a binding to a property on a component instance. It is what is used in the binding manager to track biding usages.

## Environment Key Values

The UI framework should support the concept of environment key values, similar to SwiftUI environment values.

The idea is that all styles and themes can be defined via environment key values, including the templating of components.

An `Environment` class will be provided that allows to define and access environment key values.

For each Visual, a potential `Environment` instance will be associated.

When a Visual is created, it will inherit the `Environment` instance from its parent Visual.

Not every Visual has to have an `Environment` instance, only those that need to override or define new environment key values.

Ultimately, a root `Environment` instance will be associated with the root Visual of the application (So that default values, and themes can be defined there).

Accessing this environment would be similar to `PropertyKey<T>` or `DependencyProperty` (or `EnvironmentKey<T>`) in other UI frameworks. Pick the best name.

The storage of the environment should be efficient for both storing values that are value types and are less or equal 8 bytes (`sizeof(ulong)`) or storing references types in a different dictionary.

Keys accessed via the environment should be tracked by the binding manager, so that when an environment key value changes, all components that depend on that key value are notified and re-evaluated.

The difference with regular property binding is that the instance of the binding for a property is not tied to a specific Environment instance but rather an intermediate singletion instance that acts as "get the value of this property key from the current environment". The current environment is determined by the Visual instance that is currently being evaluated, (e.g. and can be fetched via ThreadLocal or other means).

### Routed Events

We will use the concept of RouterEvents.

The difference with other UI framework is that we will allow only a single handler class per routed event type.

The RoutedEvent should be easily defined via source generator attributes.

For example the following user code:

```csharp
    [RoutedEvent(RoutingStrategy.Preview | RoutingStrategy.Bubble)]
    protected virtual void OnPointerPressed(PointerPressedEventArgs e)
    {
        
    }  
```

will generate code similar to:

```csharp
    public static readonly RoutedEvent<PointerPressedEventArgs> PointerPressedEvent = RoutedEvent.Register<MyVisual, PointerPressedEventArgs>(
        nameof(PointerPressed),
        static (sender, args) => (sender as MyVisual)?.OnPointerPressed(args),
        RoutingStrategy.Preview | RoutingStrategy.Bubble);

    public event EventHandler<PointerPressedEventArgs> PointerPressed
    {
        add => AddHandler(PointerPressedEvent, value);
        remove => RemoveHandler(PointerPressedEvent, value);
    }
```

### Functional values

In UI frameworks, it is often required to bind to a dynamic text or to have a sub-component that is replaced dynamically. 

The entire UI should support functional values for all properties.

For example a TextBlock could be declared with a `new TextBlock(() => $"A model property: {model.SomeProperty}")` constructor overload.

When the model.SomeProperty changes, the TextBlock will automatically update its text.

We would introduce a `FuncObservable<T>` type that would represent a functional value that can be observed for changes.

A `FuncObservable<T>` can be both:

- A `T` initial value (via a `Value` property)
- Or a `Func<T>` delegate that is evaluated to get the initial value.
- Or a `Binding<T>` for 2-way binding (mainly for e.g. bool, I'm not sure it will be used for Visual)
- Then the value stays initialized with its initial value until any of the bound properties change.

The `FuncObservable<T>` would be created from a `Func<T>` delegate and would track all bindings accessed during the evaluation of the function.

When any of the bound properties change, the `FuncObservable<T>` would notify its observers that the value has changed.

The `FuncObservable<T>` should be used also for all visual components that can be composed.

For instance, e.g. a Button could be declared with a `new Button(() => model.IsComplexButton ? new TextBlock($"Count: {model.Count}") : new TextBlock("Simple Button"))` constructor overload.

When the model.IsComplexButton changes, the Button will automatically update its content.

The name `FuncObservable<T>` is a bit verbose and we might want to find a better name for it. (e.g. `FuncValue<T>`, `DynamicValue<T>`, etc.)

Some components will have a list of logical children (e.g. XStack, YStack, Grid...etc.) and it will require to think about how to support this with list of functional values, (while the list itself will need to be tracked as well for binding notifications/changes e.g. if a list change)

### Rendering

The UI framework should support both:
- Inline rendering of UI components in a terminal output stream (e.g. where scrolling is done by the terminal itself
- Full screen rendering of UI components in an alternate terminal screen (e.g. where the UI framework manages the entire terminal screen and scrolling is done by the framework, if necessary)

The rendering should be optimized in both scenarios.

One approach that could be used is to have a RenderingAbstraction can both target an inline terminal output stream and a full screen terminal output stream.

For the fullscreen terminal, we might want to implement a double-buffering mechanism to minimize flickering and improve performance.

The offline buffer would represent the state of each cell in the terminal (e.g. character, foreground color, background color, attributes, etc.)

We could use a double-buffering approach where we have a front buffer (the current state of the terminal) and a back buffer (the desired state of the terminal), and we would compute the differences between the two buffers to generate the minimal set of ANSI escape codes needed to update the terminal.

A single cell should be represented as a struct, ideally fitting in 16 bytes, so that we could allow .NET SIMD optimizations for buffer comparisons and updates.

The difficulty for inline rendering is to manage the terminal cursor position correctly, especially when the terminal output stream is mixed with other non-UI output.

We want to support live elements in the terminal output stream, similar to how `Spectre.Console` and `Rich` (Python) libraries do it (e.g. progress bars, spinners, live tables, etc.)

## Controls

A set of built-in controls will be provided, including but not limited to:

- Button
- TextBlock
- TextBox
- CheckBox
- RadioButton
- ComboBox
- ListBox
- GroupBox (with Header support)
- ScrollViewer
- Panels (e.g. XStack, YStack, Grid, etc.)
- Window and Dialog components for full screen applications.
- Menu and MenuItem components for global commands and context menus.
- ProgressBar and Spinner components for indicating progress.
- TabControl for tabbed interfaces.
- TreeView for hierarchical data display.
- DataGrid/Tables for tabular data display.
- StatusBar for displaying status information, messages or e.g. key bindings hints.

Visual should have default support for:

- Enabled/Disabled state
- Focus state
- Hover state (where mouse is supported)
- Pressed state (for buttons and similar controls)
- Selected state (for list items, combo box items, etc.)
- ...etc.

Some state like Enabled/Disabled could be inherited from parent components (e.g. can be set on a container and all child components would inherit it).

## Theming and Styling

The UI framework should provide a flexible theming and styling system.

Themes can be defined via environment key values, allowing to easily switch between different themes (e.g. light/dark mode).

Styles can be defined for individual components or groups of components, allowing to customize the appearance of the UI.

When changing a theme or styles, all UI components should automatically update their appearance based on the new theme or styles (based on the binding model and tracking of property accesses).

In classical UI frameworks, templating is often heavy and complex as it requires to use basic visual shapes to define a visual (e.g. rectangles, rounded rectangles, paths, text, etc...). For terminal UI, it is unclear yet how templating should be done, but it should be vastly simpler.

## Animation

The UI framework should provide support for animating UI components.

All ease functions from `XenoUI` can be re-used here.

Thanks to the binding model, animating a property would be as simple as updating the property value over time, and the UI framework would automatically update the UI based on the new property value. If an animation changes in the middle, it should keep the existing running value as the start value for the new animation.

No explicit code should be done to animate values, as the system would interpolate between them (e.g. colors, positions, sizes, bool (1/0), etc.)

## Undo/Redo System

The UI framework should provide an undo/redo system for UI state changes.

The undo/redo system would track changes to properties on components and allow to revert or re-apply those changes.

The undo/redo system would need to be integrated with the binding model to ensure that changes are tracked correctly and that the UI is updated appropriately when undoing or redoing changes.

The undo/redo system should support grouping of changes, so that multiple changes can be undone or redone as a single operation.

## Source Generator

An increment Roslyn Source Generator will be provided to simplify the creation of UI components and binding models.

User code should be able to define components and properties using attributes, and the source generator would generate the necessary boilerplate code.

It is really important that the libraries feels easy and lightweight to use, so the source generator should help in this regard by reducing the amount of boilerplate code that users need to write.

The `XenoUI` project has an example of a source generator `XenoUI.SourceGen` that can be used as a reference for implementing the source generator for this project.

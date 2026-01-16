# Group

`Group` is a border-like container with optional corner labels (top-left/top-right/bottom-left/bottom-right).

Screenshot placeholder:

![Group](../../img/screenshots/group.png)

## Basic usage

```csharp
new Group("Settings")
    .Content(new VStack(
        new CheckBox("Enabled"),
        new TextBox("Name")
    ));
```


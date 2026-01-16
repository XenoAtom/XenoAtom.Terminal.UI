# Border

`Border` draws a border around a single content visual.

Screenshot placeholder:

![Border](../../img/screenshots/border.png)

## Basic usage

```csharp
new Border(new TextArea("TextArea inside a Border"));
```

## Dynamic content

Use the factory constructor to dynamically recompute content:

```csharp
new Border(() => new TextBlock(DateTime.Now.ToString("T")));
```


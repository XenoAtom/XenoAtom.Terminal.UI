# MaskedInput

`MaskedInput` is a TextBox-like control for entering sensitive values.

Screenshot placeholder:

![MaskedInput](../../img/screenshots/maskedinput.png)

## Basic usage

```csharp
new MaskedInput("password");
```

MaskedInput derives from TextBox and reuses the same selection/cursor behaviors.

## Clipboard mode

MaskedInput can restrict copy/cut behaviors depending on the configured clipboard mode.


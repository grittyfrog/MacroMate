# Macro Mate Auto Translation

Macro Mate support auto-translation. To create an auto translation payload
we expect a string of the form `\uE040Display Text|group,key\uE041`. 

For IPC the `Display Text` does not matter as long as the `group` and `key` are correct.

If you have a Dalamud `AutoTranslatePayload` you can create this payload like this:

```csharp
var payload = $"\uE040Payload|{payload.Group},{payload.Key}\uE041"
```

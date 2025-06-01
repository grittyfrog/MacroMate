# Subscriptions

Subscriptions let you create a Macro Mate group that is subscribed to an external source of macros. Macros will be downloaded from this subscription and placed in the group. 

To create a subscription select `New > Subscription` and enter the Subscription URL.

When the external source is updated, you will be notified that an update is available. Right click on the group
and select 'Subscription > Sync' to update.

You can change subscribed macros, but changes to `Name`, `Icon` and `Lines` will be overwritten on sync. 

## Creating Subscriptions

A Subscription source must have a YAML file with the following format:

```yaml
name: "My Subscription Name"
macros:
  - name: The Name of the macro
    group: Group/Low-Level Group
    iconId: 4507
    lines: |
      /echo Hello World
      /echo This is my macro
    notes: |
      Notes about the macro
      Typically used as a changelog

  - name: A Macro that uses Markdown
    group: Group/Another Group
    iconId: 4507
    markdownUrl: a/relative/markdown/file.md
    markdownMacroCodeBlockIndex: 0 

  - name: A Macro that uses a raw url
    group: Group/Another Group
    iconId: 4507
    rawUrl: a/relative/file/content.macro
```

Each macro accepts the following fields:

- **name** (required): The name of the macro
- **group** (optional): The [path](./paths.md) to the group this macro should be stored in. 
- **iconId** (optional): The icon id to use for the macro
- **lines** (see note): Macro lines, also support [auto translation format](./auto-translation.md)
- **notes** (optional): Notes about the macro that will be shown in a tooltip
- **rawUrl** (see note): A relative path to a raw file. The whole file will be used as the macro body
- **markdownUrl** (see note): A relative path to the markdown file to parse
- **markdownMacroCodeBlockIndex** (optional): The index of the code block in the markdown file to read the macro from (default: 0)

Note: One of `lines`, `markdownUrl` or `rawUrl` must be provided. If more then one are provided Macro Mate will priorities `lines` then `rawUrl` and then `markdownUrl`

### Markdown Parsing

If the `markdownUrl` field is provided Macro Mate will read the markdown file indicated (must be on the same host). 
The first code block in the file will be read as the macro lines (unless `markdownMacroCodeBlockIndex` is provided).

## Examples

- https://github.com/grittyfrog/Macros (example repository)

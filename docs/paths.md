# Macro Mate Paths

Macro Mate IPC uses paths to identify groups and nodes in the existing tree.

Paths are a list of segments delimited by forward slashes (`/`). I.e: `/segment 1/segment 2/segment 3`.

**Quickstart:**

```
/                     Select root 
/My Group             Select 'My Group' under root
/My Group/Subgroup    Select 'Subgroup' under 'My Group' under root
/@0                   Select the first child of root
/My Group/@2          Select the third child of 'My Group'
/My Group@1           Select the second child named 'My Group' under root
/My\/Group            Select the child named 'My/Group' under root
/My\@Group            Select the child named 'My@Group' under root
/My\\Group            Select the child named 'My\Group' under root
```

Paths always start with `/` which indicates the root node of the macro tree. Paths also treat `@` and `\` as special characters which must be escaped if used in a macro name.

Each segment is either a **name**, **index** or **named index**.

# Reference

## Name Segment

By default a segment is a Name segment. Name segments match nodes in the tree by name.

For example, the segment `My Group` matches a node with the same name. 

A name segment must have at least one character, empty name segments will result in an error.

If multiple children in the parent group are called `My Group` the first will be chosen. Use a
named index if a different child should be selected.

## Index Segment

An index segment has the form `@<index>` (i.e. `@0`, `@1`, `@2`, etc). It matches a child
at the N-th index of the parent group.

index segments are 0-indexed, meaning `@0` targets the first child, `@1` the second and so on.

## Named Index

A named index has the form `name@<index>` (i.e. `My Group@0`, `My Group@1`, etc). This index
is used to select the N-th child that shares the same name. 

For example, if the parent has three `My Group` children, then `My Group@2` will select the third
child.

named index segments are 0-indexed, meaning `My Group@0` targets the first child with the name `My Group` and `My Group@1` targets the second child with the right name.

# Escaping Rules

Because `@`, `/` and `\` are special characters they need to be escaped if they are used in a 
macro name.

To escape a character prefix it with `\`, for example:

- `\\` will be treated as a literal `\`, instead of the escape character
- `\@` will be treated as a literal `@`, instead of a index signifier
- `\/` will be treated as a literal `/`, instead of a segment separator

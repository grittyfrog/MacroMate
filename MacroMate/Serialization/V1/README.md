# Purpose

This folder defines the V1 save format. 

Much of this structure will mirror MateNode/ConditionExpr/etc 

Care must be taken when making changes to any of these files, since a backwards incompatible change may 
break existing users save files.

# Why XML?

If you're reading this README you're justifibly wondering why someone would write XML these days. 

First, these are the requirements for Macro Mate's config:

1. Macro lines should be stored raw with no adjoining syntax. This is to allow people to directly copy macros out
   of the config file and into the game. The idea is to give people an escape hatch when Dalamud is not updated
   but they desperately need a particular macro.
2. Given we are storing a tree, the config format should handle nesting well
3. Ideally there is a good C# library for interacting with the library.
4. The format should support comments, because we store IDs in several places and we want to be able to annotate those
   ids with the name of the thing they are referencing

I started with JSON, but it's immediately ruled out by the lack of multi-line support and comments.

The next natural step is YAML. YAML worked pretty well but I had a lot of trouble getting the various C# 
libraries to places comments in the correct spot. They handle postfix comments fine but I wanted comments on the 
line before and the libraries didn't seem to provide a nice way to do this.

After that, I tried TOML. Unfortunately I didn't find TOML's nested object approach to produce very clear trees, and
I found the library support lacking.

Ultimately that left me with XML, and here we are. 

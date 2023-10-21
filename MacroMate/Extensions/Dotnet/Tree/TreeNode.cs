using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

/// Pretend the Dotnet standard library has a useful tree implementation
namespace MacroMate.Extensions.Dotnet.Tree;

public abstract class TreeNode<T> where T : TreeNode<T> {
    public Guid Id { get; init; } = Guid.NewGuid();
    public T? Parent { get; private set; } = null;

    private ImmutableList<T> _children = ImmutableList<T>.Empty;
    public ImmutableList<T> Children {
        get { return _children; }
        private set {
            foreach (var child in value) {
                child.Parent = (T)this;
            }
            _children = value;
        }
    }

    public abstract string NodeName { get; }

    public IEnumerable<T> Values() {
        yield return (T)this;
        foreach (var child in Children) {
            foreach (var childValue in child.Values()) {
                yield return childValue;
            }
        }
    }

    /// Returns this and all its Descendants in depth-first order.
    public IEnumerable<T> SelfAndDescendants() {
        yield return (T)this;
        foreach (var descendant in Descendants()) {
            yield return descendant;
        }
    }

    /// Returns all the Descendants of this node in depth-first order
    public IEnumerable<T> Descendants() {
        foreach (var child in Children) {
            yield return child;
            foreach (var descendant in child.Descendants()) {
                yield return descendant;
            }
        }
    }

    public IEnumerable<T> Ancestors() {
        var node = this.Parent;
        while (node != null) {
            yield return (T)node;
            node = node.Parent;
        }
    }

    /// Find the siblings of this node by the given offset.
    ///
    /// For example:
    /// `SiblingByOffset(0)` == Returns this
    /// `SiblingByOffset(1)` = Sibling immediately after this
    /// `SiblingByOffset(2)` = Sibling two steps after this
    /// `SiblingByOffset(-1)` == Sibling immediately before this
    ///
    /// If no sibling exists at this offset, returns null
    /// If this node is the root, returns null
    public T? SiblingByOffset(int offset) {
        if (Parent == null) { return null; }
        var indexOfThis = Parent.Children.IndexOf((T)this);
        var indexOfSibling = indexOfThis + offset;
        return Parent.Children.ElementAtOrDefault(indexOfSibling);
    }

    /// Find the siblings of this node, starting with the nodes immediately
    /// next to this node, then the ones after that. "Preceeding" siblings are considered
    /// closer then siblings that come after this node
    public IEnumerable<T> ClosestSiblings() {
        var offset = 1;
        while (true) {
            var precursorSibling = SiblingByOffset(offset);
            if (precursorSibling != null) { yield return precursorSibling; }

            var postcursorSibling = SiblingByOffset(offset * -1);
            if (postcursorSibling != null) { yield return postcursorSibling; }

            if (precursorSibling == null && postcursorSibling == null) { break; }

            offset += 1;
        }
    }

    /// The Path of nodes from the Root to (and including) this node.
    public IEnumerable<T> Path() => Ancestors().Reverse().Concat(new[] { (T)this });

    /// The Path of nodes from parent to (and including) this node
    ///
    /// If `parent` is not a parent of `this` then the whole path will be returned
    public IEnumerable<T> PathFrom(T parent) =>
        Ancestors().TakeWhile(node => node != parent).Reverse().Concat(new [] { (T)this });

    /// The Path of nodes from Root to this, excluding Root and this
    public IEnumerable<T> Breadcrumbs() => Path().Skip(1).SkipLast(1);

    /// Attach a subtree to this node.
    public T Attach(T childNode) {
        this.Children = this.Children.Add(childNode);
        return (T)this;
    }

    public T Attach(IEnumerable<T> children) {
        this.Children = this.Children.AddRange(children);
        return (T)this;
    }

    public void Detach(T childNode) {
        if (childNode.Parent == this) {
            this.Children = this.Children.Remove(childNode);
        }
        childNode.Parent = null;
    }

    public bool Contains(T childNode) => this.Values().Contains(childNode);

    /// Attach `childNode` positioned above/below to `relativeTo`.
    public T AttachRelativeTo(T childNode, Guid relativeTo, TreeNodeOffset offset) {
        var relativeToIndex = Children.FindIndex(node => node.Id == relativeTo);
        var offsetIndex = offset switch {
            TreeNodeOffset.ABOVE => 0,
            TreeNodeOffset.BELOW => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(offset))
        };
        var childTargetIndex = Math.Clamp(relativeToIndex + offsetIndex, 0, Children.Count);

        this.Children = this.Children.Insert(childTargetIndex, childNode);
        return (T)this;
    }

    public enum WalkBehaviour { CONTINUE, RETURN, DELETE }

    /// Update some or all of the nodes in this tree.
    public T? Walk(Func<T, WalkBehaviour> update) {
        var behaviour = update((T)this);
        if (behaviour == WalkBehaviour.RETURN) { return (T)this; }
        if (behaviour == WalkBehaviour.DELETE) { return null; }

        var updatedChildren = this.Children
            .Select(child => child.Walk(update))
            .WithoutNull()
            .ToImmutableList();

        this.Children = updatedChildren;
        return (T)this;
    }

    public void Delete(Guid nodeId) {
        Walk(update: node => node.Id == nodeId ? WalkBehaviour.DELETE : WalkBehaviour.CONTINUE);
    }

    /// Move Subtree `source` into subtree `target`
    public T MoveInto(T target) {
        if (this == target) { return (T)this; } // Don't move ourself
        if (this.Contains(target)) { return (T)this; }

        if (this.Parent != null) {
            this.Parent.Detach((T)this);
        }
        target.Attach((T)this);
        return (T)this;
    }

    public T MoveBeside(T target, TreeNodeOffset offset) {
        if (this == target) { return (T)this; } // Don't move ourself
        if (this.Contains(target)) { return (T)this; }
        if (target.Parent == null) { return (T)this; } // Don't move next to Root

        if (this.Parent != null) {
            this.Parent.Detach((T)this);
        }

        target.Parent.AttachRelativeTo((T)this, target.Id, offset);
        return (T)this;
    }

    public string TreeString() {
        var sb = new StringBuilder();
        sb.AppendLine($"{this.NodeName} ({this.Id})");
        foreach (var child in Children) {
            sb.Append(child.TreeString());
        }
        return sb.ToString();
    }
}

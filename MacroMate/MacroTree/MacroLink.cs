using System.Collections.Generic;
using System.Linq;
using MacroMate.Extensions.Dalamud.Macros;

namespace MacroMate.MacroTree;

public class MacroLink {
    public VanillaMacroSet Set { get; set; } = VanillaMacroSet.INDIVIDUAL;
    public List<uint> Slots { get; set; } = new();

    public IEnumerable<VanillaMacroLink> VanillaMacroLinks() {
        return Slots.Select(slot => new VanillaMacroLink(Set, slot)).ToList();
    }

    public MacroLink Clone() => new MacroLink {
        Set = this.Set,
        Slots = Slots.ToList()
    };

    public string Name() {
        if (Slots.Count == 0) { return "Unbound"; }

        var setName = Set switch {
            VanillaMacroSet.INDIVIDUAL => "Individual",
            VanillaMacroSet.SHARED => "Shared",
            _ => "???"
        };
        var slots = string.Join(" ", Slots);
        return $"{slots} {setName}";
    }

    public bool IsBound() => Slots.Count > 0;

    public override string ToString() => Name();

    public override bool Equals(object? obj) {
        return obj is MacroLink link &&
            Set == link.Set &&
            Slots.SequenceEqual(link.Slots);
    }

    public override int GetHashCode() {
        unchecked // Overflow is fine, just wrap
        {
            int hash = 17;
            // Suitable nullity checks etc, of course :)
            hash = hash * 23 + Set.GetHashCode();
            foreach (var slot in this.Slots) {
                hash = hash * 23 + (int)slot;
            }
            return hash;
        }
    }

}

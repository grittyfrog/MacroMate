using System;
using System.Collections.Generic;
using System.Linq;
using MacroMate.Conditions;
using MacroMate.Extensions.Dalamud.Macros;
using MacroMate.Extensions.Dotnet;
using MacroMate.Extensions.Dotnet.Tree;

namespace MacroMate.MacroTree;

public abstract class MateNode : TreeNode<MateNode> {
    public required string Name { get; set; }
    public override string NodeName => Name;

    public class Group : MateNode {}

    public class Macro : MateNode {
        public uint IconId { get; set; } = 66001;
        public MacroLink Link = new();

        public string Lines { get; set; } = "";
        public ConditionExpr.Or ConditionExpr = Conditions.ConditionExpr.Or.Empty;

        /// Get the Vanilla Macros that can be produced by this macro.
        ///
        /// Each block of 15 lines in this Macro will produce one Vanilla macro
        public IEnumerable<VanillaMacro> VanillaMacros() {
            var lineChunks = Lines
                .Split("\n")
                .Chunk(15)
                .Select(chunkLines => string.Join('\n', chunkLines))
                .ToList();

            foreach (var (chunk, index) in lineChunks.WithIndex()) {
                // Only append a number to the title if there is more then one macro
                var title = lineChunks.Count > 1 ? $"{this.Name} {index+1}" : this.Name;

                yield return new VanillaMacro(
                    IconId: this.IconId,
                    Title: title,
                    LineCount: (uint)chunk.Count(c => c == '\n'),
                    Lines: new(() => chunk),
                    IconRowId: 1
                );
            }
        }

        public IEnumerable<VanillaMacroLink> VanillaMacroLinks() => Link.VanillaMacroLinks();

        /// Get the get the VanillaMacros that apply for this node, and it's corresponding macro slot
        ///
        /// If VanillaMacro is null, the slot should be unlinked
        public IEnumerable<(VanillaMacroLink, VanillaMacro?)> VanillaMacroLinkBinding() {
            var vanillaMacros = VanillaMacros().ToList();

            // If we have more slots then macros we want to pad the remaining slots
            // with empty macros so they get removed and we don't leave the previous
            // macro linked.
            var paddingNeeded = Math.Max(0, Link.Slots.Count - vanillaMacros.Count);
            var paddedVanillaMacros = vanillaMacros
                .Concat(Enumerable.Repeat<VanillaMacro?>(null, paddingNeeded));

            return VanillaMacroLinks().Zip(paddedVanillaMacros);
        }

        public bool SatisfiedBy(CurrentConditions conditions) {
            return ConditionExpr.SatisfiedBy(conditions);
        }

        public Macro AddAndExpression() {
            ConditionExpr = this.ConditionExpr.AddAnd();
            return this;
        }

        public Macro DeleteAndExpression(int andIndex) {
            ConditionExpr = ConditionExpr.DeleteAnd(andIndex);
            return this;
        }

        public Macro SetCondition(int andIndex, int conditionIndex, ICondition condition) {
            ConditionExpr = ConditionExpr.UpdateAnd(
                andIndex,
                (and) => and.SetCondition(conditionIndex, condition)
            );
            return this;
        }

        public Macro AddCondition(int andIndex, ICondition condition) {
            ConditionExpr = ConditionExpr.UpdateAnd(
                andIndex,
                (and) => and.AddCondition(condition)
            );
            return this;
        }

        public Macro DeleteCondition(int andIndex, int conditionIndex) {
            ConditionExpr = ConditionExpr.UpdateAnd(
                 andIndex,
                 (and) => and.DeleteCondition(conditionIndex)
             );
            return this;
        }
    }
}

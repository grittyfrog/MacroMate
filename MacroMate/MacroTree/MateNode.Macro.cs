using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Text.SeStringHandling;
using MacroMate.Conditions;
using MacroMate.Extensions.Dalamud;
using MacroMate.Extensions.Dalamud.Macros;
using MacroMate.Extensions.Dalamud.Str;
using MacroMate.Extensions.Dotnet;
using MacroMate.Extensions.Dotnet.Tree;
using MacroMate.Ipc;

namespace MacroMate.MacroTree;

public abstract partial class MateNode : TreeNode<MateNode> {
    public class Macro : MateNode {
        public uint IconId { get; set; } = 66001;
        public MacroLink Link = new();
        public bool LinkWithMacroChain = Env.MacroConfig.UseMacroChainByDefault;
        public string Notes { get; set; } = "";

        private SeString _lines = "";
        public SeString Lines {
            get { return _lines; }
            set { _lines = value.NormalizeNewlines(); }
        }

        /** <summary>If true: ignore `ConditionExpr` and always link this macro</summary> */
        public bool AlwaysLinked { get; set; } = false;

        /** <summary>The conditions when this macro should be linked</summary> */
        public ConditionExpr.Or ConditionExpr = Conditions.ConditionExpr.Or.Empty;

        public Macro Clone() => new Macro {
            Name = this.Name,
            IconId = this.IconId,
            Link = this.Link.Clone(),
            Lines = this.Lines,
            LinkWithMacroChain = this.LinkWithMacroChain,
            ConditionExpr = ConditionExpr
        };

        public void StealVanillaValuesFrom(Macro other) {
            Name = other.Name;
            Link = other.Link.Clone();
            Lines = other.Lines;

            if (other.IconId != 0) {
                IconId = other.IconId;
            }

            // We intentionally do not copy ConditionExpr since the intent of this function is to steal
            // values from a macro created externally (either from the game or from IPC) which do not provide
            // conditions
            ConditionExpr = other.ConditionExpr;
        }

        public string PathString => string.Join("/", SelfAndAncestors().Reverse().Skip(1).Select(n => n.Name));

        /// Get the Vanilla Macros that can be produced by this macro.
        ///
        /// Each block of 15 lines in this Macro will produce one Vanilla macro
        public IEnumerable<VanillaMacro> VanillaMacros() {
            var lineChunks = Lines
                .SplitIntoLines()
                .Let(lines => MaybeInsertMacroChainCommands(lines))
                .Chunk(15)
                .Select(chunkLines => SeStringEx.JoinFromLines(chunkLines))
                .ToList();

            foreach (var (chunk, index) in lineChunks.WithIndex()) {
                // Only append a number to the title if there is more then one macro
                var title = lineChunks.Count > 1 ? $"{this.Name} {index+1}" : this.Name;

                var lineCount = chunk.SplitIntoLines().Count();
                yield return new VanillaMacro(
                    IconId: this.IconId,
                    Title: title,
                    LineCount: (uint)lineCount,
                    Lines: new(() => chunk),
                    IconRowId: 1
                );
            }
        }

        private IEnumerable<SeString> MaybeInsertMacroChainCommands(IEnumerable<SeString> lines) {
            if (!LinkWithMacroChain) { return lines; }
            if (!Env.PluginInterface.MacroChainPluginIsLoaded()) { return lines; }

            return MacroChainSupport.MaybeInsertMacroChainCommands(Link, lines);
        }

        public IEnumerable<VanillaMacroLink> VanillaMacroLinks() => Link.VanillaMacroLinks();

        /// Get the get the VanillaMacros that apply for this node, and it's corresponding macro slot
        ///
        /// If VanillaMacro is null, the slot should be unlinked
        public IEnumerable<(VanillaMacroLink, VanillaMacro)> VanillaMacroLinkBinding() {
            var vanillaMacros = VanillaMacros().ToList();

            // If we have more slots then macros we want to pad the remaining slots
            // with empty macros so they get removed and we don't leave the previous
            // macro linked.
            var paddingNeeded = Math.Max(0, Link.Slots.Count - vanillaMacros.Count);

            var firstIcon = vanillaMacros.FirstOrDefault()?.IconId ?? VanillaMacro.DefaultIconId;
            var emptyMacro = VanillaMacro.Empty with { IconId = firstIcon };
            var paddedVanillaMacros = vanillaMacros
                .Concat(Enumerable.Repeat<VanillaMacro>(emptyMacro, paddingNeeded));

            return VanillaMacroLinks().Zip(paddedVanillaMacros);
        }

        public bool HasLink => AlwaysLinked || Link.Slots.Count > 0;

        public bool SatisfiedBy(CurrentConditions conditions) {
            return AlwaysLinked || ConditionExpr.SatisfiedBy(conditions);
        }

        public Macro AddAndExpression() {
            ConditionExpr = this.ConditionExpr.AddAnd();
            return this;
        }

        public Macro AddAndExpression(ConditionExpr.And andExpr) {
            ConditionExpr = this.ConditionExpr.AddAnd(andExpr);
            return this;
        }

        public Macro DeleteAndExpression(int andIndex) {
            ConditionExpr = ConditionExpr.DeleteAnd(andIndex);
            return this;
        }

        public Macro AddCondition(int andIndex, ICondition condition) {
            ConditionExpr = ConditionExpr.UpdateAnd(
                andIndex,
                (and) => and.AddCondition(condition.WrapInDefaultOp())
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

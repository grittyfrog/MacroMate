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

namespace MacroMate.MacroTree;

public abstract partial class MateNode : TreeNode<MateNode> {
    public class Macro : MateNode {
        public uint IconId { get; set; } = 66001;
        public MacroLink Link = new();
        public bool LinkWithMacroChain = false;
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

                yield return new VanillaMacro(
                    IconId: this.IconId,
                    Title: title,
                    LineCount: (uint)chunk.CountNewlines(),
                    Lines: new(() => chunk),
                    IconRowId: 1
                );
            }
        }

        private IEnumerable<SeString> MaybeInsertMacroChainCommands(IEnumerable<SeString> lines) {
            if (!LinkWithMacroChain) { return lines; }
            if (!Env.PluginInterface.MacroChainPluginIsLoaded()) { return lines; }

            // We want a continuous "Snake" of right/down Links to be able to use Macro Chain. For example:
            //
            // - 1 2 3 is fine
            // - 1 3 5 is not fine (not continuous)
            // - 0 10 20 is fine (continuous downwards)
            // - 0 1 11 is fine (continous right/down)
            // - 3 2 1 is not fine (going left)
            //
            // If all of our links don't meet this criteria then there's no point and this setting should be ignored.

            var newLines = new List<SeString>();
            var lineChunks = lines.Chunk(14).Lookahead(2);
            var linkSlots = Link.Slots.Lookahead(2);
            foreach (var (lineChunkWindow, linkWindow) in lineChunks.ZipWithDefault(linkSlots)) {
                if (lineChunkWindow == null || lineChunkWindow.Count == 0) { break; } // No more lines to work on
                var lineChunk = lineChunkWindow[0];

                if (linkWindow == null) { // No more links, which means there's nothing to /nextmacro to
                    newLines.AddRange(lineChunk);
                    continue;
                }

                // We are at the end of the link pairs, which means there's nothing to /nextmacro to
                if (linkWindow.Count < 2) {
                    newLines.AddRange(lineChunk);
                    continue;
                }

                // We only need to add /nextmacro or /nextmacro down if the next macro block has any lines
                var nextLineChunks = lineChunkWindow.ElementAtOrDefault(1);
                if (nextLineChunks == null || nextLineChunks.Count() == 0) {
                    newLines.AddRange(lineChunk);
                    continue;
                }

                var (currentLink, nextLink) = (linkWindow[0], linkWindow[1]);
                if (currentLink + 1 == nextLink) { // Right Link
                    newLines.AddRange(lineChunk);
                    newLines.Add("/nextmacro");
                } else if (currentLink + 10 == nextLink) { // Down Link
                    newLines.AddRange(lineChunk);
                    newLines.Add("/nextmacro down");
                } else { // Non-continuous link -- abort!
                    return lines;
                }
            }

            return newLines;
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

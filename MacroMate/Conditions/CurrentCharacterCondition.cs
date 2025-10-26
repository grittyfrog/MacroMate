using System.Collections.Generic;
using System.Linq;

namespace MacroMate.Conditions;

public record class CurrentCharacterCondition(
    ulong ContentId
) : IValueCondition {

    public string ValueName {
        get {
            var character = Env.LocalPlayerCharactersManager.GetCharacter(ContentId);
            if (character == null) return "<unknown character>";
            return $"{character.Name}@{character.World.Name()}";
        }
    }

    public string NarrowName => ValueName;

    public CurrentCharacterCondition() : this(0) {}

    public static CurrentCharacterCondition? Current() {
        var character = Env.LocalPlayerCharactersManager.GetCurrentCharacter();
        if (character == null) return null;
        return new CurrentCharacterCondition(character.ContentId);
    }

    public bool SatisfiedBy(ICondition other) {
        if (other is CurrentCharacterCondition otherChar) {
            return this.ContentId == otherChar.ContentId;
        }
        return false;
    }

    public static IValueCondition.IFactory Factory = new ConditionFactory();
    public IValueCondition.IFactory FactoryRef => Factory;

    class ConditionFactory : IValueCondition.IFactory {
        public string ConditionName => "Current Character";
        public string ExpressionName => "CurrentCharacter";

        public IValueCondition? Current() => CurrentCharacterCondition.Current();
        public IValueCondition Default() => new CurrentCharacterCondition();
        public IValueCondition? FromConditions(CurrentConditions conditions) =>
            conditions.currentCharacter;

        public IEnumerable<IValueCondition> TopLevel() {
            return Env.LocalPlayerCharactersManager.GetAllCharacters()
                .OrderBy(c => c.Name)
                .Select(c => new CurrentCharacterCondition(c.ContentId) as IValueCondition);
        }
    }
}

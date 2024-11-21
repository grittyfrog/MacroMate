using System.Collections.Generic;
using System.Linq;
using Lumina.Excel.Sheets;
using MacroMate.Extensions.Dalamaud.Excel;

namespace MacroMate.Conditions;

public record class JobCondition(
    ExcelId<ClassJob> Job
) : IValueCondition {
    public string ValueName => Job.DisplayName();
    public string NarrowName => Job.DisplayName();

    public bool SatisfiedBy(ICondition other) {
        return this.Equals(other);
    }

    public JobCondition() : this(1) {}
    public JobCondition(uint jobId) : this(new ExcelId<ClassJob>(jobId)) {}

    public static IValueCondition.IFactory Factory => new ConditionFactory();
    public IValueCondition.IFactory FactoryRef => Factory;

    public static JobCondition? Current() {
        var player = Env.ClientState.LocalPlayer;
        if (player == null) { return null; }

        return new JobCondition(player.ClassJob.RowId);
    }

    class ConditionFactory : IValueCondition.IFactory {
        public string ConditionName => "Job";
        public string ExpressionName => "Job";

        public IValueCondition? Current() => JobCondition.Current();
        public IValueCondition Default() => new JobCondition();
        public IValueCondition? FromConditions(CurrentConditions conditions) => conditions.job;

        public IEnumerable<IValueCondition> TopLevel() {
            return Env.DataManager.GetExcelSheet<ClassJob>()!
                .Where(job => job.RowId != 0)
                .Select(job => new JobCondition(job.RowId) as IValueCondition);
        }
    }
}

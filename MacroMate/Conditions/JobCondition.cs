using System.Collections.Generic;
using System.Linq;
using Lumina.Excel.GeneratedSheets;
using MacroMate.Extensions.Dalamaud.Excel;

namespace MacroMate.Conditions;

public record class JobCondition(
    ExcelId<ClassJob> Job
) : ICondition {
    public string ValueName => Job.DisplayName();
    public string NarrowName => Job.DisplayName();

    public bool SatisfiedBy(ICondition other) => this.Equals(other);

    public JobCondition() : this(1) {}
    public JobCondition(uint jobId) : this(new ExcelId<ClassJob>(jobId)) {}

    public static ICondition.IFactory Factory => new ConditionFactory();
    public ICondition.IFactory FactoryRef => Factory;

    public static JobCondition? Current() {
        var player = Env.ClientState.LocalPlayer;
        if (player == null) { return null; }

        return new JobCondition(player.ClassJob.Id);
    }

    class ConditionFactory : ICondition.IFactory {
        public string ConditionName => "Job";

        public ICondition? Current() => JobCondition.Current();
        public ICondition Default() => new JobCondition();
        public ICondition? FromConditions(CurrentConditions conditions) => conditions.job;

        public IEnumerable<ICondition> TopLevel() {
            return Env.DataManager.GetExcelSheet<ClassJob>()!
                .Where(job => job.RowId != 0)
                .Select(job => new JobCondition(job.RowId) as ICondition);
        }
    }
}

using System.ComponentModel;

namespace Domain.Enums;

public enum PipelineStage
{
    [Description("1. Prospecting")]
    Prospecting = 0,
    [Description("2. Qualification")]
    Qualification = 1,
    [Description("3. NeedAnalysis")]
    NeedAnalysis = 2,
    [Description("4. Proposal")]
    Proposal = 3,
    [Description("5. Negotiation")]
    Negotiation = 4,
    [Description("6. DecisionMaking")]
    DecisionMaking = 5,
    [Description("7. Closed")]
    Closed = 6
}

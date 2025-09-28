namespace NonverbalBehaviorGenerator
{
    public enum NvbgRequestKind
    {
        NotSet = 0,

        ///<remarks><see cref="string.Empty"/> in NVBG</remarks>
        None = 1,

        ///<remarks>vrAgentSpeech in NVBG</remarks>
        AgentSpeech = 2,

        ///<remarks>vrBackchannel in NVBG</remarks>
        Backchannel = 3,

        ///<remarks>vrBCFeedback in NVBG</remarks>
        BackChannelFeedback = 4,

        ///<remarks>dialogue in NVBG</remarks>
        Dialogue = 5,

        ///<remarks>facs in NVBG</remarks>
        Facs = 6,

        ///<remarks>brNvbgFeedbackRuleTest in NVBG</remarks>
        FeedbackRuleTest = 7,

        ///<remarks>idleBehavior in NVBG</remarks>
        IdleBehavior = 8,

        ///<remarks>listen in NVBG</remarks>
        Listen = 9,

        ///<remarks>negotiation in NVBG</remarks>
        Negotiation = 10,

        ///<remarks>posture in NVBG</remarks>
        Posture = 11,

        ///<remarks>vrSpeech in NVBG</remarks>
        Speech = 12,
    }

}


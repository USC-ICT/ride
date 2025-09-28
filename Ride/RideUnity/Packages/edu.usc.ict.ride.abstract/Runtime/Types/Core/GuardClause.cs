using System;

namespace Ride
{
    public readonly struct GuardClause
    {
        public readonly int numArgs;
        public readonly Delegate clause;

        public GuardClause(int numArgs, Delegate clause)
        {
            this.numArgs = numArgs;
            this.clause = clause;
        }
    }
}

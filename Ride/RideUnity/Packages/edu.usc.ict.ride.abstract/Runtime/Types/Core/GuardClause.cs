using System;

namespace Ride
{
    /// <summary>Associates a guard predicate with the number of arguments it expects.</summary>
    /// <remarks>Used by behavior and state-machine transitions to determine whether a guarded operation may proceed.</remarks>
    public readonly struct GuardClause
    {
        /// <summary>The number of arguments expected by <see cref="clause"/>.</summary>
        public readonly int numArgs;
        /// <summary>The delegate evaluated as the guard predicate.</summary>
        public readonly Delegate clause;

        /// <summary>Initializes a guard-clause description.</summary>
        /// <param name="numArgs">The number of arguments expected by the delegate.</param>
        /// <param name="clause">The guard predicate delegate.</param>
        public GuardClause(int numArgs, Delegate clause)
        {
            this.numArgs = numArgs;
            this.clause = clause;
        }
    }
}

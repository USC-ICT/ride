using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Ride
{
    /// <summary>
    /// Provides static accessors for generating random numbers using Unity's random number generator.
    /// This is a wrapper around <a href="https://docs.unity3d.com/ScriptReference/Random.html">UnityEngine.Random</a>.
    /// </summary>
    public static class RideRandom
    {
        /// <summary>
        /// Returns a random point inside a unit sphere.
        /// </summary>
        public static RideVector3 insideUnitSphere => (RideVector3)UnityEngine.Random.insideUnitSphere;

        /// <summary>
        /// Returns a random float number between <paramref name="min"/> [inclusive] and <paramref name="max"/> [inclusive].
        /// </summary>
        public static float Range(float min, float max) => UnityEngine.Random.Range(min, max);

        /// <summary>
        /// Returns a random integer number between <paramref name="min"/> [inclusive] and <paramref name="max"/> [exclusive].
        /// </summary>
        public static int Range(int min, int max) => UnityEngine.Random.Range(min, max);

        /// <summary>
        /// Returns a random element from a non-empty enumerable.
        /// Throws an InvalidOperationException if the sequence is empty.
        /// </summary>
        public static T Element<T>(IEnumerable<T> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            int count = source.Count(); // May be O(n) for non-list
            if (count == 0)
                throw new InvalidOperationException("Cannot select a random element from an empty sequence.");

            int index = Range(0, count);
            return source.ElementAt(index);
        }
    }
}

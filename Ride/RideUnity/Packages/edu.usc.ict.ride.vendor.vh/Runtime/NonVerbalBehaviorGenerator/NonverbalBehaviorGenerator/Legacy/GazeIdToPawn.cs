#nullable disable


namespace NonverbalBehaviorGenerator.Legacy
{
    // store info related to a gaze id
    // gaze id is used for tracking specific gaze
    internal sealed class GazeIdToPawn
    {
        public string gazeId;
        public string pawnName;
        public float timePassed;
        public float rank;
        public float priority;

        public GazeIdToPawn(string gId, string pName, float tPassed, float ra, float pri)
        {
            gazeId = gId;
            pawnName = pName;
            timePassed = tPassed;
            rank = ra;
            priority = pri;
        }
    }
}

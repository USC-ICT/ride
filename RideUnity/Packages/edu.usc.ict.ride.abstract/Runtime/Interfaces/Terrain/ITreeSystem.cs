using System.Collections;
using System.Collections.Generic;

namespace Ride.Terrain
{
    public interface ITreeSystem : IRideSystem
    {
        void GetBestTree(Dictionary<string, string> searchParamDict);
        void DisplayString(string url);
    }
}

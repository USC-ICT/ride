using System.Collections;
using System.Collections.Generic;

namespace Ride.UI
{
    public interface IWorldLabelBillboard
    {
        string Text { get; set; }
        RideVector3 Position { get; set; }
        void SetActive(bool value);
    }
}

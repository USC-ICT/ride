using System.Collections;
using System.Collections.Generic;

namespace Ride.UI
{
    public interface ICoreUISystem
    {
        IExitPromptMenu CreateExitPromptMenu();
        IWorldLabelBillboard CreateWorldLabelBillboard();
        IBillboardIconToggle CreateBillboardIconToggle();
    }
}

using System;
using System.Collections;
using System.Collections.Generic;

namespace Ride.UI
{
    public interface IExitPromptMenu
    {
        event EventHandler onOpenPrompt;
        event EventHandler onClosePrompt;
        void SetPausable(bool pausable);
        void SetLevelToLoad(string levelToLoad);
        void OpenPrompt();
        bool IsPromptOpen();
        void OnNo();
    }
}

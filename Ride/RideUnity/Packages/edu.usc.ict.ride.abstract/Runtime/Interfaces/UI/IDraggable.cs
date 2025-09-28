using System;

namespace Ride.UI
{
    public class DragAndDropEventArgs : EventArgs
    {
        public RideVector3 position;

        public DragAndDropEventArgs() : base()
        {
            position = RideVector3.zero;
        }

        public DragAndDropEventArgs(RideVector3 pos) : base()
        {
            position = pos;
        }
    }

    /// <summary>
    /// A user interface widget that can be dragged and dropped
    /// </summary>
    public interface IDraggable : IUIElement
    {
        event EventHandler onDrag;
        event EventHandler onDrop;
        event EventHandler onDragging;

        bool IsDragging { get; set; }

        RideVector3 Position { get; set; }
    }
}

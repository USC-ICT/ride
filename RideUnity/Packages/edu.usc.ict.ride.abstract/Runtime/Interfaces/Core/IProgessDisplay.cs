namespace Ride.UI
{
    public interface IProgressDisplay
    {
        /// <summary>
        /// Update the current and max health
        /// </summary>
        /// <param name="curr"></param>
        /// <param name="max"></param>
        void SetHealth(float curr, float max);

        /// <summary>
        /// Set the health bar visible or invisible
        /// </summary>
        /// <param name="visible"></param>
        void SetVisible(bool visible);

        /// <summary>
        /// Set the health bar to enabled or disabled
        /// </summary>
        /// <param name="enable"></param>
        void SetEnable(bool enable);
    }
}

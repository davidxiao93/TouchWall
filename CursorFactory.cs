namespace TouchWall
{
    /// <summary>
    /// Factory class to enable seamless transformation between using the cursor and not using the cursor
    /// </summary>
    public static class CursorFactory
    {
        /// <summary>
        /// Method that checks the cursor status, and returns a ICursor object accordingly
        /// </summary>
        /// <returns>ICursor object</returns>
        public static ICursor GetICursor()
        {
            switch (TouchWallApp.MultiTouchMode)
            {
                case 0:
                    // interact with cursor
                    return UseMouse.GetUseCursor();
                default:
                    // some other mode
                    return NullMouse.GetNullCursor();
            }
        }
    }
}

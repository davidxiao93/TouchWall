namespace TouchWall
{
    public static class CursorFactory
    {
        public static ICursor GetICursor()
        {
            switch (TouchWallApp.MultiTouchMode)
            {
                case 0:
                    // interact with cursor
                    return UseMouse.GetUseCursor();
                    break;
                default:
                    // some other mode
                    return NullMouse.GetNullCursor();
                    break;
            }
        }
    }
}

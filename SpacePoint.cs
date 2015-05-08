using Microsoft.Kinect;

namespace TouchWall
{
    class SpacePoint
    {
        public int PointNear;
        public CameraSpacePoint Point3D;

        /// <summary>
        /// Custom data type that holds a CameraSpacePoint and an integer
        /// </summary>
        /// <param name="spacePoint"></param>
        /// <param name="initializer"></param>
        public SpacePoint(CameraSpacePoint spacePoint, int initializer)
        {
            PointNear = initializer;
            Point3D = spacePoint;
        }
    }
}

using Microsoft.Kinect;

namespace TouchWall
{
    class SpacePoint
    {
        public int PointNear;
        public CameraSpacePoint Point3D;

        public SpacePoint(CameraSpacePoint spacePoint, int initializer)
        {
            PointNear = initializer;
            Point3D = spacePoint;
        }
    }
}

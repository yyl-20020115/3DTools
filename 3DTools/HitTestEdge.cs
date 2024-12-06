using System.Windows;
using System.Windows.Media.Media3D;

namespace _3DTools;

public class HitTestEdge(Point3D p1, Point3D p2, Point uv1, Point uv2)
{
    public void Project(Matrix3D objectToViewportTransform)
	{
		var point3D1 = objectToViewportTransform.Transform(this._p1);
		var point3D2 = objectToViewportTransform.Transform(this._p2);
		this._p1Transformed = new Point(point3D1.X, point3D1.Y);
		this._p2Transformed = new Point(point3D2.X, point3D2.Y);
	}

	public Point3D _p1 = p1;

	public Point3D _p2 = p2;

	public Point _uv1 = uv1;

	public Point _uv2 = uv2;

	public Point _p1Transformed;

	public Point _p2Transformed;
}

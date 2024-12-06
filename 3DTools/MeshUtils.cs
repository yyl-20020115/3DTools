using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace _3DTools;

public static class MeshUtils
{
    public static PointCollection GenerateCylindricalTextureCoordinates(MeshGeometry3D mesh, Vector3D dir)
    {
        if (mesh == null)
        {
            return null;
        }
        Rect3D bounds = mesh.Bounds;
        int count = mesh.Positions.Count;
        PointCollection pointCollection = new PointCollection(count);
        IEnumerable<Point3D> enumerable = MeshUtils.TransformPoints(ref bounds, mesh.Positions, ref dir);
        foreach (Point3D point3D in enumerable)
        {
            pointCollection.Add(new Point(MeshUtils.GetUnitCircleCoordinate(-point3D.Z, point3D.X), 1.0 - MeshUtils.GetPlanarCoordinate(point3D.Y, bounds.Y, bounds.SizeY)));
        }
        return pointCollection;
    }

    public static PointCollection GenerateSphericalTextureCoordinates(MeshGeometry3D mesh, Vector3D dir)
    {
        if (mesh == null)
        {
            return null;
        }
        Rect3D bounds = mesh.Bounds;
        int count = mesh.Positions.Count;
        PointCollection pointCollection = new PointCollection(count);
        IEnumerable<Point3D> enumerable = MeshUtils.TransformPoints(ref bounds, mesh.Positions, ref dir);
        foreach (Point3D point3D in enumerable)
        {
            Vector3D vector3D = new Vector3D(point3D.X, point3D.Y, point3D.Z);
            MathUtils.TryNormalize(ref vector3D);
            pointCollection.Add(new Point(MeshUtils.GetUnitCircleCoordinate(-vector3D.Z, vector3D.X), 1.0 - (Math.Asin(vector3D.Y) / 3.141592653589793 + 0.5)));
        }
        return pointCollection;
    }

    public static PointCollection GeneratePlanarTextureCoordinates(MeshGeometry3D mesh, Vector3D dir)
    {
        if (mesh == null)
        {
            return null;
        }
        Rect3D bounds = mesh.Bounds;
        int count = mesh.Positions.Count;
        PointCollection pointCollection = new PointCollection(count);
        IEnumerable<Point3D> enumerable = MeshUtils.TransformPoints(ref bounds, mesh.Positions, ref dir);
        foreach (Point3D point3D in enumerable)
        {
            pointCollection.Add(new Point(MeshUtils.GetPlanarCoordinate(point3D.X, bounds.X, bounds.SizeX), MeshUtils.GetPlanarCoordinate(point3D.Z, bounds.Z, bounds.SizeZ)));
        }
        return pointCollection;
    }

    internal static double GetPlanarCoordinate(double end, double start, double width)
    {
        return (end - start) / width;
    }

    internal static double GetUnitCircleCoordinate(double y, double x)
    {
        return Math.Atan2(y, x) / 6.283185307179586 + 0.5;
    }

    internal static IEnumerable<Point3D> TransformPoints(ref Rect3D bounds, Point3DCollection points, ref Vector3D dir)
    {
        if (dir == MathUtils.YAxis)
        {
            return points;
        }
        Vector3D axisOfRotation = Vector3D.CrossProduct(dir, MathUtils.YAxis);
        double angleInDegrees = Vector3D.AngleBetween(dir, MathUtils.YAxis);
        Quaternion quaternion;
        if (axisOfRotation.X != 0.0 || axisOfRotation.Y != 0.0 || axisOfRotation.Z != 0.0)
        {
            quaternion = new Quaternion(axisOfRotation, angleInDegrees);
        }
        else
        {
            quaternion = new Quaternion(MathUtils.XAxis, angleInDegrees);
        }
        Vector3D vector = new Vector3D(bounds.X + bounds.SizeX / 2.0, bounds.Y + bounds.SizeY / 2.0, bounds.Z + bounds.SizeZ / 2.0);
        Matrix3D identity = Matrix3D.Identity;
        identity.Translate(-vector);
        identity.Rotate(quaternion);
        int count = points.Count;
        Point3D[] array = new Point3D[count];
        for (int i = 0; i < count; i++)
        {
            array[i] = identity.Transform(points[i]);
        }
        bounds = MathUtils.TransformBounds(bounds, identity);
        return array;
    }
}

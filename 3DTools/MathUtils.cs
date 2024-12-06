using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace _3DTools;

public static class MathUtils
{
    public static double GetAspectRatio(Size size) => size.Width / size.Height;

    public static double DegreesToRadians(double degrees) => degrees * 0.017453292519943295;

    private static Matrix3D GetViewMatrix(ProjectionCamera camera)
    {
        Vector3D vector3D = -camera.LookDirection;
        vector3D.Normalize();
        Vector3D vector3D2 = Vector3D.CrossProduct(camera.UpDirection, vector3D);
        vector3D2.Normalize();
        Vector3D vector = Vector3D.CrossProduct(vector3D, vector3D2);
        Vector3D vector2 = (Vector3D)camera.Position;
        double offsetX = -Vector3D.DotProduct(vector3D2, vector2);
        double offsetY = -Vector3D.DotProduct(vector, vector2);
        double offsetZ = -Vector3D.DotProduct(vector3D, vector2);
        return new Matrix3D(vector3D2.X, vector.X, vector3D.X, 0.0, vector3D2.Y, vector.Y, vector3D.Y, 0.0, vector3D2.Z, vector.Z, vector3D.Z, 0.0, offsetX, offsetY, offsetZ, 1.0);
    }

    public static Matrix3D GetViewMatrix(Camera camera)
    {
        ArgumentNullException.ThrowIfNull(camera);
        if (camera is ProjectionCamera projectionCamera)
        {
            return MathUtils.GetViewMatrix(projectionCamera);
        }
        if (camera is MatrixCamera matrixCamera)
        {
            return matrixCamera.ViewMatrix;
        }
        throw new ArgumentException($"Unsupported camera type '{camera.GetType().FullName}'.", nameof(camera));
    }

    private static Matrix3D GetProjectionMatrix(OrthographicCamera camera, double aspectRatio)
    {
        double width = camera.Width;
        double num = width / aspectRatio;
        double nearPlaneDistance = camera.NearPlaneDistance;
        double farPlaneDistance = camera.FarPlaneDistance;
        double num2 = 1.0 / (nearPlaneDistance - farPlaneDistance);
        double offsetZ = nearPlaneDistance * num2;
        return new Matrix3D(2.0 / width, 0.0, 0.0, 0.0, 0.0, 2.0 / num, 0.0, 0.0, 0.0, 0.0, num2, 0.0, 0.0, 0.0, offsetZ, 1.0);
    }

    private static Matrix3D GetProjectionMatrix(PerspectiveCamera camera, double aspectRatio)
    {
        double num = MathUtils.DegreesToRadians(camera.FieldOfView);
        double nearPlaneDistance = camera.NearPlaneDistance;
        double farPlaneDistance = camera.FarPlaneDistance;
        double num2 = 1.0 / Math.Tan(num / 2.0);
        double m = aspectRatio * num2;
        double num3 = (farPlaneDistance == double.PositiveInfinity) ? -1.0 : (farPlaneDistance / (nearPlaneDistance - farPlaneDistance));
        double offsetZ = nearPlaneDistance * num3;
        return new Matrix3D(num2, 0.0, 0.0, 0.0, 0.0, m, 0.0, 0.0, 0.0, 0.0, num3, -1.0, 0.0, 0.0, offsetZ, 0.0);
    }

    public static Matrix3D GetProjectionMatrix(Camera camera, double aspectRatio)
    {
        ArgumentNullException.ThrowIfNull(camera);
        if (camera is PerspectiveCamera perspectiveCamera)
        {
            return MathUtils.GetProjectionMatrix(perspectiveCamera, aspectRatio);
        }
        if (camera is OrthographicCamera orthographicCamera)
        {
            return MathUtils.GetProjectionMatrix(orthographicCamera, aspectRatio);
        }
        if (camera is MatrixCamera matrixCamera)
        {
            return matrixCamera.ProjectionMatrix;
        }
        throw new ArgumentException($"Unsupported camera type '{camera.GetType().FullName}'.", nameof(camera));
    }

    private static Matrix3D GetHomogeneousToViewportTransform(Rect viewport)
    {
        double num = viewport.Width / 2.0;
        double num2 = viewport.Height / 2.0;
        double offsetX = viewport.X + num;
        double offsetY = viewport.Y + num2;
        return new Matrix3D(num, 0.0, 0.0, 0.0, 0.0, -num2, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, offsetX, offsetY, 0.0, 1.0);
    }

    public static Matrix3D TryWorldToViewportTransform(Viewport3DVisual visual, out bool success)
    {
        success = false;
        Matrix3D result = MathUtils.TryWorldToCameraTransform(visual, out success);
        if (success)
        {
            result.Append(MathUtils.GetProjectionMatrix(visual.Camera, MathUtils.GetAspectRatio(visual.Viewport.Size)));
            result.Append(MathUtils.GetHomogeneousToViewportTransform(visual.Viewport));
            success = true;
        }
        return result;
    }

    public static Matrix3D TryWorldToCameraTransform(Viewport3DVisual visual, out bool success)
    {
        success = false;
        Matrix3D identity = Matrix3D.Identity;
        Camera camera = visual.Camera;
        if (camera == null)
        {
            return MathUtils.ZeroMatrix;
        }
        Rect viewport = visual.Viewport;
        if (viewport == Rect.Empty)
        {
            return MathUtils.ZeroMatrix;
        }
        Transform3D transform = camera.Transform;
        if (transform != null)
        {
            Matrix3D value = transform.Value;
            if (!value.HasInverse)
            {
                return MathUtils.ZeroMatrix;
            }
            value.Invert();
            identity.Append(value);
        }
        identity.Append(MathUtils.GetViewMatrix(camera));
        success = true;
        return identity;
    }

    private static Matrix3D GetWorldTransformationMatrix(DependencyObject visual, out Viewport3DVisual viewport)
    {
        Matrix3D identity = Matrix3D.Identity;
        viewport = null;
        if (visual is not Visual3D)
        {
            throw new ArgumentException("Must be of type Visual3D.", "visual");
        }
        while (visual != null && visual is ModelVisual3D)
        {
            Transform3D transform3D = (Transform3D)visual.GetValue(ModelVisual3D.TransformProperty);
            if (transform3D != null)
            {
                identity.Append(transform3D.Value);
            }
            visual = VisualTreeHelper.GetParent(visual);
        }
        viewport = (visual as Viewport3DVisual);
        if (viewport != null)
        {
            return identity;
        }
        if (visual != null)
        {
            throw new ApplicationException(string.Format("Unsupported type: '{0}'.  Expected tree of ModelVisual3Ds leading up to a Viewport3DVisual.", visual.GetType().FullName));
        }
        return MathUtils.ZeroMatrix;
    }

    public static Matrix3D TryTransformTo2DAncestor(DependencyObject visual, out Viewport3DVisual viewport, out bool success)
    {
        Matrix3D worldTransformationMatrix = MathUtils.GetWorldTransformationMatrix(visual, out viewport);
        worldTransformationMatrix.Append(MathUtils.TryWorldToViewportTransform(viewport, out success));
        if (!success)
        {
            return MathUtils.ZeroMatrix;
        }
        return worldTransformationMatrix;
    }

    public static Matrix3D TryTransformToCameraSpace(DependencyObject visual, out Viewport3DVisual viewport, out bool success)
    {
        Matrix3D worldTransformationMatrix = MathUtils.GetWorldTransformationMatrix(visual, out viewport);
        worldTransformationMatrix.Append(MathUtils.TryWorldToCameraTransform(viewport, out success));
        if (!success)
        {
            return MathUtils.ZeroMatrix;
        }
        return worldTransformationMatrix;
    }

    public static Rect3D TransformBounds(Rect3D bounds, Matrix3D transform)
    {
        double num = bounds.X;
        double num2 = bounds.Y;
        double num3 = bounds.Z;
        double num4 = bounds.X + bounds.SizeX;
        double num5 = bounds.Y + bounds.SizeY;
        double num6 = bounds.Z + bounds.SizeZ;
        Point3D[] array = new Point3D[]
        {
            new Point3D(num, num2, num3),
            new Point3D(num, num2, num6),
            new Point3D(num, num5, num3),
            new Point3D(num, num5, num6),
            new Point3D(num4, num2, num3),
            new Point3D(num4, num2, num6),
            new Point3D(num4, num5, num3),
            new Point3D(num4, num5, num6)
        };
        transform.Transform(array);
        Point3D point3D = array[0];
        num4 = (num = point3D.X);
        num5 = (num2 = point3D.Y);
        num6 = (num3 = point3D.Z);
        for (int i = 1; i < array.Length; i++)
        {
            point3D = array[i];
            num = Math.Min(num, point3D.X);
            num2 = Math.Min(num2, point3D.Y);
            num3 = Math.Min(num3, point3D.Z);
            num4 = Math.Max(num4, point3D.X);
            num5 = Math.Max(num5, point3D.Y);
            num6 = Math.Max(num6, point3D.Z);
        }
        return new Rect3D(num, num2, num3, num4 - num, num5 - num2, num6 - num3);
    }

    public static bool TryNormalize(ref Vector3D v)
    {
        double length = v.Length;
        if (length != 0.0)
        {
            v /= length;
            return true;
        }
        return false;
    }

    public static Point3D GetCenter(Rect3D box)
    {
        return new Point3D(box.X + box.SizeX / 2.0, box.Y + box.SizeY / 2.0, box.Z + box.SizeZ / 2.0);
    }

    public static readonly Matrix3D ZeroMatrix = new Matrix3D(0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0);

    public static readonly Vector3D XAxis = new Vector3D(1.0, 0.0, 0.0);

    public static readonly Vector3D YAxis = new Vector3D(0.0, 1.0, 0.0);

    public static readonly Vector3D ZAxis = new Vector3D(0.0, 0.0, 1.0);
}

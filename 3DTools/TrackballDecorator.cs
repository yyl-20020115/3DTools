using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace _3DTools;

public class TrackballDecorator : Viewport3DDecorator
{
    public TrackballDecorator()
    {
        this._transform = new Transform3DGroup();
        this._transform.Children.Add(this._scale);
        this._transform.Children.Add(new RotateTransform3D(this._rotation));
        this._eventSource = new Border();
        this._eventSource.Background = Brushes.Transparent;
        base.PreViewportChildren.Add(this._eventSource);
    }

        public Transform3D Transform => this._transform;

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        this._previousPosition2D = e.GetPosition(this);
        this._previousPosition3D = this.ProjectToTrackball(base.ActualWidth, base.ActualHeight, this._previousPosition2D);
        if (Mouse.Captured == null)
        {
            Mouse.Capture(this, CaptureMode.Element);
        }
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (base.IsMouseCaptured)
        {
            Mouse.Capture(this, CaptureMode.None);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (base.IsMouseCaptured)
        {
            Point position = e.GetPosition(this);
            if (position == this._previousPosition2D)
            {
                return;
            }
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.Track(position);
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                this.Zoom(position);
            }
            this._previousPosition2D = position;
            Viewport3D viewport3D = base.Viewport3D;
            if (viewport3D != null && viewport3D.Camera != null)
            {
                if (viewport3D.Camera.IsFrozen)
                {
                    viewport3D.Camera = viewport3D.Camera.Clone();
                }
                if (viewport3D.Camera.Transform != this._transform)
                {
                    viewport3D.Camera.Transform = this._transform;
                }
            }
        }
    }

    private void Track(Point currentPosition)
    {
        Vector3D vector3D = this.ProjectToTrackball(base.ActualWidth, base.ActualHeight, currentPosition);
        Vector3D axisOfRotation = Vector3D.CrossProduct(this._previousPosition3D, vector3D);
        double num = Vector3D.AngleBetween(this._previousPosition3D, vector3D);
        if (axisOfRotation.Length == 0.0)
        {
            return;
        }
        Quaternion right = new Quaternion(axisOfRotation, -num);
        Quaternion left = new Quaternion(this._rotation.Axis, this._rotation.Angle);
        left *= right;
        this._rotation.Axis = left.Axis;
        this._rotation.Angle = left.Angle;
        this._previousPosition3D = vector3D;
    }

    private Vector3D ProjectToTrackball(double width, double height, Point point)
    {
        double num = point.X / (width / 2.0);
        double num2 = point.Y / (height / 2.0);
        num -= 1.0;
        num2 = 1.0 - num2;
        double num3 = 1.0 - num * num - num2 * num2;
        double z = (num3 > 0.0) ? Math.Sqrt(num3) : 0.0;
        return new Vector3D(num, num2, z);
    }

    private void Zoom(Point currentPosition)
    {
        double num = currentPosition.Y - this._previousPosition2D.Y;
        double num2 = Math.Exp(num / 100.0);
        this._scale.ScaleX *= num2;
        this._scale.ScaleY *= num2;
        this._scale.ScaleZ *= num2;
    }

    private Point _previousPosition2D;

    private Vector3D _previousPosition3D = new Vector3D(0.0, 0.0, 1.0);

    private Transform3DGroup _transform;

    private ScaleTransform3D _scale = new ScaleTransform3D();

    private AxisAngleRotation3D _rotation = new AxisAngleRotation3D();

    private Border _eventSource;
}

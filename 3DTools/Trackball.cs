using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace _3DTools;

public class Trackball
{
    public Trackball()
    {
        this._transform = new Transform3DGroup();
        this._transform.Children.Add(this._scale);
        this._transform.Children.Add(new RotateTransform3D(this._rotation));
    }

    public Transform3D Transform
    {
        get
        {
            return this._transform;
        }
    }

    public FrameworkElement EventSource
    {
        get
        {
            return this._eventSource;
        }
        set
        {
            if (this._eventSource != null)
            {
                this._eventSource.MouseDown -= new MouseButtonEventHandler(this.OnMouseDown);
                this._eventSource.MouseUp -= new MouseButtonEventHandler(this.OnMouseUp);
                this._eventSource.MouseMove -= this.OnMouseMove;
            }
            this._eventSource = value;
            this._eventSource.MouseDown += new MouseButtonEventHandler(this.OnMouseDown);
            this._eventSource.MouseUp += new MouseButtonEventHandler(this.OnMouseUp);
            this._eventSource.MouseMove += this.OnMouseMove;
        }
    }

    private void OnMouseDown(object sender, MouseEventArgs e)
    {
        Mouse.Capture(this.EventSource, CaptureMode.Element);
        this._previousPosition2D = e.GetPosition(this.EventSource);
        this._previousPosition3D = this.ProjectToTrackball(this.EventSource.ActualWidth, this.EventSource.ActualHeight, this._previousPosition2D);
    }

    private void OnMouseUp(object sender, MouseEventArgs e)
    {
        Mouse.Capture(this.EventSource, CaptureMode.None);
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        Point position = e.GetPosition(this.EventSource);
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            this.Track(position);
        }
        else if (e.RightButton == MouseButtonState.Pressed)
        {
            this.Zoom(position);
        }
        this._previousPosition2D = position;
    }

    private void Track(Point currentPosition)
    {
        Vector3D vector3D = this.ProjectToTrackball(this.EventSource.ActualWidth, this.EventSource.ActualHeight, currentPosition);
        Vector3D axisOfRotation = Vector3D.CrossProduct(this._previousPosition3D, vector3D);
        double num = Vector3D.AngleBetween(this._previousPosition3D, vector3D);
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

    private FrameworkElement _eventSource;

    private Point _previousPosition2D;

    private Vector3D _previousPosition3D = new Vector3D(0.0, 0.0, 1.0);

    private Transform3DGroup _transform;

    private ScaleTransform3D _scale = new ScaleTransform3D();

    private AxisAngleRotation3D _rotation = new AxisAngleRotation3D();
}

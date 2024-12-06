using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace _3DTools;

public class ScreenSpaceLines3D : ModelVisual3D, IDisposable
{
    public ScreenSpaceLines3D()
    {
        this._mesh = new MeshGeometry3D();
        this._model = new GeometryModel3D
        {
            Geometry = this._mesh
        };
        this.SetColor(this.Color);
        base.Content = this._model;
        this.Points = [];
        CompositionTarget.Rendering += this.OnRender;
    }

    ~ScreenSpaceLines3D()
    {
        this.Dispose(false);
    }

    public Color Color
    {
        get => (Color)base.GetValue(ScreenSpaceLines3D.ColorProperty);
        set => base.SetValue(ScreenSpaceLines3D.ColorProperty, value);
    }

    public double Thickness
    {
        get => (double)base.GetValue(ScreenSpaceLines3D.ThicknessProperty);
        set => base.SetValue(ScreenSpaceLines3D.ThicknessProperty, value);
    }

    public Point3DCollection Points
    {
        get => (Point3DCollection)base.GetValue(ScreenSpaceLines3D.PointsProperty);
        set => base.SetValue(ScreenSpaceLines3D.PointsProperty, value);
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void MakeWireframe(Model3D model)
    {
        this.Points.Clear();
        if (model == null)
        {
            return;
        }
        Matrix3DStack matrix3DStack = new Matrix3DStack();
        matrix3DStack.Push(Matrix3D.Identity);
        this.WireframeHelper(model, matrix3DStack);
    }

    private void WireframeHelper(Model3D model, Matrix3DStack matrixStack)
    {
        Transform3D transform = model.Transform;
        if (transform != null && transform != Transform3D.Identity)
        {
            matrixStack.Prepend(model.Transform.Value);
        }
        try
        {
            if (model is Model3DGroup model3DGroup)
            {
                this.WireframeHelper(model3DGroup, matrixStack);
            }
            else
            {
                GeometryModel3D geometryModel3D = model as GeometryModel3D;
                if (geometryModel3D != null)
                {
                    this.WireframeHelper(geometryModel3D, matrixStack);
                }
            }
        }
        finally
        {
            if (transform != null && transform != Transform3D.Identity)
            {
                matrixStack.Pop();
            }
        }
    }

    private void WireframeHelper(Model3DGroup group, Matrix3DStack matrixStack)
    {
        foreach (Model3D model in group.Children)
        {
            this.WireframeHelper(model, matrixStack);
        }
    }

    private void WireframeHelper(GeometryModel3D model, Matrix3DStack matrixStack)
    {
        Geometry3D geometry = model.Geometry;
        MeshGeometry3D meshGeometry3D = geometry as MeshGeometry3D;
        if (meshGeometry3D != null)
        {
            Point3D[] array = new Point3D[meshGeometry3D.Positions.Count];
            meshGeometry3D.Positions.CopyTo(array, 0);
            matrixStack.Peek().Transform(array);
            Int32Collection triangleIndices = meshGeometry3D.TriangleIndices;
            if (triangleIndices.Count > 0)
            {
                int num = array.Length - 1;
                int i = 2;
                int count = triangleIndices.Count;
                while (i < count)
                {
                    int num2 = triangleIndices[i - 2];
                    int num3 = triangleIndices[i - 1];
                    int num4 = triangleIndices[i];
                    if (0 > num2 || num2 > num || 0 > num3 || num3 > num || 0 > num4)
                    {
                        return;
                    }
                    if (num4 > num)
                    {
                        return;
                    }
                    this.AddTriangle(array, num2, num3, num4);
                    i += 3;
                }
                return;
            }
            int j = 2;
            int num5 = array.Length;
            while (j < num5)
            {
                int i2 = j - 2;
                int i3 = j - 1;
                int i4 = j;
                this.AddTriangle(array, i2, i3, i4);
                j += 3;
            }
        }
    }

    private void AddTriangle(Point3D[] positions, int i0, int i1, int i2)
    {
        this.Points.Add(positions[i0]);
        this.Points.Add(positions[i1]);
        this.Points.Add(positions[i1]);
        this.Points.Add(positions[i2]);
        this.Points.Add(positions[i2]);
        this.Points.Add(positions[i0]);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            CompositionTarget.Rendering -= this.OnRender;
        }
    }

    private static void OnColorChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((ScreenSpaceLines3D)sender).SetColor((Color)args.NewValue);
    }

    private static void OnThicknessChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((ScreenSpaceLines3D)sender).GeometryDirty();
    }

    private static void OnPointsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        ((ScreenSpaceLines3D)sender).GeometryDirty();
    }

    private void OnRender(object sender, EventArgs e)
    {
        if (this.Points.Count == 0 && this._mesh.Positions.Count == 0)
        {
            return;
        }
        if (this.UpdateTransforms())
        {
            this.RebuildGeometry();
        }
    }

    private void GeometryDirty()
    {
        this._visualToScreen = MathUtils.ZeroMatrix;
    }

    private void RebuildGeometry()
    {
        double halfThickness = this.Thickness / 2.0;
        int num = this.Points.Count / 2;
        Point3DCollection point3DCollection = new Point3DCollection(num * 4);
        for (int i = 0; i < num; i++)
        {
            int num2 = i * 2;
            Point3D startPoint = this.Points[num2];
            Point3D endPoint = this.Points[num2 + 1];
            this.AddSegment(point3DCollection, startPoint, endPoint, halfThickness);
        }
        point3DCollection.Freeze();
        this._mesh.Positions = point3DCollection;
        Int32Collection int32Collection = new Int32Collection(this.Points.Count * 3);
        for (int j = 0; j < this.Points.Count / 2; j++)
        {
            int32Collection.Add(j * 4 + 2);
            int32Collection.Add(j * 4 + 1);
            int32Collection.Add(j * 4);
            int32Collection.Add(j * 4 + 2);
            int32Collection.Add(j * 4 + 3);
            int32Collection.Add(j * 4 + 1);
        }
        int32Collection.Freeze();
        this._mesh.TriangleIndices = int32Collection;
    }

    private void AddSegment(Point3DCollection positions, Point3D startPoint, Point3D endPoint, double halfThickness)
    {
        Vector3D vector3D = endPoint * this._visualToScreen - startPoint * this._visualToScreen;
        vector3D.Z = 0.0;
        vector3D.Normalize();
        Vector vector = new Vector(-vector3D.Y, vector3D.X);
        vector *= halfThickness;
        Point3D value;
        Point3D value2;
        this.Widen(startPoint, vector, out value, out value2);
        positions.Add(value);
        positions.Add(value2);
        this.Widen(endPoint, vector, out value, out value2);
        positions.Add(value);
        positions.Add(value2);
    }

    private void Widen(Point3D pIn, Vector delta, out Point3D pOut1, out Point3D pOut2)
    {
        Point4D point = (Point4D)pIn;
        Point4D point4D = point * this._visualToScreen;
        Point4D point2 = point4D;
        point4D.X += delta.X * point4D.W;
        point4D.Y += delta.Y * point4D.W;
        point2.X -= delta.X * point2.W;
        point2.Y -= delta.Y * point2.W;
        point4D *= this._screenToVisual;
        point2 *= this._screenToVisual;
        pOut1 = new Point3D(point4D.X / point4D.W, point4D.Y / point4D.W, point4D.Z / point4D.W);
        pOut2 = new Point3D(point2.X / point2.W, point2.Y / point2.W, point2.Z / point2.W);
    }

    private bool UpdateTransforms()
    {
        Viewport3DVisual viewport3DVisual;
        bool flag;
        Matrix3D matrix3D = MathUtils.TryTransformTo2DAncestor(this, out viewport3DVisual, out flag);
        if (!flag || !matrix3D.HasInverse)
        {
            this._mesh.Positions = null;
            return false;
        }
        if (matrix3D == this._visualToScreen)
        {
            return false;
        }
        this._visualToScreen = (this._screenToVisual = matrix3D);
        this._screenToVisual.Invert();
        return true;
    }

    private void SetColor(Color color)
    {
        MaterialGroup materialGroup = new MaterialGroup();
        materialGroup.Children.Add(new DiffuseMaterial(new SolidColorBrush(Colors.Black)));
        materialGroup.Children.Add(new EmissiveMaterial(new SolidColorBrush(color)));
        materialGroup.Freeze();
        this._model.Material = materialGroup;
        this._model.BackMaterial = materialGroup;
    }

    public static readonly DependencyProperty ColorProperty = DependencyProperty.Register("Color", typeof(Color), typeof(ScreenSpaceLines3D), new PropertyMetadata(Colors.White, new PropertyChangedCallback(ScreenSpaceLines3D.OnColorChanged)));

    public static readonly DependencyProperty ThicknessProperty = DependencyProperty.Register("Thickness", typeof(double), typeof(ScreenSpaceLines3D), new PropertyMetadata(1.0, new PropertyChangedCallback(ScreenSpaceLines3D.OnThicknessChanged)));

    public static readonly DependencyProperty PointsProperty = DependencyProperty.Register("Points", typeof(Point3DCollection), typeof(ScreenSpaceLines3D), new PropertyMetadata(null, new PropertyChangedCallback(ScreenSpaceLines3D.OnPointsChanged)));

    private Matrix3D _visualToScreen;

    private Matrix3D _screenToVisual;

    private readonly GeometryModel3D _model;

    private readonly MeshGeometry3D _mesh;
}

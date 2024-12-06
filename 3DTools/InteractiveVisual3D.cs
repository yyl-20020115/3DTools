using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace _3DTools;

public class InteractiveVisual3D : ModelVisual3D
{
    public InteractiveVisual3D()
    {
        this.InternalVisualBrush = this.CreateVisualBrush();
        this._content = new GeometryModel3D();
        this.Content = this._content;
        this.GenerateMaterial();
    }

    static InteractiveVisual3D()
    {
        _defaultMaterialPropertyValue = new DiffuseMaterial();
        _defaultMaterialPropertyValue.SetValue(IsInteractiveMaterialProperty, true);
        _defaultMaterialPropertyValue.Freeze();
        MaterialProperty = DependencyProperty.Register("Material", typeof(Material), typeof(InteractiveVisual3D), new PropertyMetadata(_defaultMaterialPropertyValue, new PropertyChangedCallback(OnMaterialPropertyChanged)));
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        this._lastVisCorners = null;
    }

    internal List<HitTestEdge> GetVisualEdges(Point[] texCoordsOfInterest)
    {
        this._lastEdges = this.GrabValidEdges(texCoordsOfInterest);
        this._lastVisCorners = texCoordsOfInterest;
        return this._lastEdges;
    }

    private List<HitTestEdge> GrabValidEdges(Point[] tc)
    {
        List<HitTestEdge> list = [];
        Dictionary<Edge, EdgeInfo> dictionary = [];
        MeshGeometry3D meshGeometry3D = (MeshGeometry3D)this._content.Geometry;
        Point3DCollection positions = meshGeometry3D.Positions;
        PointCollection textureCoordinates = meshGeometry3D.TextureCoordinates;
        Int32Collection triangleIndices = meshGeometry3D.TriangleIndices;
        Transform3D transform = this._content.Transform;
        Matrix3D matrix3D = MathUtils.TryTransformToCameraSpace(this, out Viewport3DVisual viewport3DVisual, out bool flag);
        if (!flag)
        {
            return [];
        }
        if (transform != null)
        {
            matrix3D.Prepend(transform.Value);
        }
        Matrix3D matrix3D2 = MathUtils.TryTransformTo2DAncestor(this, out viewport3DVisual, out flag);
        if (!flag)
        {
            return [];
        }
        if (transform != null)
        {
            matrix3D2.Prepend(transform.Value);
        }
        bool flag2 = this._lastVisCorners != null;
        if (this._lastVisCorners != null)
        {
            for (int i = 0; i < tc.Length; i++)
            {
                if (tc[i] != this._lastVisCorners[i])
                {
                    flag2 = false;
                    break;
                }
            }
            if (this._lastMatrix3D != matrix3D2)
            {
                flag2 = false;
            }
        }
        if (flag2)
        {
            return this._lastEdges;
        }
        this._lastMatrix3D = matrix3D2;
        try
        {
            matrix3D.Invert();
        }
        catch (InvalidOperationException)
        {
            return new List<HitTestEdge>();
        }
        Point3D camPosObjSpace = matrix3D.Transform(new Point3D(0.0, 0.0, 0.0));
        Rect empty = Rect.Empty;
        for (int j = 0; j < tc.Length; j++)
        {
            empty.Union(tc[j]);
        }
        int[] array = new int[3];
        Point3D[] array2 = new Point3D[3];
        Point[] array3 = new Point[3];
        for (int k = 0; k < triangleIndices.Count; k += 3)
        {
            Rect empty2 = Rect.Empty;
            for (int l = 0; l < 3; l++)
            {
                array[l] = triangleIndices[k + l];
                array2[l] = positions[array[l]];
                array3[l] = textureCoordinates[array[l]];
                empty2.Union(array3[l]);
            }
            if (empty.IntersectsWith(empty2))
            {
                this.ProcessTriangle(array2, array3, tc, list, dictionary, camPosObjSpace);
            }
        }
        foreach (Edge key in dictionary.Keys)
        {
            EdgeInfo edgeInfo = dictionary[key];
            if (edgeInfo.hasFrontFace && edgeInfo.numSharing == 1)
            {
                this.HandleSilhouetteEdge(edgeInfo.uv1, edgeInfo.uv2, key._start, key._end, tc, list);
            }
        }
        for (int m = 0; m < list.Count; m++)
        {
            list[m].Project(matrix3D2);
        }
        return list;
    }

    private void ProcessTriangle(Point3D[] p, Point[] uv, Point[] tc, List<HitTestEdge> edgeList, Dictionary<Edge, EdgeInfo> adjInformation, Point3D camPosObjSpace)
    {
        Vector3D vector = Vector3D.CrossProduct(p[1] - p[0], p[2] - p[0]);
        Vector3D vector2 = camPosObjSpace - p[0];
        if (vector.X != 0.0 || vector.Y != 0.0 || vector.Z != 0.0)
        {
            double num = Vector3D.DotProduct(vector, vector2);
            if (num > 0.0)
            {
                this.ProcessTriangleEdges(p, uv, tc, PolygonSide.FRONT, edgeList, adjInformation);
                this.ProcessVisualBoundsIntersections(p, uv, tc, edgeList);
                return;
            }
            this.ProcessTriangleEdges(p, uv, tc, PolygonSide.BACK, edgeList, adjInformation);
        }
    }

    private void ProcessVisualBoundsIntersections(Point3D[] p, Point[] uv, Point[] tc, List<HitTestEdge> edgeList)
    {
        List<Point3D> list = [];
        List<Point> list2 = [];
        for (int i = 0; i < tc.Length; i++)
        {
            Point point = tc[i];
            Point point2 = tc[(i + 1) % tc.Length];
            list.Clear();
            list2.Clear();
            bool flag = false;
            for (int j = 0; j < uv.Length; j++)
            {
                Point point3 = uv[j];
                Point point4 = uv[(j + 1) % uv.Length];
                Point3D point3D = p[j];
                Point3D point3D2 = p[(j + 1) % p.Length];
                if (Math.Max(point.X, point2.X) >= Math.Min(point3.X, point4.X) && Math.Min(point.X, point2.X) <= Math.Max(point3.X, point4.X) && Math.Max(point.Y, point2.Y) >= Math.Min(point3.Y, point4.Y) && Math.Min(point.Y, point2.Y) <= Math.Max(point3.Y, point4.Y))
                {
                    Vector vector = point4 - point3;
                    double num = this.IntersectRayLine(point3, vector, point, point2, out bool flag2);
                    if (flag2)
                    {
                        this.HandleCoincidentLines(point, point2, point3D, point3D2, point3, point4, edgeList);
                        flag = true;
                        break;
                    }
                    if (num >= 0.0 && num <= 1.0)
                    {
                        Point point5 = point3 + vector * num;
                        Point3D item = point3D + (point3D2 - point3D) * num;
                        double length = (point - point2).Length;
                        if ((point5 - point).Length < length && (point5 - point2).Length < length)
                        {
                            list.Add(item);
                            list2.Add(point5);
                        }
                    }
                }
            }
            if (!flag)
            {
                if (list.Count >= 2)
                {
                    edgeList.Add(new HitTestEdge(list[0], list[1], list2[0], list2[1]));
                }
                else if (list.Count == 1)
                {
                    if (this.IsPointInTriangle(point, uv, p, out Point3D p2))
                    {
                        edgeList.Add(new HitTestEdge(list[0], p2, list2[0], point));
                    }
                    if (this.IsPointInTriangle(point2, uv, p, out p2))
                    {
                        edgeList.Add(new HitTestEdge(list[0], p2, list2[0], point2));
                    }
                }
                else if (this.IsPointInTriangle(point, uv, p, out Point3D p3) && this.IsPointInTriangle(point2, uv, p, out Point3D p4))
                {
                    edgeList.Add(new HitTestEdge(p3, p4, point, point2));
                }
            }
        }
    }

    private bool IsPointInTriangle(Point p, Point[] triUVVertices, Point3D[] tri3DVertices, out Point3D inters3DPoint)
    {
        inters3DPoint = default;
        double num = triUVVertices[0].X - triUVVertices[2].X;
        double num2 = triUVVertices[1].X - triUVVertices[2].X;
        double num3 = triUVVertices[2].X - p.X;
        double num4 = triUVVertices[0].Y - triUVVertices[2].Y;
        double num5 = triUVVertices[1].Y - triUVVertices[2].Y;
        double num6 = triUVVertices[2].Y - p.Y;
        double num7 = num * num5 - num2 * num4;
        if (num7 == 0.0)
        {
            return false;
        }
        double num8 = (num2 * num6 - num3 * num5) / num7;
        num7 = num2 * num4 - num * num5;
        if (num7 == 0.0)
        {
            return false;
        }
        double num9 = (num * num6 - num3 * num4) / num7;
        if (num8 < 0.0 || num8 > 1.0 || num9 < 0.0 || num9 > 1.0 || num8 + num9 > 1.0)
        {
            return false;
        }
        inters3DPoint = (Point3D)(num8 * (Vector3D)tri3DVertices[0] + num9 * (Vector3D)tri3DVertices[1] + (1.0 - num8 - num9) * (Vector3D)tri3DVertices[2]);
        return true;
    }

    private void HandleCoincidentLines(Point visUV1, Point visUV2, Point3D tri3D1, Point3D tri3D2, Point triUV1, Point triUV2, List<HitTestEdge> edgeList)
    {
        Point uv;
        Point3D p;
        Point uv2;
        Point3D p2;
        if (Math.Abs(visUV1.X - visUV2.X) > Math.Abs(visUV1.Y - visUV2.Y))
        {
            Point point;
            Point point2;
            if (visUV1.X <= visUV2.X)
            {
                point = visUV1;
                point2 = visUV2;
            }
            else
            {
                point = visUV2;
                point2 = visUV1;
            }
            Point point3;
            Point3D point3D;
            Point point4;
            Point3D point3D2;
            if (triUV1.X <= triUV2.X)
            {
                point3 = triUV1;
                point3D = tri3D1;
                point4 = triUV2;
                point3D2 = tri3D2;
            }
            else
            {
                point3 = triUV2;
                point3D = tri3D2;
                point4 = triUV1;
                point3D2 = tri3D1;
            }
            if (point.X < point3.X)
            {
                uv = point3;
                p = point3D;
            }
            else
            {
                uv = point;
                p = point3D + (point.X - point3.X) / (point4.X - point3.X) * (point3D2 - point3D);
            }
            if (point2.X > point4.X)
            {
                uv2 = point4;
                p2 = point3D2;
            }
            else
            {
                uv2 = point2;
                p2 = point3D + (point2.X - point3.X) / (point4.X - point3.X) * (point3D2 - point3D);
            }
        }
        else
        {
            Point point;
            Point point2;
            if (visUV1.Y <= visUV2.Y)
            {
                point = visUV1;
                point2 = visUV2;
            }
            else
            {
                point = visUV2;
                point2 = visUV1;
            }
            Point point3;
            Point3D point3D;
            Point point4;
            Point3D point3D2;
            if (triUV1.Y <= triUV2.Y)
            {
                point3 = triUV1;
                point3D = tri3D1;
                point4 = triUV2;
                point3D2 = tri3D2;
            }
            else
            {
                point3 = triUV2;
                point3D = tri3D2;
                point4 = triUV1;
                point3D2 = tri3D1;
            }
            if (point.Y < point3.Y)
            {
                uv = point3;
                p = point3D;
            }
            else
            {
                uv = point;
                p = point3D + (point.Y - point3.Y) / (point4.Y - point3.Y) * (point3D2 - point3D);
            }
            if (point2.Y > point4.Y)
            {
                uv2 = point4;
                p2 = point3D2;
            }
            else
            {
                uv2 = point2;
                p2 = point3D + (point2.Y - point3.Y) / (point4.Y - point3.Y) * (point3D2 - point3D);
            }
        }
        edgeList.Add(new HitTestEdge(p, p2, uv, uv2));
    }

    private double IntersectRayLine(Point o, Vector d, Point p1, Point p2, out bool coinc)
    {
        coinc = false;
        double num = p2.Y - p1.Y;
        double num2 = p2.X - p1.X;
        if (num2 == 0.0)
        {
            if (d.X == 0.0)
            {
                coinc = (o.X == p1.X);
                return -1.0;
            }
            return (p2.X - o.X) / d.X;
        }
        else
        {
            double num3 = (o.X - p1.X) * num / num2 - o.Y + p1.Y;
            double num4 = d.Y - d.X * num / num2;
            if (num4 == 0.0)
            {
                double num5 = -o.X * num / num2 + o.Y;
                double num6 = -p1.X * num / num2 + p1.Y;
                coinc = (num5 == num6);
                return -1.0;
            }
            return num3 / num4;
        }
    }

    private void ProcessTriangleEdges(Point3D[] p, Point[] uv, Point[] tc, PolygonSide polygonSide, List<HitTestEdge> edgeList, Dictionary<Edge, EdgeInfo> adjInformation)
    {
        for (int i = 0; i < p.Length; i++)
        {
            Point3D point3D = p[i];
            Point3D point3D2 = p[(i + 1) % p.Length];
            Edge key;
            Point uv2;
            Point uv3;
            if (point3D.X < point3D2.X || (point3D.X == point3D2.X && point3D.Y < point3D2.Y) || (point3D.X == point3D2.X && point3D.Y == point3D2.Y && point3D.Z < point3D.Z))
            {
                key = new Edge(point3D, point3D2);
                uv2 = uv[i];
                uv3 = uv[(i + 1) % p.Length];
            }
            else
            {
                key = new Edge(point3D2, point3D);
                uv3 = uv[i];
                uv2 = uv[(i + 1) % p.Length];
            }
            EdgeInfo edgeInfo;
            if (adjInformation.ContainsKey(key))
            {
                edgeInfo = adjInformation[key];
            }
            else
            {
                edgeInfo = new EdgeInfo();
                adjInformation[key] = edgeInfo;
            }
            edgeInfo.numSharing++;
            bool flag = edgeInfo.hasBackFace && edgeInfo.hasFrontFace;
            if (polygonSide == PolygonSide.FRONT)
            {
                edgeInfo.hasFrontFace = true;
                edgeInfo.uv1 = uv2;
                edgeInfo.uv2 = uv3;
            }
            else
            {
                edgeInfo.hasBackFace = true;
            }
            if (!flag && edgeInfo.hasBackFace && edgeInfo.hasFrontFace)
            {
                this.HandleSilhouetteEdge(edgeInfo.uv1, edgeInfo.uv2, key._start, key._end, tc, edgeList);
            }
        }
    }

    private void HandleSilhouetteEdge(Point uv1, Point uv2, Point3D p3D1, Point3D p3D2, Point[] bounds, List<HitTestEdge> edgeList)
    {
        List<Point3D> list = [];
        List<Point> list2 = [];
        Vector vector = uv2 - uv1;
        for (int i = 0; i < bounds.Length; i++)
        {
            Point point = bounds[i];
            Point point2 = bounds[(i + 1) % bounds.Length];
            if (Math.Max(point.X, point2.X) >= Math.Min(uv1.X, uv2.X) && Math.Min(point.X, point2.X) <= Math.Max(uv1.X, uv2.X) && Math.Max(point.Y, point2.Y) >= Math.Min(uv1.Y, uv2.Y) && Math.Min(point.Y, point2.Y) <= Math.Max(uv1.Y, uv2.Y))
            {
                double num = this.IntersectRayLine(uv1, vector, point, point2, out bool flag);
                if (flag)
                {
                    return;
                }
                if (num >= 0.0 && num <= 1.0)
                {
                    Point point3 = uv1 + vector * num;
                    Point3D item = p3D1 + (p3D2 - p3D1) * num;
                    double length = (point - point2).Length;
                    if ((point3 - point).Length < length && (point3 - point2).Length < length)
                    {
                        list.Add(item);
                        list2.Add(point3);
                    }
                }
            }
        }
        if (list.Count >= 2)
        {
            edgeList.Add(new HitTestEdge(list[0], list[1], list2[0], list2[1]));
            return;
        }
        if (list.Count == 1)
        {
            if (this.IsPointInPolygon(bounds, uv1))
            {
                edgeList.Add(new HitTestEdge(list[0], p3D1, list2[0], uv1));
            }
            if (this.IsPointInPolygon(bounds, uv2))
            {
                edgeList.Add(new HitTestEdge(list[0], p3D2, list2[0], uv2));
                return;
            }
        }
        else if (this.IsPointInPolygon(bounds, uv1) && this.IsPointInPolygon(bounds, uv2))
        {
            edgeList.Add(new HitTestEdge(p3D1, p3D2, uv1, uv2));
        }
    }

    private bool IsPointInPolygon(Point[] polygon, Point p)
    {
        bool flag = false;
        for (int i = 0; i < polygon.Length; i++)
        {
            double num = Vector.CrossProduct(polygon[(i + 1) % polygon.Length] - polygon[i], polygon[i] - p);
            bool flag2 = num > 0.0;
            if (i == 0)
            {
                flag = flag2;
            }
            else if (flag != flag2)
            {
                return false;
            }
        }
        return true;
    }

    private void GenerateMaterial()
    {
        this.InternalVisualBrush.Visual = null;
        this.InternalVisualBrush = this.CreateVisualBrush();
        Material material = this.Material.Clone();
        this._content.Material = material;
        this.InternalVisualBrush.Visual = this.InternalVisual;
        this.SwapInVisualBrush(material);
        if (this.IsBackVisible)
        {
            this._content.BackMaterial = material;
        }
    }

    private VisualBrush CreateVisualBrush()
    {
        VisualBrush visualBrush = new();
        RenderOptions.SetCachingHint(visualBrush, CachingHint.Cache);
        visualBrush.ViewportUnits = BrushMappingMode.Absolute;
        visualBrush.TileMode = TileMode.None;
        return visualBrush;
    }

    private void SwapInVisualBrush(Material material)
    {
        bool flag = false;
        Stack<Material> stack = new();
        stack.Push(material);
        while (stack.Count > 0)
        {
            Material material2 = stack.Pop();
            if (material2 is DiffuseMaterial diffuseMaterial)
            {
                if ((bool)diffuseMaterial.GetValue(IsInteractiveMaterialProperty))
                {
                    diffuseMaterial.Brush = this.InternalVisualBrush;
                    flag = true;
                }
            }
            else if (material2 is EmissiveMaterial emissiveMaterial)
            {
                if ((bool)emissiveMaterial.GetValue(IsInteractiveMaterialProperty))
                {
                    emissiveMaterial.Brush = this.InternalVisualBrush;
                    flag = true;
                }
            }
            else
            {
                if (material2 is not SpecularMaterial)
                {
                    if (material2 is MaterialGroup materialGroup)
                    {
                        using MaterialCollection.Enumerator enumerator = materialGroup.Children.GetEnumerator();
                        while (enumerator.MoveNext())
                        {
                            Material item = enumerator.Current;
                            stack.Push(item);
                        }
                        continue;
                    }
                    throw new ArgumentException("material needs to be either a DiffuseMaterial, EmissiveMaterial, SpecularMaterial or a MaterialGroup", "material");
                }
                SpecularMaterial specularMaterial = (SpecularMaterial)material2;
                if ((bool)specularMaterial.GetValue(IsInteractiveMaterialProperty))
                {
                    specularMaterial.Brush = this.InternalVisualBrush;
                    flag = true;
                }
            }
        }
        if (!flag)
        {
            throw new ArgumentException("material needs to contain at least one material that has the IsInteractiveMaterial attached property", "material");
        }
    }

            public Visual Visual
    {
        get => (Visual)base.GetValue(VisualProperty);
        set => base.SetValue(VisualProperty, value);
    }

    internal UIElement InternalVisual => this._internalVisual;

    private VisualBrush InternalVisualBrush
    {
        get => this._visualBrush;
        set => this._visualBrush = value;
    }

    internal static void OnVisualChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        InteractiveVisual3D interactiveVisual3D = (InteractiveVisual3D)sender;
        AdornerDecorator adornerDecorator = null;
        if (interactiveVisual3D.InternalVisual != null)
        {
            adornerDecorator = (AdornerDecorator)interactiveVisual3D.InternalVisual;
            if (adornerDecorator.Child is VisualDecorator)
            {
                VisualDecorator visualDecorator = (VisualDecorator)adornerDecorator.Child;
                visualDecorator.Content = null;
            }
        }
        if (adornerDecorator == null)
        {
            adornerDecorator = new AdornerDecorator();
        }
        UIElement child;
        if (interactiveVisual3D.Visual is UIElement element)
        {
            child = element;
        }
        else
        {
            child = new VisualDecorator
            {
                Content = interactiveVisual3D.Visual
            };
        }
        adornerDecorator.Child = null;
        adornerDecorator.Child = child;
        interactiveVisual3D._internalVisual = adornerDecorator;
        interactiveVisual3D.InternalVisualBrush.Visual = interactiveVisual3D.InternalVisual;
    }

    public bool IsBackVisible
    {
        get => (bool)base.GetValue(IsBackVisibleProperty);
        set => base.SetValue(IsBackVisibleProperty, value);
    }

    internal static void OnIsBackVisiblePropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        InteractiveVisual3D interactiveVisual3D = (InteractiveVisual3D)sender;
        if (interactiveVisual3D.IsBackVisible)
        {
            interactiveVisual3D._content.BackMaterial = interactiveVisual3D._content.Material;
            return;
        }
        interactiveVisual3D._content.BackMaterial = null;
    }

    public Material Material
    {
        get => (Material)base.GetValue(MaterialProperty);
        set => base.SetValue(MaterialProperty, value);
    }

    internal static void OnMaterialPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        InteractiveVisual3D interactiveVisual3D = (InteractiveVisual3D)sender;
        interactiveVisual3D.GenerateMaterial();
    }

    public Geometry3D Geometry
    {
        get => (Geometry3D)base.GetValue(GeometryProperty);
        set => base.SetValue(GeometryProperty, value);
    }

    internal static void OnGeometryChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        InteractiveVisual3D interactiveVisual3D = (InteractiveVisual3D)sender;
        interactiveVisual3D._content.Geometry = interactiveVisual3D.Geometry;
    }

    public static void SetIsInteractiveMaterial(UIElement element, bool value)
    {
        element.SetValue(IsInteractiveMaterialProperty, value);
    }

    public static bool GetIsInteractiveMaterial(UIElement element) => (bool)element.GetValue(IsInteractiveMaterialProperty);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public new Model3D Content
    {
        get
        {
            return base.Content;
        }
        set
        {
            base.Content = value;
        }
    }

    private static DependencyProperty VisualProperty = DependencyProperty.Register("Visual", typeof(Visual), typeof(InteractiveVisual3D), new PropertyMetadata(null, new PropertyChangedCallback(OnVisualChanged)));

    private static readonly DependencyProperty IsBackVisibleProperty = DependencyProperty.Register("IsBackVisible", typeof(bool), typeof(InteractiveVisual3D), new PropertyMetadata(false, new PropertyChangedCallback(OnIsBackVisiblePropertyChanged)));

    private static readonly DiffuseMaterial _defaultMaterialPropertyValue;

    public static readonly DependencyProperty MaterialProperty;

    public static readonly DependencyProperty GeometryProperty = DependencyProperty.Register("Geometry", typeof(Geometry3D), typeof(InteractiveVisual3D), new PropertyMetadata(null, new PropertyChangedCallback(OnGeometryChanged)));

    public static readonly DependencyProperty IsInteractiveMaterialProperty = DependencyProperty.RegisterAttached("IsInteractiveMaterial", typeof(bool), typeof(InteractiveVisual3D), new PropertyMetadata(false));

    internal readonly GeometryModel3D _content;

    private Point[] _lastVisCorners;

    private List<HitTestEdge> _lastEdges;

    private Matrix3D _lastMatrix3D;

    private UIElement _internalVisual;

    private VisualBrush _visualBrush;

    private struct Edge(Point3D s, Point3D e)
    {
        public Point3D _start = s;

        public Point3D _end = e;
    }

    private class EdgeInfo
    {
        public EdgeInfo()
        {
            this.hasFrontFace = (this.hasBackFace = false);
            this.numSharing = 0;
        }

        public bool hasFrontFace;

        public bool hasBackFace;

        public Point uv1;

        public Point uv2;

        public int numSharing;
    }

    private enum PolygonSide
    {
        FRONT,
        BACK
    }
}

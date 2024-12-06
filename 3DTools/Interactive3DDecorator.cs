using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace _3DTools;

public class Interactive3DDecorator : Viewport3DDecorator
{
    public Interactive3DDecorator()
    {
        base.ClipToBounds = true;
        this._offsetX = (this._offsetY = 0.0);
        this._scale = 1.0;
        this._hiddenVisTranslate = new TranslateTransform(this._offsetX, this._offsetY);
        this._hiddenVisScale = new ScaleTransform(this._scale, this._scale);
        this._hiddenVisTransform = new TransformGroup();
        this._hiddenVisTransform.Children.Add(this._hiddenVisScale);
        this._hiddenVisTransform.Children.Add(this._hiddenVisTranslate);
        this._hiddenVisual = new Decorator();
        this._hiddenVisual.Opacity = 0.0;
        this._hiddenVisual.RenderTransform = this._hiddenVisTransform;
        this._oldHiddenVisual = new Decorator();
        this._oldHiddenVisual.Opacity = 0.0;
        this._oldKeyboardFocusVisual = new Decorator();
        this._oldKeyboardFocusVisual.Opacity = 0.0;
        base.PostViewportChildren.Add(this._oldHiddenVisual);
        base.PostViewportChildren.Add(this._oldKeyboardFocusVisual);
        base.PostViewportChildren.Add(this._hiddenVisual);
        this._closestIntersectInfo = null;
        this._lastValidClosestIntersectInfo = null;
        base.AllowDrop = true;
    }

    protected override void MeasurePostViewportChildren(Size constraint)
    {
        var availableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
        foreach (UIElement uielement in base.PostViewportChildren)
        {
            uielement.Measure(availableSize);
        }
    }

    protected override void ArrangePostViewportChildren(Size arrangeSize)
    {
        foreach (UIElement uielement in base.PostViewportChildren)
        {
            uielement.Arrange(new Rect(uielement.DesiredSize));
        }
    }

    protected override void OnPreviewDragOver(DragEventArgs e)
    {
        base.OnPreviewDragOver(e);
        if (base.Viewport3D != null)
        {
            Point position = e.GetPosition(base.Viewport3D);
            this.ArrangeHiddenVisual(position, true);
        }
    }

    protected override void OnDragOver(DragEventArgs e)
    {
        base.OnDragOver(e);
        e.Effects = DragDropEffects.None;
        e.Handled = true;
    }

    protected override void OnDragEnter(DragEventArgs e)
    {
        base.OnDragEnter(e);
        e.Effects = DragDropEffects.None;
        e.Handled = true;
    }

    protected override void OnPreviewMouseMove(MouseEventArgs e)
    {
        base.OnPreviewMouseMove(e);
        if (this._isInPosition)
        {
            this._isInPosition = false;
            return;
        }
        if (base.Viewport3D != null)
        {
            bool flag = this.ArrangeHiddenVisual(e.GetPosition(base.Viewport3D), false);
            if (flag)
            {
                e.Handled = true;
                this._isInPosition = true;
                if (this.ContainsInk)
                {
                    base.InvalidateArrange();
                }
                Mouse.Synchronize();
            }
        }
    }

    private bool ArrangeHiddenVisual(Point mouseposition, bool scaleHiddenVisual)
    {
        bool result = false;
        Viewport3D viewport3D = base.Viewport3D;
        if (viewport3D != null)
        {
            PointHitTestParameters hitTestParameters = new PointHitTestParameters(mouseposition);
            this._closestIntersectInfo = null;
            this._mouseCaptureInHiddenVisual = this._hiddenVisual.IsMouseCaptureWithin;
            VisualTreeHelper.HitTest(viewport3D, new HitTestFilterCallback(this.InteractiveMV3DFilter), new HitTestResultCallback(this.HTResult), hitTestParameters);
            if (this._closestIntersectInfo == null && this._mouseCaptureInHiddenVisual && this._lastValidClosestIntersectInfo != null)
            {
                this.HandleMouseCaptureButOffMesh(this._lastValidClosestIntersectInfo.InteractiveModelVisual3DHit, mouseposition);
            }
            else if (this._closestIntersectInfo != null)
            {
                this._lastValidClosestIntersectInfo = this._closestIntersectInfo;
            }
            result = this.UpdateHiddenVisual(this._closestIntersectInfo, mouseposition, scaleHiddenVisual);
        }
        return result;
    }

    private void HandleMouseCaptureButOffMesh(InteractiveVisual3D imv3DHit, Point mousePos)
    {
        UIElement uielement = (UIElement)Mouse.Captured;
        Rect descendantBounds = VisualTreeHelper.GetDescendantBounds(uielement);
        GeneralTransform generalTransform = uielement.TransformToAncestor(this._hiddenVisual);
        Point[] array =
        [
            generalTransform.Transform(new Point(descendantBounds.Left, descendantBounds.Top)),
            generalTransform.Transform(new Point(descendantBounds.Right, descendantBounds.Top)),
            generalTransform.Transform(new Point(descendantBounds.Right, descendantBounds.Bottom)),
            generalTransform.Transform(new Point(descendantBounds.Left, descendantBounds.Bottom))
        ];
        Point[] array2 = new Point[4];
        for (int i = 0; i < array.Length; i++)
        {
            array2[i] = VisualCoordsToTextureCoords(array[i], this._hiddenVisual);
        }
        List<HitTestEdge> visualEdges = imv3DHit.GetVisualEdges(array2);
        if (this.Debug)
        {
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(this);
            if (this._DEBUGadorner == null)
            {
                this._DEBUGadorner = new DebugEdgesAdorner(this, visualEdges);
                adornerLayer.Add(this._DEBUGadorner);
            }
            else
            {
                adornerLayer.Remove(this._DEBUGadorner);
                this._DEBUGadorner = new DebugEdgesAdorner(this, visualEdges);
                adornerLayer.Add(this._DEBUGadorner);
            }
        }
        this.FindClosestIntersection(mousePos, visualEdges, imv3DHit);
    }

    private void FindClosestIntersection(Point mousePos, List<HitTestEdge> edges, InteractiveVisual3D imv3DHit)
    {
        double num = double.MaxValue;
        Point uv = default;
        for (int i = 0; i < edges.Count; i++)
        {
            Vector vector = mousePos - edges[i]._p1Transformed;
            Vector vector2 = edges[i]._p2Transformed - edges[i]._p1Transformed;
            double num2 = vector2 * vector2;
            Point point;
            double length;
            if (num2 == 0.0)
            {
                point = edges[i]._p1Transformed;
                length = vector.Length;
            }
            else
            {
                double num3 = vector2 * vector;
                if (num3 < 0.0)
                {
                    point = edges[i]._p1Transformed;
                }
                else if (num3 > num2)
                {
                    point = edges[i]._p2Transformed;
                }
                else
                {
                    point = edges[i]._p1Transformed + num3 / num2 * vector2;
                }
                length = (mousePos - point).Length;
            }
            if (length < num)
            {
                num = length;
                if (num2 != 0.0)
                {
                    uv = (point - edges[i]._p1Transformed).Length / Math.Sqrt(num2) * (edges[i]._uv2 - edges[i]._uv1) + edges[i]._uv1;
                }
                else
                {
                    uv = edges[i]._uv1;
                }
            }
        }
        if (num != 1.7976931348623157E+308)
        {
            UIElement uielement = (UIElement)Mouse.Captured;
            UIElement internalVisual = imv3DHit.InternalVisual;
            Rect descendantBounds = VisualTreeHelper.GetDescendantBounds(uielement);
            Point point2 = TextureCoordsToVisualCoords(uv, internalVisual);
            Point point3 = internalVisual.TransformToDescendant(uielement).Transform(point2);
            if (point3.X <= descendantBounds.Left + 1.0)
            {
                point3.X -= 2.0;
            }
            if (point3.Y <= descendantBounds.Top + 1.0)
            {
                point3.Y -= 2.0;
            }
            if (point3.X >= descendantBounds.Right - 1.0)
            {
                point3.X += 2.0;
            }
            if (point3.Y >= descendantBounds.Bottom - 1.0)
            {
                point3.Y += 2.0;
            }
            Point pt = uielement.TransformToAncestor(internalVisual).Transform(point3);
            this._closestIntersectInfo = new ClosestIntersectionInfo(VisualCoordsToTextureCoords(pt, internalVisual), imv3DHit.InternalVisual, imv3DHit);
        }
    }

    public HitTestFilterBehavior InteractiveMV3DFilter(DependencyObject o)
    {
        HitTestFilterBehavior result = HitTestFilterBehavior.Continue;
        if (o is Visual3D && this._mouseCaptureInHiddenVisual)
        {
            if (o is InteractiveVisual3D)
            {
                InteractiveVisual3D interactiveVisual3D = (InteractiveVisual3D)o;
                if (interactiveVisual3D.InternalVisual != this._hiddenVisual.Child)
                {
                    result = HitTestFilterBehavior.ContinueSkipSelf;
                }
            }
            else
            {
                result = HitTestFilterBehavior.ContinueSkipSelf;
            }
        }
        return result;
    }

    private bool UpdateHiddenVisual(ClosestIntersectionInfo isectInfo, Point mousePos, bool scaleHiddenVisual)
    {
        bool flag = false;
        double newOffsetX;
        double newOffsetY;
        if (isectInfo != null)
        {
            UIElement uielementHit = isectInfo.UIElementHit;
            if (this._hiddenVisual.Child != uielementHit)
            {
                UIElement child = this._hiddenVisual.Child;
                if (this._oldHiddenVisual.Child == uielementHit)
                {
                    this._oldHiddenVisual.Child = null;
                }
                if (this._oldKeyboardFocusVisual.Child == uielementHit)
                {
                    this._oldKeyboardFocusVisual.Child = null;
                }
                if (this._oldHiddenVisual.Child == child)
                {
                    this._oldHiddenVisual.Child = null;
                }
                if (this._oldKeyboardFocusVisual.Child == child)
                {
                    this._oldKeyboardFocusVisual.Child = null;
                }
                Decorator decorator;
                if (child != null && child.IsKeyboardFocusWithin)
                {
                    decorator = this._oldKeyboardFocusVisual;
                }
                else
                {
                    decorator = this._oldHiddenVisual;
                }
                this._hiddenVisual.Child = uielementHit;
                decorator.Child = child;
                flag = true;
            }
            Point point = TextureCoordsToVisualCoords(isectInfo.PointHit, this._hiddenVisual);
            newOffsetX = mousePos.X - point.X;
            newOffsetY = mousePos.Y - point.Y;
        }
        else
        {
            newOffsetX = base.ActualWidth + 1.0;
            newOffsetY = base.ActualHeight + 1.0;
        }
        double newScale;
        if (scaleHiddenVisual)
        {
            newScale = Math.Max(base.Viewport3D.RenderSize.Width, base.Viewport3D.RenderSize.Height);
        }
        else
        {
            newScale = 1.0;
        }
        return flag | this.PositionHiddenVisual(newOffsetX, newOffsetY, newScale, mousePos);
    }

    private bool PositionHiddenVisual(double newOffsetX, double newOffsetY, double newScale, Point mousePosition)
    {
        bool result = false;
        if (newOffsetX != this._offsetX || newOffsetY != this._offsetY || this._scale != newScale)
        {
            this._offsetX = newOffsetX;
            this._offsetY = newOffsetY;
            this._scale = newScale;
            this._hiddenVisTranslate.X = this._scale * (this._offsetX - mousePosition.X) + mousePosition.X;
            this._hiddenVisTranslate.Y = this._scale * (this._offsetY - mousePosition.Y) + mousePosition.Y;
            this._hiddenVisScale.ScaleX = this._scale;
            this._hiddenVisScale.ScaleY = this._scale;
            result = true;
        }
        return result;
    }

    private static Point TextureCoordsToVisualCoords(Point uv, UIElement uiElem)
    {
        Rect descendantBounds = VisualTreeHelper.GetDescendantBounds(uiElem);
        return new Point(uv.X * descendantBounds.Width + descendantBounds.Left, uv.Y * descendantBounds.Height + descendantBounds.Top);
    }

    private static Point VisualCoordsToTextureCoords(Point pt, UIElement uiElem)
    {
        Rect descendantBounds = VisualTreeHelper.GetDescendantBounds(uiElem);
        return new Point((pt.X - descendantBounds.Left) / (descendantBounds.Right - descendantBounds.Left), (pt.Y - descendantBounds.Top) / (descendantBounds.Bottom - descendantBounds.Top));
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo info)
    {
        base.OnRenderSizeChanged(info);
        TranslateTransform renderTransform = new TranslateTransform(info.NewSize.Width + 1.0, 0.0);
        this._oldKeyboardFocusVisual.RenderTransform = renderTransform;
        this._oldHiddenVisual.RenderTransform = renderTransform;
    }

    private HitTestResultBehavior HTResult(HitTestResult rawresult)
    {
        RayHitTestResult rayHitTestResult = rawresult as RayHitTestResult;
        HitTestResultBehavior result = HitTestResultBehavior.Continue;
        if (rayHitTestResult != null)
        {
            this._closestIntersectInfo = this.GetIntersectionInfo(rayHitTestResult);
            result = HitTestResultBehavior.Stop;
        }
        return result;
    }

    private ClosestIntersectionInfo GetIntersectionInfo(RayHitTestResult rayHitResult)
    {
        ClosestIntersectionInfo result = null;
        if (rayHitResult is RayMeshGeometry3DHitTestResult rayMeshGeometry3DHitTestResult)
        {
            if (rayMeshGeometry3DHitTestResult.VisualHit is InteractiveVisual3D interactiveVisual3D)
            {
                MeshGeometry3D meshHit = rayMeshGeometry3DHitTestResult.MeshHit;
                UIElement internalVisual = interactiveVisual3D.InternalVisual;
                if (internalVisual != null)
                {
                    double vertexWeight = rayMeshGeometry3DHitTestResult.VertexWeight1;
                    double vertexWeight2 = rayMeshGeometry3DHitTestResult.VertexWeight2;
                    double vertexWeight3 = rayMeshGeometry3DHitTestResult.VertexWeight3;
                    int vertexIndex = rayMeshGeometry3DHitTestResult.VertexIndex1;
                    int vertexIndex2 = rayMeshGeometry3DHitTestResult.VertexIndex2;
                    int vertexIndex3 = rayMeshGeometry3DHitTestResult.VertexIndex3;
                    if (meshHit.TextureCoordinates != null && vertexIndex < meshHit.TextureCoordinates.Count && vertexIndex2 < meshHit.TextureCoordinates.Count && vertexIndex3 < meshHit.TextureCoordinates.Count)
                    {
                        Point point = meshHit.TextureCoordinates[vertexIndex];
                        Point point2 = meshHit.TextureCoordinates[vertexIndex2];
                        Point point3 = meshHit.TextureCoordinates[vertexIndex3];
                        Point p = new Point(point.X * vertexWeight + point2.X * vertexWeight2 + point3.X * vertexWeight3, point.Y * vertexWeight + point2.Y * vertexWeight2 + point3.Y * vertexWeight3);
                        result = new ClosestIntersectionInfo(p, internalVisual, interactiveVisual3D);
                    }
                }
            }
        }
        return result;
    }

    public bool Debug
    {
        get
        {
            return (bool)base.GetValue(DebugProperty);
        }
        set
        {
            base.SetValue(DebugProperty, value);
        }
    }

    internal static void OnDebugPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        Interactive3DDecorator interactive3DDecorator = (Interactive3DDecorator)sender;
        if ((bool)e.NewValue)
        {
            interactive3DDecorator._hiddenVisual.Opacity = 0.2;
            return;
        }
        interactive3DDecorator._hiddenVisual.Opacity = 0.0;
        if (interactive3DDecorator._DEBUGadorner != null)
        {
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(interactive3DDecorator);
            adornerLayer.Remove(interactive3DDecorator._DEBUGadorner);
            interactive3DDecorator._DEBUGadorner = null;
        }
    }

    public bool ContainsInk
    {
        get => (bool)GetValue(ContainsInkProperty);
        set => SetValue(ContainsInkProperty, value);
    }

    private const double BUFFER_SIZE = 2.0;

    public static readonly DependencyProperty DebugProperty = DependencyProperty.Register("Debug", typeof(bool), typeof(Interactive3DDecorator), new PropertyMetadata(false, new PropertyChangedCallback(Interactive3DDecorator.OnDebugPropertyChanged)));

    public static readonly DependencyProperty ContainsInkProperty = DependencyProperty.Register("ContainsInk", typeof(bool), typeof(Interactive3DDecorator), new PropertyMetadata(false));

    private Decorator _hiddenVisual;

    private Decorator _oldHiddenVisual;

    private Decorator _oldKeyboardFocusVisual;

    private TranslateTransform _hiddenVisTranslate;

    private ScaleTransform _hiddenVisScale;

    private TransformGroup _hiddenVisTransform;

    private bool _mouseCaptureInHiddenVisual;

    private double _offsetX;

    private double _offsetY;

    private double _scale;

    private ClosestIntersectionInfo _closestIntersectInfo;

    private ClosestIntersectionInfo _lastValidClosestIntersectInfo;

    private DebugEdgesAdorner _DEBUGadorner;

    private bool _isInPosition;

    public class DebugEdgesAdorner(UIElement adornedElement, List<HitTestEdge> edges) : Adorner(adornedElement)
    {
        protected override void OnRender(DrawingContext drawingContext)
        {
            Pen pen = new(new SolidColorBrush(Colors.Navy), 1.5);
            for (int i = 0; i < this._edges.Count; i++)
            {
                drawingContext.DrawLine(pen, this._edges[i]._p1Transformed, this._edges[i]._p2Transformed);
            }
        }

        private readonly List<HitTestEdge> _edges = edges;
    }

    private class ClosestIntersectionInfo(Point p, UIElement v, InteractiveVisual3D iv3D)
    {
        public Point PointHit
        {
            get => p;
            set => p = value;
        }

        public UIElement UIElementHit
        {
            get => v;
            set => v = value;
        }
        public InteractiveVisual3D InteractiveModelVisual3DHit
        {
            get => iv3D;
            set => iv3D = value;
        }
    }
}

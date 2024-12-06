using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace _3DTools;

[ContentProperty("Content")]
public abstract class Viewport3DDecorator : FrameworkElement, IAddChild
{
    public Viewport3DDecorator()
    {
        this._preViewportChildren = new UIElementCollection(this, this);
        this._postViewportChildren = new UIElementCollection(this, this);
        this._content = null;
    }

    public UIElement Content
    {
        get => this._content;
        set
        {
            if (value is not System.Windows.Controls.Viewport3D && value is not Viewport3DDecorator)
            {
                throw new ArgumentException("Not a valid child type", nameof(value));
            }
            if (this._content != value)
            {
                UIElement content = this._content;
                base.RemoveVisualChild(content);
                base.RemoveLogicalChild(content);
                this._content = value;
                base.AddLogicalChild(value);
                base.AddVisualChild(value);
                this.OnViewport3DDecoratorContentChange(content, value);
                this.BindToContentsWidthHeight(value);
                base.InvalidateMeasure();
            }
        }
    }

    private void BindToContentsWidthHeight(UIElement newContent)
    {
        Binding binding = new Binding("Width");
        binding.Mode = BindingMode.OneWay;
        Binding binding2 = new Binding("Height");
        binding2.Mode = BindingMode.OneWay;
        binding.Source = newContent;
        binding2.Source = newContent;
        BindingOperations.SetBinding(this, FrameworkElement.WidthProperty, binding);
        BindingOperations.SetBinding(this, FrameworkElement.HeightProperty, binding2);
        Binding binding3 = new Binding("MaxWidth");
        binding3.Mode = BindingMode.OneWay;
        Binding binding4 = new Binding("MaxHeight");
        binding4.Mode = BindingMode.OneWay;
        binding3.Source = newContent;
        binding4.Source = newContent;
        BindingOperations.SetBinding(this, FrameworkElement.MaxWidthProperty, binding3);
        BindingOperations.SetBinding(this, FrameworkElement.MaxHeightProperty, binding4);
        Binding binding5 = new Binding("MinWidth");
        binding5.Mode = BindingMode.OneWay;
        Binding binding6 = new Binding("MinHeight");
        binding6.Mode = BindingMode.OneWay;
        binding5.Source = newContent;
        binding6.Source = newContent;
        BindingOperations.SetBinding(this, FrameworkElement.MinWidthProperty, binding5);
        BindingOperations.SetBinding(this, FrameworkElement.MinHeightProperty, binding6);
    }

    protected virtual void OnViewport3DDecoratorContentChange(UIElement oldContent, UIElement newContent)
    {
    }

        public Viewport3D Viewport3D
    {
        get
        {
            Viewport3D result = null;
            Viewport3DDecorator viewport3DDecorator = this;
            UIElement content;
            for (; ; )
            {
                content = viewport3DDecorator.Content;
                if (content == null)
                {
                    return result;
                }
                if (content is Viewport3D)
                {
                    break;
                }
                viewport3DDecorator = (Viewport3DDecorator)content;
            }
            result = (Viewport3D)content;
            return result;
        }
    }

        protected UIElementCollection PreViewportChildren
    {
        get
        {
            return this._preViewportChildren;
        }
    }

        protected UIElementCollection PostViewportChildren
    {
        get
        {
            return this._postViewportChildren;
        }
    }

        protected override int VisualChildrenCount
    {
        get
        {
            int num = (this.Content == null) ? 0 : 1;
            return this.PreViewportChildren.Count + this.PostViewportChildren.Count + num;
        }
    }

    protected override Visual GetVisualChild(int index)
    {
        int num = index;
        if (index < this.PreViewportChildren.Count)
        {
            return this.PreViewportChildren[index];
        }
        index -= this.PreViewportChildren.Count;
        if (this.Content != null && index == 0)
        {
            return this.Content;
        }
        index -= ((this.Content == null) ? 0 : 1);
        if (index < this.PostViewportChildren.Count)
        {
            return this.PostViewportChildren[index];
        }
        throw new ArgumentOutOfRangeException(nameof(index), num, "Out of range visual requested");
    }

        protected override IEnumerator LogicalChildren
    {
        get
        {
            Visual[] array = new Visual[this.VisualChildrenCount];
            for (int i = 0; i < this.VisualChildrenCount; i++)
            {
                array[i] = this.GetVisualChild(i);
            }
            return array.GetEnumerator();
        }
    }

    protected override Size MeasureOverride(Size constraint)
    {
        Size result = default(Size);
        this.MeasurePreViewportChildren(constraint);
        if (this.Content != null)
        {
            this.Content.Measure(constraint);
            result = this.Content.DesiredSize;
        }
        this.MeasurePostViewportChildren(constraint);
        return result;
    }

    protected virtual void MeasurePreViewportChildren(Size constraint)
    {
        this.MeasureUIElementCollection(this.PreViewportChildren, constraint);
    }

    protected virtual void MeasurePostViewportChildren(Size constraint)
    {
        this.MeasureUIElementCollection(this.PostViewportChildren, constraint);
    }

    private void MeasureUIElementCollection(UIElementCollection collection, Size constraint)
    {
        foreach (UIElement uielement in collection)
        {
            uielement.Measure(constraint);
        }
    }

    protected override Size ArrangeOverride(Size arrangeSize)
    {
        this.ArrangePreViewportChildren(arrangeSize);
        if (this.Content != null)
        {
            this.Content.Arrange(new Rect(arrangeSize));
        }
        this.ArrangePostViewportChildren(arrangeSize);
        return arrangeSize;
    }

    protected virtual void ArrangePreViewportChildren(Size arrangeSize)
    {
        this.ArrangeUIElementCollection(this.PreViewportChildren, arrangeSize);
    }

    protected virtual void ArrangePostViewportChildren(Size arrangeSize)
    {
        this.ArrangeUIElementCollection(this.PostViewportChildren, arrangeSize);
    }

    private void ArrangeUIElementCollection(UIElementCollection collection, Size constraint)
    {
        foreach (UIElement uielement in collection)
        {
            uielement.Arrange(new Rect(constraint));
        }
    }

    void IAddChild.AddChild(object value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (this.Content != null)
        {
            throw new ArgumentException("Viewport3DDecorator can only have one child");
        }
        this.Content = (UIElement)value;
    }

    void IAddChild.AddText(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (!char.IsWhiteSpace(text[i]))
            {
                throw new ArgumentException("Non whitespace in add text", text);
            }
        }
    }

    private readonly UIElementCollection _preViewportChildren;

    private readonly UIElementCollection _postViewportChildren;

    private UIElement _content;
}

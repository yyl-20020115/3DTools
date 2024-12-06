using System;
using System.Collections;
using System.Windows;
using System.Windows.Media;

namespace _3DTools;

internal class VisualDecorator : FrameworkElement
{
    public VisualDecorator() => this._visual = null;

    public Visual Content
    {
        get => this._visual;
        set
        {
            if (this._visual != value)
            {
                var visual = this._visual;
                base.RemoveVisualChild(visual);
                base.RemoveLogicalChild(visual);
                this._visual = value;
                base.AddLogicalChild(value);
                base.AddVisualChild(value);
            }
        }
    }

    protected override int VisualChildrenCount => this.Content == null ? 0 : 1;

    protected override Visual GetVisualChild(int index) => index == 0 && this.Content != null
            ? this._visual
            : throw new ArgumentOutOfRangeException(nameof(index), index, "Out of range visual requested");

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

    private Visual _visual;
}

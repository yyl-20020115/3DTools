using System;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace _3DTools;

public abstract class MeshTextureCoordinateConverter : MeshConverter<PointCollection>
{
    public MeshTextureCoordinateConverter()
    {
    }

    public override object Convert(MeshGeometry3D mesh, object parameter)
    {
        string text = parameter as string;
        if (parameter != null && text == null)
        {
            throw new ArgumentException("Parameter must be a string.");
        }
        Vector3D dir = MathUtils.YAxis;
        if (text != null)
        {
            dir = Vector3D.Parse(text);
            MathUtils.TryNormalize(ref dir);
        }
        return this.Convert(mesh, dir);
    }

    public abstract object Convert(MeshGeometry3D mesh, Vector3D dir);
}

using System;
using System.Windows;
using System.Windows.Media.Media3D;

namespace EarthDemo;

public class SphereMeshGenerator
{
    private Point3D center = new();

    // Four public properties allow access to private fields.
    public int Slices { set; get; } = 64;

    public int Stacks { set; get; } = 32;

    public Point3D Center
    {
        set => this.center = value;
        get => this.center;
    }

    public double Radius { set; get; } = 1;

    // Get-only property generates MeshGeometry3D.
    //can not be replaced with method
    public MeshGeometry3D Geometry
    {
        get
        {
            // Create a MeshGeometry3D.
            var mesh = new MeshGeometry3D();

            // Fill the vertices, normals, and textures collections.
            for (int stack = 0; stack <= Stacks; stack++)
            {
                double phi = Math.PI / 2 - stack * Math.PI / Stacks;
                double y = Radius * Math.Sin(phi);
                double scale = -Radius * Math.Cos(phi);

                for (int slice = 0; slice <= Slices; slice++)
                {
                    double theta = slice * 2 * Math.PI / Slices;
                    double x = scale * Math.Sin(theta);
                    double z = scale * Math.Cos(theta);

                    Vector3D normal = new(x, y, z);
                    mesh.Normals.Add(normal);
                    mesh.Positions.Add(normal + Center);
                    mesh.TextureCoordinates.Add(new Point((double)slice / Slices,(double)stack / Stacks));
                }
            }

            // Fill the indices collection.
            for (int stack = 0; stack < Stacks; stack++)
            {
                int top = (stack + 0) * (Slices + 1);
                int bottom = (stack + 1) * (Slices + 1);

                for (int slice = 0; slice < Slices; slice++)
                {
                    if (stack != 0)
                    {
                        mesh.TriangleIndices.Add(top + slice);
                        mesh.TriangleIndices.Add(bottom + slice);
                        mesh.TriangleIndices.Add(top + slice + 1);
                    }

                    if (stack != Stacks - 1)
                    {
                        mesh.TriangleIndices.Add(top + slice + 1);
                        mesh.TriangleIndices.Add(bottom + slice);
                        mesh.TriangleIndices.Add(bottom + slice + 1);
                    }
                }
            }
            return mesh;
        }
    }
}

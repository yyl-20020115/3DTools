﻿using System.Windows.Media.Media3D;

namespace _3DTools;

public class SphericalTextureCoordinateGenerator : MeshTextureCoordinateConverter
{
    public override object Convert(MeshGeometry3D mesh, Vector3D dir) 
        => MeshUtils.GenerateSphericalTextureCoordinates(mesh, dir);
}

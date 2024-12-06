using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace _3DTools
{
		public partial class Trackport3D : UserControl
	{
				public Trackport3D()
		{
			this.InitializeComponent();
			this.Viewport.Children.Add(this.Wireframe);
			this.Camera.Transform = this._trackball.Transform;
			this.Headlight.Transform = this._trackball.Transform;
		}

				public void LoadModel(Stream fileStream)
		{
			this._model = (Model3D)XamlReader.Load(fileStream);
			this.SetupScene();
		}

								public Color HeadlightColor
		{
			get
			{
				return this.Headlight.Color;
			}
			set
			{
				this.Headlight.Color = value;
			}
		}

								public Color AmbientLightColor
		{
			get
			{
				return this.AmbientLight.Color;
			}
			set
			{
				this.AmbientLight.Color = value;
			}
		}

								public ViewMode ViewMode
		{
			get
			{
				return this._viewMode;
			}
			set
			{
				this._viewMode = value;
				this.SetupScene();
			}
		}

				private void SetupScene()
		{
			switch (this.ViewMode)
			{
			case ViewMode.Solid:
				this.Root.Content = this._model;
				this.Wireframe.Points.Clear();
				return;
			case ViewMode.Wireframe:
				this.Root.Content = null;
				this.Wireframe.MakeWireframe(this._model);
				return;
			default:
				return;
			}
		}

				private void OnLoaded(object sender, RoutedEventArgs e)
		{
			this._trackball.EventSource = this.CaptureBorder;
		}

				private Trackball _trackball = new Trackball();

				private readonly ScreenSpaceLines3D Wireframe = new ScreenSpaceLines3D();

				private ViewMode _viewMode;

				private Model3D _model;
	}
}

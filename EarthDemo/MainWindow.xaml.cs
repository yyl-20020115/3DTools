using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
namespace EarthDemo;

/// <summary>
/// Window1.xaml 的交互逻辑
/// </summary>
public partial class Window1 : Window
{
    public Window1()
    {
        InitializeComponent();
        CompositionTarget.Rendering += new EventHandler(CompositionTarget_Rendering);
    }
    private bool is_stopping = false;
    void CompositionTarget_Rendering(object sender, EventArgs e)
    {
        YRotate.Angle++;
        if (YRotate.Angle > 360)
            YRotate.Angle = 0;
        if (is_stopping)
        {
            CompositionTarget.Rendering -= new EventHandler(CompositionTarget_Rendering);
            is_stopping = true;
        }
    }
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            this.WindowStyle = WindowStyle.SingleBorderWindow;
            //menu.Background = new SolidColorBrush(SystemColors.ControlColor);
            //MenuItem menuitem = (MenuItem)menu.Items[0];
            //int count  = menuitem.Items.Count;
            //for (int i = 0; i < count; i++)
            //{
            //    MenuItem item = (MenuItem)menuitem.Items[i];
            //    item.Background = new SolidColorBrush(SystemColors.ControlColor);
            //}

        }
        else if (e.Key == Key.F1)
        {
            this.WindowStyle = WindowStyle.None;
            //menu.Background = Brushes.Transparent;
            //MenuItem menuitem = (MenuItem)menu.Items[0];
            //int count = menuitem.Items.Count;

            //for (int i = 0; i < count; i++)
            //{
            //    MenuItem item = (MenuItem)menuitem.Items[i];
            //    item.Background = Brushes.Transparent;
            //}
        }
        else if (e.Key == Key.S)
        {
            if (is_stopping)
            {
                CompositionTarget.Rendering += new EventHandler(CompositionTarget_Rendering);
                is_stopping = false;
            }
            else is_stopping = true;

            //if (isstop)
            //    rotatestory.Resume(this);
            //else
            //    rotatestory.Pause(this);

            //isstop = !isstop;
        }
    }

    private void Earthmodel_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        double z = Cam.Position.Z;

        if (z > 100)
        {
            z = 99;
            Cam.Position = new Point3D(0, 0, z);
            return;
        }
        if (z < 4)
        {
            z = 5;
            Cam.Position = new Point3D(0, 0, z);
            return;
        }
        z = z - (double)(e.Delta / 60);
        Cam.Position = new Point3D(0, 0, z);

        //if(e.Delta 
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        NameScope.SetNameScope(this, new NameScope());
        this.RegisterName("YRotate", YRotate);
        this.RegisterName("EarthOffset", EarthOffset);
        this.RegisterName("Cam", Cam);
    }

    private void Storyboard_Completed(object sender, EventArgs e)
    {
        Cam.Position = new Point3D(0, 0, 7);
    }

    private void Earthmodel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        //MessageBox.Show("EarthDemo");
    }

}

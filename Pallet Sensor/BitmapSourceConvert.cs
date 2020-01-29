using Emgu.CV.Structure;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

//Used to convert between Image<> and BitmapSource
//Source: http://www.emgu.com/wiki/index.php/WPF_in_CSharp

public static class BitmapSourceConvert
{
    [DllImport("gdi32")]
    private static extern int DeleteObject(IntPtr o);

    public static BitmapSource ToBitmapSource(Emgu.CV.Image<Hsv, Byte> image)
    {
        using (System.Drawing.Bitmap source = image.Bitmap)
        {
            IntPtr ptr = source.GetHbitmap();

            BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                ptr,
                IntPtr.Zero,
                Int32Rect.Empty,
                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

            DeleteObject(ptr);
            return bs;
        }
    }
}

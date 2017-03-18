using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace J3DModelViewer
{
    public static class ColorUtils
    {
        public static Vector3 HUEtoRGB(float h)
        {
            float r = Math.Abs(h * 6 - 3) - 1;
            float g = 2 - Math.Abs(h * 6 - 2);
            float b = 2 - Math.Abs(h * 6 - 4);

            return Vector3.Clamp(new Vector3(r, g, b), Vector3.Zero, Vector3.One);
        }

        public static Vector3 HSVtoRGB(Vector3 hsv)
        {
            Vector3 rgb = HUEtoRGB(hsv.X);

            Vector3 rgbMinusOne = rgb - Vector3.One;
            rgbMinusOne = Vector3.Multiply(rgbMinusOne, hsv.Y);
            rgbMinusOne += Vector3.One;

            return rgbMinusOne * hsv.Z;
        }
    }
}

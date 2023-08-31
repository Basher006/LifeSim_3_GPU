using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LifeSim_3_GPU
{
    public struct HSV
    {
        public double H;
        public double S;
        public double V;
    }
    internal class ColorConverter
    {

        public static byte[] MinusHalfValue(byte R, byte G, byte B)
        {
            var hsv = RGBToHSV(R, G, B);
            hsv[2] -= hsv[2] / 2;
            return HSVToRGB(hsv[0], hsv[1], hsv[2]);
        }

        public static MyColor HSVToRGB(HSV hsv)
        {
            double r, g, b;

            if (hsv.S == 0)
            {
                r = hsv.V;
                g = hsv.V;
                b = hsv.V;
            }
            else
            {
                int i;
                double f, p, q, t;

                if (hsv.H == 360)
                    hsv.H = 0;
                else
                    hsv.H = hsv.H / 60;

                i = (int)hsv.H;
                f = hsv.H - i;

                p = hsv.V * (1.0d - hsv.S);
                q = hsv.V * (1.0d - (hsv.S * f));
                t = hsv.V * (1.0d - (hsv.S * (1.0d - f)));

                switch (i)
                {
                    case 0:
                        r = hsv.V;
                        g = t;
                        b = p;
                        break;
                    case 1:
                        r = q;
                        g = hsv.V;
                        b = p;
                        break;
                    case 2:
                        r = p;
                        g = hsv.V;
                        b = t;
                        break;
                    case 3:
                        r = p;
                        g = q;
                        b = hsv.V;
                        break;
                    case 4:
                        r = t;
                        g = p;
                        b = hsv.V;
                        break;
                    default:
                        r = hsv.V;
                        g = p;
                        b = q;
                        break;
                }
            }
            return new MyColor((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }

        public static byte[] HSVToRGB(double H, double S, double V)
        { 
            double r, g, b;

            if (S == 0)
            {
                r = V;
                g = V;
                b = V;
            }
            else
            {
                int i;
                double f, p, q, t;

                if (H == 360)
                    H = 0;
                else
                    H = H / 60;

                i = (int) H;
                f = H - i;

                p = V* (1.0 - S);
                q = V* (1.0 - (S* f));
                t = V* (1.0 - (S* (1.0 - f)));

                switch (i)
                {
                    case 0:
                        r = V;
                        g = t;
                        b = p;
                        break;

                    case 1:
                        r = q;
                        g = V;
                        b = p;
                        break;

                    case 2:
                        r = p;
                        g = V;
                        b = t;
                        break;

                    case 3:
                        r = p;
                        g = q;
                        b = V;
                        break;

                    case 4:
                        r = t;
                        g = p;
                        b = V;
                        break;

                    default:
                        r = V;
                        g = p;
                        b = q;
                        break;
                }
            }
            return new byte[] { (byte)(r * 255), (byte)(g * 255), (byte)(b * 255) };
        }

        public static HSV RGBToHSV(MyColor color)
        {
            double delta, min;
            double h = 0, s, v;

            double RG_min = color.R < color.G ? color.R : color.G;
            min = RG_min < color.B ? RG_min : color.B;

            double RG_max = color.R > color.G ? color.R : color.G;
            v = RG_max > color.B ? RG_max : color.B;

            //min = Math.Min(Math.Min(R, G), B);
            //v = Math.Max(Math.Max(R, G), B);

            delta = v - min;

            if (v == 0.0)
                s = 0;
            else
                s = delta / v;

            if (s == 0)
                h = 0.0;
            else
            {
                if (color.R == v)
                    h = (color.G - color.B) / delta;
                else if (color.G == v)
                    h = 2 + (color.B - color.R) / delta;
                else if (color.B == v)
                    h = 4 + (color.R - color.G) / delta;

                h *= 60;

                if (h < 0.0)
                    h += 360;
            }
            return new HSV { H=h, S=s, V = (v / 255) };
        }

        public static double[] RGBToHSV(double R, double G, double B)
        {
            double delta, min;
            double h = 0, s, v;

            double RG_min = R < G ? R : G;
            min = RG_min < B ? RG_min : B;

            double RG_max = R > G ? R : G;
            v = RG_max > B ? RG_max : B;

            //min = Math.Min(Math.Min(R, G), B);
            //v = Math.Max(Math.Max(R, G), B);

            delta = v - min;

            if (v == 0.0)
                s = 0;
            else
                s = delta / v;

            if (s == 0)
                h = 0.0;
            else
            {
                if (R == v)
                    h = (G - B) / delta;
                else if (G == v)
                    h = 2 + (B - R) / delta;
                else if (B == v)
                    h = 4 + (R - G) / delta;

                h *= 60;

                if (h < 0.0)
                    h += 360;
            }
            return new double[] { h, s, (v / 250) };
        }
    }
}

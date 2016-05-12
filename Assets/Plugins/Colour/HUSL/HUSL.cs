using System;
using System.Collections.Generic;

public class HUSL
{
    public struct Pair
    {
        public double a, b;
    }

    public struct Triplet
    {
        public double a, b, c;
    }

    protected static double[][] M = new double[][]
    {
        new double[] {  3.240969941904521, -1.537383177570093, -0.498610760293    },
        new double[] { -0.96924363628087,   1.87596750150772,   0.041555057407175 },
        new double[] {  0.055630079696993, -0.20397695888897,   1.056971514242878 },
    };

    protected static Triplet[] Mt = new Triplet[]
    {
        new Triplet { a =  3.240969941904521, b = -1.537383177570093, c = -0.498610760293    },
        new Triplet { a = -0.96924363628087,  b =  1.87596750150772,  c =  0.041555057407175 },
        new Triplet { a =  0.055630079696993, b = -0.20397695888897,  c =  1.056971514242878 },
    };

    protected static double[][] MInv = new double[][]
    {
        new double[] { 0.41239079926595,  0.35758433938387, 0.18048078840183  },
        new double[] { 0.21263900587151,  0.71516867876775, 0.072192315360733 },
        new double[] { 0.019330818715591, 0.11919477979462, 0.95053215224966  },
    };

    protected static double RefX = 0.95045592705167;
    protected static double RefY = 1.0;
    protected static double RefZ = 1.089057750759878;

    protected static double RefU = 0.19783000664283;
    protected static double RefV = 0.46831999493879;

    protected static double Kappa   = 903.2962962;
    protected static double Epsilon = 0.0088564516;

    private static Pair[] bounds = new Pair[6];

    protected static Pair[] GetBounds(double L, out int count)
    {
        count = 0;

        double sub1 = Math.Pow(L + 16, 3) / 1560896;
        double sub2 = sub1 > Epsilon ? sub1 : L / Kappa;

        for (int c = 0; c < 3; ++c)
        {
            var m1 = M[c][0];
            var m2 = M[c][1]; 
            var m3 = M[c][2];

            for (int t = 0; t < 2; ++t)
            {
                var top1 = (284517 * m1 - 94839 * m3) * sub2;
                var top2 = (838422 * m3 + 769860 * m2 + 731718 * m1) * L * sub2 - 769860 * t * L;
                var bottom = (632260 * m3 - 126452 * m2) * sub2 + 126452 * t;

                bounds[count] = new Pair { a = top1 / bottom, b = top2 / bottom };
                count += 1;
            }
        }

        return bounds;
    }

    protected static double IntersectLineLine(Pair lineA,
                                              Pair lineB)
    {
        return (lineA.b - lineB.b) / (lineB.a - lineA.a);
    }

    protected static double DistanceFromPole(Pair point)
    {
        return Math.Sqrt(Math.Pow(point.a, 2) + Math.Pow(point.b, 2));
    }

    protected static bool LengthOfRayUntilIntersect(double theta, 
                                                    Pair line,
                                                    out double length)
    {
        length = line.b / (Math.Sin(theta) - line.a * Math.Cos(theta));

        return length >= 0;
    }

    protected static double MaxSafeChromaForL(double L) 
    {
        int count;

        var bounds = GetBounds(L, out count);
        double min = Double.MaxValue;

        Pair other = new Pair();

        for (int i = 0; i < 2; ++i)
        {
            Pair line = bounds[i];

            var m1 = line.a; 
            var b1 = line.b;

            other.a = -1 / line.a;

            double x = IntersectLineLine(line, other);

            other.a = x;
            other.b = line.b + x * line.a;

            double length = DistanceFromPole(other);

            min = Math.Min(min, length);
        }

        return min;
    }

    protected static double MaxChromaForLH(double L, double H) 
    {
        double hrad = H / 360 * Math.PI * 2;

        int count;

        var bounds = GetBounds(L, out count);
        double min = Double.MaxValue;

        for (int i = 0; i < count; ++i)
        {
            double length;

            if (LengthOfRayUntilIntersect(hrad, bounds[i], out length))
            {
                min = Math.Min(min, length);
            }
        }

        return min;
    }

    protected static double DotProduct(IList<double> a, 
                                       IList<double> b) 
    {
        double sum = 0;

        for (int i = 0; i < a.Count; ++i)
        {
            sum += a[i] * b[i];
        }

        return sum;
    }

    protected static double DotProduct(Triplet a,
                                       Triplet b)
    {
        return a.a * b.a
             + a.b * b.b
             + a.c * b.c;
    }

    protected static double Round(double value, int places) 
    {
        double n = Math.Pow(10, places);

        return Math.Round(value * n) / n;
    }

    protected static double FromLinear(double c) 
    {
        if (c <= 0.0031308)
        {
            return 12.92 * c;
        }
        else 
        {
            return 1.055 * Math.Pow(c, 1 / 2.4) - 0.055;
        }
    }
    
    protected static double ToLinear(double c) 
    {
        if (c > 0.04045)
        {
            return Math.Pow((c + 0.055) / (1 + 0.055), 2.4);
        } 
        else
        {
            return c / 12.92;
        }
    }

    protected static IList<int> RGBPrepare(IList<double> tuple)
    {

        for (int i = 0; i < tuple.Count; ++i)
        {
            tuple[i] = Round(tuple[i], 3);
        }

        for (int i = 0; i < tuple.Count; ++i) 
        {
            double ch = tuple[i];

            if (ch < -0.0001 || ch > 1.0001) 
            {
                throw new System.Exception("Illegal rgb value: " + ch);
            }
        }

        var results = new int[tuple.Count];

        for (int i = 0; i < tuple.Count; ++i) 
        {
            results[i] = (int) Math.Round(tuple[i] * 255);
        }

        return results;
    }

    public static Triplet XYZToRGB(Triplet triple) 
    {
        double R = FromLinear(DotProduct(Mt[0], triple));
        double G = FromLinear(DotProduct(Mt[1], triple));
        double B = FromLinear(DotProduct(Mt[2], triple));

        triple.a = R;
        triple.b = G;
        triple.c = B;

        return triple;
    }

    public static IList<double> RGBToXYZ(IList<double> tuple) 
    {
        var rgbl = new double[]
        {
            ToLinear(tuple[0]),
            ToLinear(tuple[1]),
            ToLinear(tuple[2]),
        };

        return new double[]
        {
            DotProduct(MInv[0], rgbl),
            DotProduct(MInv[1], rgbl),
            DotProduct(MInv[2], rgbl),
        };
    }

    protected static double YToL(double Y) 
    {
        if (Y <= Epsilon)
        {
            return (Y / RefY) * Kappa;
        } 
        else
        {
            return 116 * Math.Pow(Y / RefY, 1.0 / 3.0) - 16;
        }
    }

    protected static double LToY(double L) 
    {
        if (L <= 8) 
        {
            return RefY * L / Kappa;
        } 
        else 
        {
            return RefY * Math.Pow((L + 16) / 116, 3);
        }
    }

    public static IList<double> XYZToLUV(IList<double> tuple) 
    {
        double X = tuple[0];
        double Y = tuple[1];
        double Z = tuple[2];

        double varU = (4 * X) / (X + (15 * Y) + (3 * Z));
        double varV = (9 * Y) / (X + (15 * Y) + (3 * Z));

        double L = YToL(Y);

        if (L == 0) 
        {
            return new double[] { 0, 0, 0 };
        }

        var U = 13 * L * (varU - RefU);
        var V = 13 * L * (varV - RefV);

        return new Double [] { L, U, V };
    }
    
    public static Triplet LUVToXYZ(Triplet triple) 
    {
        double L = triple.a;
        double U = triple.b;
        double V = triple.c;

        if (L == 0) 
        {
            triple.a = 0;
            triple.b = 0;
            triple.c = 0;

            return triple;
        }

        double varU = U / (13 * L) + RefU;
        double varV = V / (13 * L) + RefV;

        double Y = LToY(L);
        double X = 0 - (9 * Y * varU) / ((varU - 4) * varV - varU * varV);
        double Z = (9 * Y - (15 * varV * Y) - (varV * X)) / (3 * varV);

        triple.a = X;
        triple.b = Y;
        triple.c = Z;

        return triple;
    }
    
    public static IList<double> LUVToLCH(IList<double> tuple) 
    {
        double L = tuple[0];
        double U = tuple[1];
        double V = tuple[2];

        double C = Math.Pow(Math.Pow(U, 2) + Math.Pow(V, 2), 0.5);
        double Hrad = Math.Atan2(V, U);

        double H = Hrad * 180.0 / Math.PI;

        if (H < 0) 
        {
            H = 360 + H;
        }

        return new double[] { L, C, H };
    }
    
    public static Triplet LCHToLUV(Triplet triple) 
    {
        double L = triple.a;
        double C = triple.b;
        double H = triple.c;

        double Hrad = H / 360.0 * 2 * Math.PI;
        double U = Math.Cos(Hrad) * C;
        double V = Math.Sin(Hrad) * C;

        triple.a = L;
        triple.b = U;
        triple.c = V;

        return triple;
    }
    
    public static Triplet HUSLToLCH(Triplet triple) 
    {
        double H = triple.a;
        double S = triple.b; 
        double L = triple.c;

        if (L > 99.9999999)
        {
            triple.a = 100;
            triple.b = 0;
            triple.c = H;

            return triple;
        }

        if (L < 0.00000001) 
        {
            triple.a = 0;
            triple.b = 0;
            triple.c = H;

            return triple;
        }

        double max = MaxChromaForLH(L, H);
        double C = max / 100 * S;

        triple.a = L;
        triple.b = C;
        triple.c = H;

        return triple;
    }
    
    public static IList<double> LCHToHUSL(IList<double> tuple) 
    {
        double L = tuple[0];
        double C = tuple[1];
        double H = tuple[2];

        if (L > 99.9999999) 
        {
            return new Double[] { H, 0, 100 };
        }

        if (L < 0.00000001) 
        {
            return new Double[] { H, 0, 0 };
        }

        double max = MaxChromaForLH(L, H);
        double S = C / max * 100;

        return new double[] { H, S, L };
    }
    
    public static Triplet HUSLPToLCH(Triplet triple) 
    {
        double H = triple.a;
        double S = triple.b; 
        double L = triple.c;
        
        if (L > 99.9999999)
        {
            triple.a = 100;
            triple.b = 0;
            triple.c = H;

            return triple;
        }
        
        if (L < 0.00000001) 
        {
            triple.a = 0;
            triple.b = 0;
            triple.c = H;

            return triple;
        }

        double max = MaxSafeChromaForL(L);
        double C = max / 100 * S;

        triple.a = L;
        triple.b = C;
        triple.c = H;

        return triple;
    }
    
    public static IList<double> LCHToHUSLP(IList<double> tuple) 
    {
        double L = tuple[0];
        double C = tuple[1];
        double H = tuple[2];
        
        if (L > 99.9999999) 
        {
            return new Double[] { H, 0, 100 };
        }
        
        if (L < 0.00000001) 
        {
            return new Double[] { H, 0, 0 };
        }
        
        double max = MaxSafeChromaForL(L);
        double S = C / max * 100;

        return new double[] { H, S, L };
    }
    
    public static string RGBToHex(IList<double> tuple) 
    {
        IList<int> prepared = RGBPrepare(tuple);

        return string.Format("#{0}{1}{2}",
                             prepared[0].ToString("x2"),
                             prepared[1].ToString("x2"),
                             prepared[2].ToString("x2"));
    }
    
    public static IList<double> HexToRGB(string hex) 
    {
        return new double[]
        {
            int.Parse(hex.Substring(1, 2), System.Globalization.NumberStyles.HexNumber) / 255.0,
            int.Parse(hex.Substring(3, 2), System.Globalization.NumberStyles.HexNumber) / 255.0,
            int.Parse(hex.Substring(5, 2), System.Globalization.NumberStyles.HexNumber) / 255.0,
        };
    }
    
    public static Triplet LCHToRGB(Triplet triple)
    {
        return XYZToRGB(LUVToXYZ(LCHToLUV(triple)));
    }
    
    public static IList<double> RGBToLCH(IList<double> tuple)
    {
        return LUVToLCH(XYZToLUV(RGBToXYZ(tuple)));
    }
    
    public static Triplet HUSLToRGB(Triplet triple)
    {
        return LCHToRGB(HUSLToLCH(triple));
    }

    public static IList<double> RGBToHUSL(IList<double> tuple)
    {
        return LCHToHUSL(RGBToLCH(tuple));
    }
    
    public static Triplet HUSLPToRGB(Triplet triple)
    {
        return LCHToRGB(HUSLPToLCH(triple));
    }
    
    public static IList<double> RGBToHUSLP(IList<double> tuple)
    {
        return LCHToHUSLP(RGBToLCH(tuple));
    }

}

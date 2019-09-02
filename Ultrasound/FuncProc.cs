using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ultrasound
{
    class FuncProc
    {
        public enum NoiseType { none, rnd, sp };
        public static float[] AntiSpike(float[] func, float acut = (float)Double.PositiveInfinity)
        {
            var l = func.Length;
            var res = new float[l];
            float border;
            if (acut == Double.PositiveInfinity)
            {
                var sortedFunc = (from entry in func orderby entry ascending select entry).ToArray();
                border = sortedFunc[(int)Math.Round(l * 0.9)];
                //var Q = GetQ(sortedFunc);
                //border = (Q["Q3"] - Q["Q1"]) * 1;
            }
            else
            {
                border = acut;
            }
            for (int i = 0; i < l; i++)
            {
                var v = func[i];
                if (Math.Abs(v) > border)
                {
                    res[i] = InBorder(func, border, i);
                }
                else
                {
                    res[i] = v;
                }
            }
            return res;
        }


        public static Dictionary<string, float> GetQ(float[] sortedFunc)
        {
            var res = new Dictionary<string, float>();
            double k = Convert.ToDouble(sortedFunc.Length) / 2;
            res.Add("Q2", sortedFunc[Convert.ToInt32(k) - 1]);
            double k1, k2;
            if (Math.Round(k) == k)
            {
                k1 = (k) / 2;
                k2 = k + (k) / 2;
            }
            else
            {
                k1 = Math.Floor(k) / 2;
                k2 = k + Math.Ceiling(k) / 2;
            }
            if (Math.Round(k1) == k1)
            {
                var el1 = sortedFunc[Convert.ToInt32(k1) - 1];
                var el2 = sortedFunc[Convert.ToInt32(k1 + 1) - 1];
                res.Add("Q1", (el2 + el1) / 2);
            }
            else
            {
                res.Add("Q1", sortedFunc[Convert.ToInt32(Math.Ceiling(k1)) - 1]);
            }
            if (Math.Round(k2) == k2)
            {
                var el1 = sortedFunc[Convert.ToInt32(k2) - 1];
                var el2 = sortedFunc[Convert.ToInt32(k2 + 1) - 1];
                res.Add("Q3", (el2 + el1) / 2);
            }
            else
            {
                res.Add("Q3", sortedFunc[Convert.ToInt32(Math.Ceiling(k2)) - 1]);
            }
            return res;
        }
        private static float InBorder(float[] func, float border, int i, int ignore = 0)
        {
            float v;
            if (i == 0 || (ignore == -1 && i != func.Length - 1))
            {
                v = func[i + 1];
                if (Math.Abs(v) > border)
                {
                    v = InBorder(func, border, i + 1, -1);
                }
            }
            else if (i == func.Length - 1 || ignore == 1)
            {
                v = func[i - 1];
                if (Math.Abs(v) > border)
                {
                    v = InBorder(func, border, i - 1, 1);
                }
            }
            else
            {
                var v1 = func[i - 1];
                if (Math.Abs(v1) > border)
                {
                    v1 = InBorder(func, border, i - 1, 1);
                }
                var v2 = func[i + 1];
                if (Math.Abs(v2) > border)
                {
                    v2 = InBorder(func, border, i + 1, -1);
                }
                v = (v1 + v2) / 2;
            }
            return v;
        }

        public static float[] Lpf(float fcut, int m)
        {
            var lpw = new float[m + 1];
            var res = new float[2 * m + 1];

            var d = new float[] { 0.35577019f, 0.2436983f, 0.07211497f, 0.00630165f };
            float arg = 2 * fcut * 0.001f;
            lpw[0] = arg;
            arg *= (float)Math.PI;
            for (int i = 1; i <= m; i++)
            {
                lpw[i] = (float)(Math.Sin(i * arg) / (Math.PI * i));
            }
            lpw[m] /= 2;

            float sumg = lpw[0];
            for (int i = 1; i <= m; i++)
            {
                float sum = d[0];
                arg = (float)(Math.PI * i) / m;
                for (int k = 1; k <= 3; k++)
                {
                    sum += 2 * d[k] * (float)Math.Cos(arg * k);
                }
                lpw[i] *= sum;
                sumg += 2 * lpw[i];

            }
            for (int i = 0; i <= m; i++)
            {
                lpw[i] /= sumg;
            }

            for (int i = 1; i <= m; i++)
            {
                res[i - 1] = lpw[m - i + 1];
                res[i + m] = lpw[i];
            }
            res[m] = lpw[0];

            return res;
        }
        public static float[] Hpf(float fcut, int m)
        {
            var lpw = Lpf(fcut, m);
            var res = new float[2 * m + 1];
            for (int i = 0; i <= 2 * m; i++)
            {
                if (i == m)
                {
                    res[i] = 1 - lpw[i];
                }
                else
                {
                    res[i] = -lpw[i];
                }
            }
            return res;
        }


        public static float[] ApplyFilter(float[] func1, float[] func2)
        {
            var newData = new float[func1.Length];
            var func1Expanded = new float[func1.Length + func2.Length];
            for (var i = func1.Length - 1; i >= 0; i--)
            {
                func1Expanded[i + func2.Length / 2] = func1[i];
            }

            for (var k = 0; k < func1.Length; k++)
            {
                float sum = 0;
                for (var i = 0; i < func2.Length; i++)
                {
                    sum += func1Expanded[k - i + func2.Length] * func2[i];
                }
                newData[k] = sum;
            }

            return newData;
        }

        public static float[,] ApplyFilter(float[,] data, float[] func2)
        {
            var newData = new float[data.GetLength(0), data.GetLength(1)];
            for (int j = 0; j < data.GetLength(0); j++)
            {
                var func1 = new float[data.GetLength(1)];
                for (int i = 0; i < data.GetLength(1); i++)
                {
                    func1[i] = data[j, i];
                }
                func1 = ApplyFilter(func1, func2);
                for (int i = 0; i < data.GetLength(1); i++)
                {
                    newData[j, i] = func1[i];
                }
            }
            return newData;
        }

        public static float[] GetDiscreteSpectre(float[] data)
        {
            var compData = new Exocortex.DSP.Complex[4096];
            for (int i = 0; i < Math.Min(data.Length, compData.Length); i++)
            {
                compData[i].Re = data[i];
            }
            Exocortex.DSP.Fourier.FFT(compData, compData.Length, Exocortex.DSP.FourierDirection.Forward);


            return GetDiscrete(compData);
        }

        public static float[,] GetDiscreteSpectre(float[,] data)
        {
            int w = data.GetLength(0);
            int h = data.GetLength(1);
            int lenX = (int)Math.Pow(2, Math.Ceiling(Math.Log(w, 2)));
            int lenY = (int)Math.Pow(2, Math.Ceiling(Math.Log(h, 2)));
            var compData = new Exocortex.DSP.Complex[lenX * lenY];
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    compData[i * h + j].Re = data[i, j];
                }
            }
            Exocortex.DSP.Fourier.FFT2(compData, lenX, lenY, Exocortex.DSP.FourierDirection.Forward);
            var res = new float[w, h];
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    res[i, j] = (float)Math.Sqrt(Math.Pow(compData[i * h + j].Re, 2) + Math.Pow(compData[i * h + j].Im, 2));
                }
            }


            return res;
        }

        public static float[] GetDiscrete(Exocortex.DSP.Complex[] compData, bool half = true)
        {
            var len = half ? compData.Length / 2 : compData.Length;
            var resDiscreed = new float[len];
            for (int i = 0; i < len; i++)
            {
                resDiscreed[i] = (float)Math.Sqrt(Math.Pow(compData[i].Re, 2) + Math.Pow(compData[i].Im, 2));
            }

            return resDiscreed;
        }
        public static float[,] AddNoise(float[,] data, NoiseType noiseType, double f = 0.1, int rndSize = 5)
        {
            if (noiseType == NoiseType.none) return data;

            int w = data.GetLength(0);
            int h = data.GetLength(1);
            var newData = new float[w, h];
            Random rnd = new Random();

            double min = Double.PositiveInfinity;
            double max = Double.NegativeInfinity;
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    min = Math.Min(min, data[i, j]);
                    max = Math.Max(max, data[i, j]);
                }
            }

            float swipe = (float)(max - min);

            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    float val = data[i, j];
                    if (rnd.NextDouble() <= f)
                    {
                        switch (noiseType)
                        {
                            case NoiseType.rnd:
                                val += rnd.Next(-1, 2) * rnd.Next(1, rndSize) / 10 * swipe;
                                break;
                            case NoiseType.sp:
                                val = (float)rnd.NextDouble() / 10 * swipe;
                                break;
                        }
                    }
                    newData[i, j] = val;

                }
            }

            return newData;
        }

    }
}

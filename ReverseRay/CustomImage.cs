using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

namespace Ultrasound
{
    struct DataPoint
    {
        public float Value;
        public int X;
        public int Y;
    }

    class CustomImage
    {
        class DataResult
        {
            private DataPoint[,] data;

            public DataResult(float[,] data)
            {
                int w = data.GetLength(0);
                int h = data.GetLength(1);
                this.data = new DataPoint[w, h];

                for (int i = 0; i < w; i++)
                {
                    for (int j = 0; j < h; j++)
                    {
                        this.data[i, j] = new DataPoint() { Value = data[i, j], X = i, Y = j };
                    }
                }
            }


            public DataResult FlipData(bool flipX, bool flipY)
            {
                if (!flipX && !flipY)
                {
                    return this;
                }
                int w = data.GetLength(0);
                int h = data.GetLength(1);
                if (w == 0 || h == 0)
                {
                    return this;
                }
                var flipped = new DataPoint[w, h];

                int kX = flipX ? 1 : 0;
                int kY = flipY ? 1 : 0;

                for (int i = 0; i < w; i++)
                {
                    for (int j = 0; j < h; j++)
                    {
                        flipped[i, j] = data[kX * (w - 1) + (1 - kX * 2) * i, kY * (h - 1) + (1 - kY * 2) * j];
                    }
                }

                data = flipped;

                return this;
            }

            public DataResult FilterData(double limit)
            {
                int w = data.GetLength(0);
                int h = data.GetLength(1);
                if (w == 0 || h == 0)
                {
                    return this;
                }
                var filtered = new DataPoint[w, h];

                float posInf = (float)Double.PositiveInfinity;

                for (int i = 0; i < w; i++)
                {
                    for (int j = 0; j < h; j++)
                    {
                        filtered[i, j] = Math.Abs(data[i, j].Value) > limit ? data[i, j] : (new DataPoint() { Value = posInf, X = data[i, j].X, Y = data[i, j].Y });
                    }
                }

                data = filtered;

                return this;
            }

            public DataResult RotateData(double rot)
            {
                if (rot == 0)
                {
                    return this;
                }
                int w = data.GetLength(0);
                int h = data.GetLength(1);
                if (w == 0 || h == 0)
                {
                    return this;
                }
                var origPoints = new PointF[w * h];
                var rotatedPoints = new PointF[origPoints.Length];

                for (int i = 0; i < w; i++)
                {
                    for (int j = 0; j < h; j++)
                    {
                        int k = i * h + j;
                        origPoints[k] = new PointF(i, j);
                        rotatedPoints[k] = RotatePoint(origPoints[k], rot);
                    }
                }

                float posInf = (float)Double.PositiveInfinity;
                float negInf = (float)Double.NegativeInfinity;

                float boxL = posInf;
                float boxT = posInf;
                float boxR = negInf;
                float boxB = negInf;
                for (int i = 0; i < w * h; i++)
                {
                    PointF p = rotatedPoints[i];
                    if (p.X < boxL) boxL = p.X;
                    if (p.X > boxR) boxR = p.X;
                    if (p.Y < boxT) boxT = p.Y;
                    if (p.Y > boxB) boxB = p.Y;
                }

                int newW = (int)(boxR - boxL + 1);
                int newH = (int)(boxB - boxT + 1);
                var rotated = new DataPoint[newW, newH];

                for (int i = 0; i < newW; i++)
                {
                    for (int j = 0; j < newH; j++)
                    {
                        rotated[i, j] = new DataPoint() { Value = negInf, X = -1, Y = -1 };
                    }
                }
                for (int i = 0; i < w * h; i++)
                {
                    PointF p = rotatedPoints[i];
                    PointF origP = origPoints[i];
                    rotated[(int)(p.X - boxL), (int)(p.Y - boxT)] = data[(int)origP.X, (int)origP.Y];
                }

                data = rotated;

                return this;
            }

            public DataResult RepairData()
            {
                float negInf = (float)Double.NegativeInfinity;
                int w = data.GetLength(0);
                int h = data.GetLength(1);
                if (w == 0 || h == 0)
                {
                    return this;
                }

                var repaired = new DataPoint[w, h];

                for (int i = 0; i < w; i++)
                {
                    for (int j = 0; j < h; j++)
                    {
                        if (data[i, j].Value != negInf)
                        {
                            repaired[i, j] = data[i, j];
                            continue;
                        }
                        float sum = 0;
                        int pxCnt = 0;
                        if (i > 0 && data[i - 1, j].Value != negInf)
                        {
                            pxCnt++;
                            sum += data[i - 1, j].Value;
                        }
                        if (j > 0 && data[i, j - 1].Value != negInf)
                        {
                            pxCnt++;
                            sum += data[i, j - 1].Value;
                        }
                        if (i < w - 1 && data[i + 1, j].Value != negInf)
                        {
                            pxCnt++;
                            sum += data[i + 1, j].Value;
                        }
                        if (j < h - 1 && data[i, j + 1].Value != negInf)
                        {
                            pxCnt++;
                            sum += data[i, j + 1].Value;
                        }


                        if (i > 0 && j > 0 && data[i - 1, j - 1].Value != negInf)
                        {
                            pxCnt++;
                            sum += data[i - 1, j - 1].Value;
                        }
                        if (i < w - 1 && j > 0 && data[i + 1, j - 1].Value != negInf)
                        {
                            pxCnt++;
                            sum += data[i + 1, j - 1].Value;
                        }
                        if (i > 0 && j < h - 1 && data[i - 1, j + 1].Value != negInf)
                        {
                            pxCnt++;
                            sum += data[i - 1, j + 1].Value;
                        }
                        if (i < w - 1 && j < h - 1 && data[i + 1, j + 1].Value != negInf)
                        {
                            pxCnt++;
                            sum += data[i + 1, j + 1].Value;
                        }
                        if (pxCnt > 4)
                        {
                            repaired[i, j] = new DataPoint() { Value = sum / pxCnt, X = -1, Y = -1 };
                        }
                        else
                        {
                            repaired[i, j] = data[i, j];
                        }
                    }
                }

                data = repaired;

                return this;
            }

            public DataResult ScaleData(double scaleX, double scaleY)
            {
                if (scaleX == 1 && scaleY == 1)
                {
                    return this;
                }
                int w = data.GetLength(0);
                int h = data.GetLength(1);
                if (w == 0 || h == 0)
                {
                    return this;
                }
                int sw = (int)Math.Ceiling(w * scaleX);
                int sh = (int)Math.Ceiling(h * scaleY);
                var scaled = new DataPoint[sw, sh];

                for (int i = 0; i < sw; i++)
                {
                    for (int j = 0; j < sh; j++)
                    {
                        scaled[i, j] = data[(int)(i / scaleX), (int)(j / scaleY)];
                    }
                }

                data = scaled;

                return this;
            }

            private PointF GetDataCenter()
            {
                return new PointF((int)Math.Floor((double)data.GetLength(0) / 2), (int)Math.Floor((double)data.GetLength(1) / 2));
            }

            private PointF RotatePoint(PointF p, double angle)
            {
                var c = GetDataCenter();
                double sin = Math.Sin(angle); // angle is in radians
                double cos = Math.Cos(angle); // angle is in radians
                double xnew = cos * (p.X - c.X) - sin * (p.Y - c.Y) + c.X;
                double ynew = sin * (p.X - c.X) + cos * (p.Y - c.Y) + c.Y;
                return new PointF((float)xnew, (float)ynew);
            }

            public DataPoint[,] GetData()
            {
                return data;
            }
        }
        public static float[,] ReadData(string path, bool norming = false)
        {

            FileInfo fInfo = new FileInfo(path);
            int a = 0, b = 0;
            float[,] data = null;
            string input;
            switch (fInfo.Extension)
            {
                case ".dat":
                case ".xcr":
                    input = Interaction.InputBox("Введите кол-во столбцов", "Открытие файла", "");
                    if (!Int32.TryParse(input, out a))
                    {
                        a = 0;
                    }
                    input = Interaction.InputBox("Введите кол-во строк", "Открытие файла", "");
                    if (!Int32.TryParse(input, out b))
                    {
                        b = 0;
                    }
                    if (a <= 0 || b <= 0)
                    {
                        return null;
                    }

                    int l = a * b;
                    double[] res = new double[l];

                    FileStream fStream;
                    byte[] buffer;

                    switch (fInfo.Extension)
                    {
                        case ".dat":
                            fStream = new FileStream(path, FileMode.Open);
                            buffer = new byte[fStream.Length];
                            fStream.Read(buffer, 0, (int)fStream.Length);
                            for (int i = 0; i < l; i++)
                            {
                                res[i] = BitConverter.ToSingle(buffer, i * 4);
                            }
                            fStream.Close();
                            break;
                        case ".xcr":
                            fStream = new FileStream(path, FileMode.Open);
                            buffer = new byte[fStream.Length];
                            fStream.Read(buffer, 0, (int)fStream.Length);
                            //bufferOld = new byte[fStream.Length];
                            //fStream.Read(bufferOld, 0, (int)fStream.Length);
                            //for (int i = 0; i < bufferOld.Length; i += 2)
                            //{
                            //    buffer[i] = bufferOld[i + 1];
                            //    buffer[i + 1] = bufferOld[i];
                            //}

                            int header;
                            if (a * b * 2 == (int)fStream.Length)
                            {
                                header = 0;
                            }
                            else
                            {
                                input = Interaction.InputBox("Размер заголовка", "Открытие файла", "1024");
                                if (!Int32.TryParse(input, out header))
                                {
                                    header = 1024;
                                }
                            }

                            for (int i = 0; i < l; i++)
                            {
                                res[i] = BitConverter.ToUInt16(buffer, (i + header) * 2);
                            }
                            fStream.Close();
                            break;
                    }
                    data = new float[a, b];
                    float min;
                    float max;

                    min = (float)Double.PositiveInfinity;
                    max = (float)Double.NegativeInfinity;
                    for (int i = 0; i < a; i++)
                    {
                        for (int j = 0; j < b; j++)
                        {
                            data[i, j] = (float)res[(b - 1 - j) * a + i];
                            if (data[i, j] < min)
                            {
                                min = data[i, j];
                            }
                            if (data[i, j] > max)
                            {
                                max = data[i, j];
                            }
                        }
                    }
                    if (norming)
                    {
                        for (int i = 0; i < a; i++)
                        {
                            for (int j = 0; j < b; j++)
                            {
                                data[i, j] = 255 * (data[i, j] - min) / (max - min);
                                //var br = (int)data[i, j];
                                //curImg.SetPixel(j, b - i - 1, Color.FromArgb(255, br, br, br));
                            }
                        }
                    }
                    break;
                case ".slice":
                    input = Interaction.InputBox("Введите кол-во столбцов", "Открытие файла", "");
                    if (!Int32.TryParse(input, out a))
                    {
                        a = 0;
                    }
                    input = Interaction.InputBox("Введите кол-во строк", "Открытие файла", "");
                    if (!Int32.TryParse(input, out b))
                    {
                        b = 0;
                    }
                    if (a <= 0 || b <= 0)
                    {
                        return null;
                    }

                    l = a * b;
                    res = new double[l];
                    fStream = new FileStream(path, FileMode.Open);
                    buffer = new byte[fStream.Length];
                    fStream.Read(buffer, 0, (int)fStream.Length);
                    for (int i = 0; i < l; i++)
                    {
                        res[i] = BitConverter.ToSingle(buffer, i * 4);
                    }
                    fStream.Close();
                    data = new float[a, b];

                    min = (float)Double.PositiveInfinity;
                    max = (float)Double.NegativeInfinity;
                    for (int i = 0; i < a; i++)
                    {
                        for (int j = 0; j < b; j++)
                        {
                            data[i, j] = (float)res[i * b + j];
                            if (data[i, j] < min)
                            {
                                min = data[i, j];
                            }
                            if (data[i, j] > max)
                            {
                                max = data[i, j];
                            }
                        }
                    }
                    if (norming)
                    {
                        for (int i = 0; i < a; i++)
                        {
                            for (int j = 0; j < b; j++)
                            {
                                data[i, j] = 255 * (data[i, j] - min) / (max - min);
                                //var br = (int)data[i, j];
                                //curImg.SetPixel(j, b - i - 1, Color.FromArgb(255, br, br, br));
                            }
                        }
                    }
                    break;
                case ".jpg":
                case ".jpeg":
                case ".png":
                    Bitmap img = new Bitmap(path, true);
                    a = img.Width;
                    b = img.Height;
                    data = new float[a, b];
                    Color c;
                    for (int i = 0; i < a; i++)
                    {
                        for (int j = 0; j < b; j++)
                        {
                            c = img.GetPixel(i, j);
                            data[i, j] = (c.R + c.G + c.B) / 3f;
                        }
                    }
                    break;
                default:
                    throw new FileLoadException("Неопознанный формат файла. Поддерживаются: xcr, dat, jpg, jpeg, png");
            }

            return data;
        }

        public static Bitmap ReadImg(string path)
        {
            FileInfo fInfo = new FileInfo(path);
            Bitmap curImg = null;
            int a = 0, b = 0;
            if (fInfo.Extension == ".dat" || fInfo.Extension == ".xcr")
            {
                string input;
                input = Interaction.InputBox("Введите кол-во столбцов", "Открытие файла", "");
                if (!Int32.TryParse(input, out a))
                {
                    a = 0;
                }
                input = Interaction.InputBox("Введите кол-во строк", "Открытие файла", "");
                if (!Int32.TryParse(input, out b))
                {
                    b = 0;
                }
                if (a <= 0 || b <= 0)
                {
                    return null;
                }
            }
            int l = a * b;
            double[] res = new double[0];

            FileStream fStream;
            byte[] buffer;

            double[][] data;
            double min;
            double max;
            switch (fInfo.Extension)
            {
                case ".dat":
                    curImg = new Bitmap(a, b);
                    fStream = new FileStream(path, FileMode.Open);
                    buffer = new byte[fStream.Length];
                    fStream.Read(buffer, 0, (int)fStream.Length);
                    res = new double[l];
                    for (int i = 0; i < l; i++)
                    {
                        res[i] = BitConverter.ToSingle(buffer, i * 4);
                    }
                    fStream.Close();

                    data = new double[b][];
                    min = Double.PositiveInfinity;
                    max = Double.NegativeInfinity;
                    for (int i = 0; i < b; i++)
                    {
                        data[i] = new double[a];
                        for (int j = 0; j < a; j++)
                        {
                            data[i][j] = res[i * a + j];
                            if (data[i][j] < min)
                            {
                                min = data[i][j];
                            }
                            if (data[i][j] > max)
                            {
                                max = data[i][j];
                            }
                        }
                    }
                    for (int i = 0; i < b; i++)
                    {
                        for (int j = 0; j < a; j++)
                        {
                            data[i][j] = 255 * (data[i][j] - min) / (max - min);
                        }
                    }
                    min = Double.PositiveInfinity;
                    max = Double.NegativeInfinity;
                    for (int i = 0; i < b; i++)
                    {
                        for (int j = 0; j < a; j++)
                        {
                            if (data[i][j] < min)
                            {
                                min = data[i][j];
                            }
                            if (data[i][j] > max)
                            {
                                max = data[i][j];
                            }
                        }
                    }
                    for (int i = 0; i < b; i++)
                    {
                        for (int j = 0; j < a; j++)
                        {
                            var br = (int)data[i][j];
                            curImg.SetPixel(j, b - i - 1, Color.FromArgb(255, br, br, br));
                        }
                    }
                    break;
                case ".xcr":
                    curImg = new Bitmap(a, b);
                    fStream = new FileStream(path, FileMode.Open);
                    buffer = new byte[fStream.Length];
                    fStream.Read(buffer, 0, (int)fStream.Length);
                    //bufferOld = new byte[fStream.Length];
                    //fStream.Read(bufferOld, 0, (int)fStream.Length);
                    //for (int i = 0; i < bufferOld.Length; i += 2)
                    //{
                    //    buffer[i] = bufferOld[i + 1];
                    //    buffer[i + 1] = bufferOld[i];
                    //}

                    int header;
                    if (a * b * 2 == (int)fStream.Length)
                    {
                        header = 0;
                    }
                    else
                    {
                        string input = Interaction.InputBox("Размер заголовка", "Открытие файла", "1024");
                        if (!Int32.TryParse(input, out header))
                        {
                            header = 1024;
                        }
                    }

                    res = new double[l];
                    for (int i = 0; i < l; i++)
                    {
                        res[i] = BitConverter.ToUInt16(buffer, (i + header) * 2);
                    }
                    fStream.Close();

                    data = new double[b][];
                    min = Double.PositiveInfinity;
                    max = Double.NegativeInfinity;
                    for (int i = 0; i < b; i++)
                    {
                        data[i] = new double[a];
                        for (int j = 0; j < a; j++)
                        {
                            data[i][j] = res[i * a + j];
                            if (data[i][j] < min)
                            {
                                min = data[i][j];
                            }
                            if (data[i][j] > max)
                            {
                                max = data[i][j];
                            }
                        }
                    }
                    for (int i = 0; i < b; i++)
                    {
                        for (int j = 0; j < a; j++)
                        {
                            data[i][j] = 255 * (data[i][j] - min) / (max - min);
                        }
                    }
                    min = Double.PositiveInfinity;
                    max = Double.NegativeInfinity;
                    for (int i = 0; i < b; i++)
                    {
                        for (int j = 0; j < a; j++)
                        {
                            if (data[i][j] < min)
                            {
                                min = data[i][j];
                            }
                            if (data[i][j] > max)
                            {
                                max = data[i][j];
                            }
                        }
                    }
                    for (int i = 0; i < b; i++)
                    {
                        for (int j = 0; j < a; j++)
                        {
                            var br = (int)data[i][j];
                            curImg.SetPixel(j, b - i - 1, Color.FromArgb(255, br, br, br));
                        }
                    }
                    break;
                case ".jpg":
                case ".jpeg":
                case ".png":
                    curImg = new Bitmap(path, true);
                    break;
            }

            return curImg;
        }


        private Bitmap cachedBitmap = null;
        private bool changed = true;

        public Bitmap Bitmap
        {
            get
            {
                if (changed)
                {
                    RefreshCache();
                }
                return cachedBitmap;
            }
        }



        private DataResult GetDataResult()
        {
            return dataResult;
        }

        private double pi2 = Math.PI * 2;

        private double scaleX = 1;
        public double ScaleX
        {
            get
            {
                return scaleX;
            }
            set
            {
                if (value > 0)
                {
                    changed = true;
                    scaleX = value;
                }
            }
        }

        private double scaleY = 1;
        public double ScaleY
        {
            get
            {
                return scaleY;
            }
            set
            {
                if (value > 0)
                {
                    changed = true;
                    scaleY = value;
                }
            }
        }

        private bool flipX = false;
        public bool FlipX
        {
            get
            {
                return flipX;
            }
            set
            {
                changed = true;
                flipX = value;
            }
        }

        private bool flipY = false;
        public bool FlipY
        {
            get
            {
                return flipY;
            }
            set
            {
                changed = true;
                flipY = value;
            }
        }

        private bool color = false;
        public bool IsColored
        {
            get
            {
                return color;
            }
            set
            {
                changed = true;
                color = value;
            }
        }

        private double bright = 1;
        public double Bright
        {
            get
            {
                return bright;
            }
            set
            {
                changed = true;
                bright = value;
            }
        }

        private DataResult dataResult;


        private float[,] data;
        public float[,] Data
        {
            get { return data; }
            set
            {
                changed = true;
                data = value;

            }
        }

        private double rotation = 0;
        public double Rotation
        {
            get { return rotation; }
            set
            {
                value = value % pi2;
                if (value < 0)
                {
                    value += pi2;
                }
                changed = true;
                rotation = value;
            }
        }
        public double RotationGrad
        {
            get { return rotation * 360 / pi2; }
            set
            {
                Rotation = value * pi2 / 360;
            }
        }

        private double limit = 1;
        public double LimitPow
        {
            get
            {
                return Math.Round(Math.Log10(1.0 / limit), 4);
            }
            set
            {
                changed = true;
                limit = 1 / Math.Pow(10, Math.Round(value, 4));
            }
        }

        private void RefreshCache()
        {
            dataResult = GenerateDataResult();
            cachedBitmap = GenerateBitmap();
            changed = false;
        }

        private Bitmap GenerateBitmap()
        {

            float[,] imgData = GetImgData();
            int w = imgData.GetLength(0);
            int h = imgData.GetLength(1);
            if (w == 0 || h == 0)
            {
                return null;
            }

            float negInf = (float)Double.NegativeInfinity;
            float posInf = (float)Double.PositiveInfinity;

            float max = negInf;
            float min = posInf;
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    if (imgData[i, j] == negInf) continue; // при поворотах пустые пиксели не считаем
                    if (imgData[i, j] == posInf) continue; // все что меньше лимита не считаем
                    if (imgData[i, j] < min) min = imgData[i, j];
                    if (imgData[i, j] > max) max = imgData[i, j];
                }
            }
            //max = Math.Max(Math.Abs(min), Math.Abs(max));

            var bitmap = new DirectBitmap(w, h);
            if (min == max) return bitmap.Bitmap;

            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    float val = imgData[i, j];
                    if (val == negInf)
                    {
                        bitmap.SetPixel(i, j, Color.Transparent);
                        continue;
                    }
                    if (val == posInf)
                    {
                        bitmap.SetPixel(i, j, Color.Black);
                        continue;
                    }
                    if (!IsColored)
                    {
                        int shade = (int)(Math.Abs(255 * (val - min) / (max - min)) * bright);
                        if (shade > 255) shade = 255;
                        if (val > 0)
                            bitmap.SetPixel(i, j, Color.FromArgb(shade, shade, shade));
                        else if (val < 0)
                            bitmap.SetPixel(i, j, Color.FromArgb(shade, shade, Math.Min(255, shade * 2)));
                    } else
                    {
                        int maxShade = 256 * 256 * 256 - 1;
                        int shade = (int)(Math.Abs(maxShade * (val - min) / (max - min)) * bright);
                        if (shade > maxShade) shade = maxShade;
                        int B = shade % 256;
                        shade -= B;
                        shade /= 256;
                        int G = shade % 256;
                        shade -= G;
                        shade /= 256;
                        int R = shade;
                        bitmap.SetPixel(i, j, Color.FromArgb(R, G, B));
                    }
                }
            }

            return bitmap.Bitmap;
        }

        public float GetValue(Point point)
        {
            var kp = GetDataKeyPair(point);
            return GetValue(kp);
        }

        public float GetValue(Tuple<int, int> keyPair)
        {
            return data[keyPair.Item1, keyPair.Item2];
        }


        public DataPoint GetDataPoint(Point point)
        {
            var data = GetDataResult().GetData();
            int w = data.GetLength(0);
            int h = data.GetLength(1);
            if (point.X > 0 && point.X < w && point.Y > 0 && point.Y < h)
            {
                return data[point.X, point.Y];
            }
            return new DataPoint() { Value = 0, X = -1, Y = -1 };
        }

        public Tuple<int, int> GetDataKeyPair(Point point)
        {
            var data = GetDataResult().GetData();
            var res = new Tuple<int, int>(data[point.X, point.Y].X, data[point.X, point.Y].Y);
            return res;
        }

        private DataResult GenerateDataResult()
        {
            var dataResult = new DataResult(data)
                .FlipData(flipX, flipY)
                .ScaleData(scaleX, scaleY)
                .RotateData(rotation)
                .RepairData()
                .FilterData(limit);

            return dataResult;
        }

        private float[,] GetImgData()
        {
            var data = dataResult.GetData();
            int w = data.GetLength(0);
            int h = data.GetLength(1);

            var floatData = new float[w, h];
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    floatData[i, j] = data[i, j].Value;
                }
            }

            return floatData;
        }

        public CustomImage Clone()
        {
            var obj = new CustomImage();
            obj.LimitPow = LimitPow;
            obj.FlipX = FlipX;
            obj.FlipY = FlipY;
            obj.ScaleX = ScaleX;
            obj.ScaleY = ScaleY;
            obj.Bright = Bright;
            obj.Rotation = Rotation;
            obj.Data = Data;
            return obj;
        }
    }
}

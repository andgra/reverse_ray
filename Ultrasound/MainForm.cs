using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using ZedGraph;
using System.Diagnostics;
using Exocortex.DSP;
using Microsoft.VisualBasic;

namespace Ultrasound
{
    public partial class MainForm : Form
    {
        public int currentSlice = 15; //номер слайса
        public float scX = 2; //масштаб по X
        public float scY = 0.2f; //масштаб по Y
        public float rot = 0; //поворот в градусах
        public float limit = 8; //уровень фильтрации
        public SliceFormingType defaultSorting = SliceFormingType.opv;

        public bool useDeconv = true;

        public enum GroupStatus {none = -3, avg = -1, sum = -2};
        public GroupStatus curGroupStatus = GroupStatus.avg;

        public int dataX, dataY, dataWidth, dataHeight;

        float[,] data;//данные текущего слайса

        private CustomImage curImg;

        public MainForm()
        {
            InitializeComponent();
            resizePanel();
            curImg = new CustomImage();
            Data.useDeconvolvedData = useDeconv;
        }

        private void Img_Resize(object sender, EventArgs e)
        {
            resizePanel();
        }

        public void resizePanel()
        {
            var size = this.Size;
            var panelSize = imgPanel.Size;
            panelSize.Height = size.Height - imgBox.Top - mainMenu.Height - imgPanel.Top - System.Windows.Forms.SystemInformation.HorizontalScrollBarHeight;
            panelSize.Width = size.Width - imgBox.Left - imgPanel.Left - System.Windows.Forms.SystemInformation.VerticalScrollBarWidth;
            imgPanel.Size = panelSize;
        }

        private void openImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                if (!openFile(openFileDialog.FileName)) return;
            }
        }

        private bool openFile(string path)//, bool reverse = false)
        {
            try
            {
                Data.OpenMultiFile(path);
            }
            catch (Exception exp)
            {
                MessageBox.Show("An error occurred while attempting to open the file. The error is:" + Environment.NewLine + exp + Environment.NewLine);
                return false;
            }
            //if (reverse)
            //    Data.MakeReverse(Data.data);

            InitiateViewSetting();

            //CheckDataForAnomalMaxValues(1.2F, -1.4F);//для проверки на значения, выходящие за заданные пределы
            
            DrawImage();

            InitiateViewSettingAfterDraw();
            
            return true;
        }

        private void InitiateViewSetting()//проводим инициализацию того, что должно быть инициализировано до отрисовки
        {
            if (Data.numberOfTraces - 1 < currentSlice) currentSlice = Data.numberOfTraces - 1;
            Data.RefreshSlicesData(defaultSorting);
            sliceNumInput.Maximum = Data.numberOfTraces - 1;
            sliceNumInput.ValueChanged -= numericUpDown_ValueChanged;
            sliceNumInput.Value = currentSlice;
            sliceNumInput.ValueChanged += numericUpDown_ValueChanged;
            вклвыклToolStripMenuItem.Checked = Data.useDeconvolvedData;
            LoadNewSlice();
            //numericUpDownX.Maximum = Data.width;
            //numericUpDownY.Maximum = Data.maxtime;
            //numericUpDownWidth.Maximum = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;//Data.width - 1;
            //numericUpDownHeight.Maximum = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;//Data.maxtime;

            //dataWidth = Math.Min(Data.width, (int)System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width);
            //dataHeight = Math.Min(Data.maxtime, (int)numericUpDownHeight.Maximum);

            //numericUpDownWidth.Value = dataWidth;
            //numericUpDownHeight.Value = dataHeight;

            //numericUpDownY.Increment = dataHeight / 2;
            //numericUpDownX.Increment = dataWidth / 2;

            //limit = 1;
            //magn = 1;

            //comboBoxMagn.Text = "X1";
            //numericUpDownLimit.Value = 0;
            //Проверить
        }

        private void InitiateViewSettingAfterDraw()//проводим инициализацию того, что должно быть инициализировано только после отрисовки
        {
            curImg.LimitPow = limit;
            ScaleImg(scX, scY);
            RotateImg(rot);
            SetFormingTypeMenu(Data.currentSliceFormingType);
            imgBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBox_MouseMove);//отображаем данные по координатам только после загрузки
            imgBox.MouseLeave += new EventHandler(pictureBox_MouseLeave);
        }

        private void LoadNewSlice(int slice = -1)//загружаем данные текущего слайса
        {
            switch (curGroupStatus)
            {
                case GroupStatus.avg:
                    data = Data.AvgSlices();
                    break;
                case GroupStatus.sum:
                    data = Data.SumSlices();
                    break;
                case GroupStatus.none:
                    if (slice < 0)
                    {
                        slice = currentSlice;
                    }
                    data = Data.GetSlice(slice);
                    sliceNumInput.Value = slice;
                    currentSlice = slice;
                    break;
            }
        }

        private void DrawImage(bool isColored = false)//отрисовываем данные текущего слайса
        {
            curImg.Data = data;
            curImg.IsColored = isColored;
            imgBox.Image = curImg.Bitmap;
            imgBox.Size = imgBox.Image.Size;
        }

        private void pictureBox_MouseMove(object sender, MouseEventArgs e)//обновляем информационное табло при движениях мышки
        {
            var dp = curImg.GetDataPoint(new Point(e.X, e.Y));
            //if (e.X / magn + dataX < Data.width && e.Y / magn + dataY < Data.maxtime)
                this.Text = "x: " + dp.X + "; time: " + dp.Y * Data.dt + " = " + dp.Y + " * dt; value: " + dp.Value;
            //else
                //this.Text = Data.filename;//когда мы не показываем данные, показываем название файла
        }

        void pictureBox_MouseLeave(object sender, EventArgs e)//когда мы не показываем данные, показываем название файла
        {
            this.Text = Data.filename;
        }

        private void numericUpDown_ValueChanged(object sender, EventArgs e)//загружаем указанный слайс
        {
            currentSlice = (int)sliceNumInput.Value;
            curGroupStatus = GroupStatus.none;
            LoadNewSlice(currentSlice);
            //data = KinematicFixes();
            DrawImage();
        }

        /// <summary>
        /// Алгоритм подавления переотражений. Сначала запрашивается реверсируемое изображение, затем нормальное, потом делаем И между реверсом первого и вторым
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void refractionToolStripMenuItem_Click(object sender, EventArgs e)
        //{
        //    DialogResult result = openFileDialog.ShowDialog();
        //    if (result == DialogResult.OK)//сначала преломление(в первый массив)
        //    {
        //        Data.OpenFile(openFileDialog.FileName, false);
        //    }
        //    Data.MakeReverse(Data.data);//делаем преобразование

        //    result = openFileDialog.ShowDialog();
        //    if (result == DialogResult.OK)//потом отражение(во второй массив)
        //    {
        //        Data.OpenFile(openFileDialog.FileName, true);
        //    }
        //    //после этих операций в data все метаданные по отражению, как нам и нужно
        //    for (int z = 0; z < Data.depth; z++)
        //    {
        //        for (int x = 0; x < Data.width; x++)
        //        {
        //            for (int t = 0; t <= Data.maxtime; t++)
        //            {
        //                if ((Data.data[x, t, z] != 0) && (Data.dataSecondary[x, t, z] != 0))
        //                {
        //                    Data.data[x, t, z] += Math.Abs(Data.dataSecondary[x, t, z]);
        //                }
        //                else
        //                {
        //                    Data.data[x, t, z] = 0;
        //                }
        //            }
        //        }
        //    }

        //    Data.Save("curResult.data", Data.data, Data.maxtime);


        //    //в дальнейшем вынести графическую инициализация в отдельный метод
        //    //LoadNewSlice(0);
        //    //numericUpDownSlice.Maximum = Data.depth - 1;
        //    //numericUpDownX.Maximum = Data.width - 1;
        //    //numericUpDownY.Maximum = Data.maxtime;
        //    //numericUpDownWidth.Maximum = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;//Data.width - 1;

        //    //numericUpDownHeight.Maximum = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;//Data.maxtime;

        //    //dataWidth = Math.Min(Data.width - 1, (int)System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width);
        //    //dataHeight = Math.Min(Data.maxtime, (int)numericUpDownHeight.Maximum);

        //    //numericUpDownWidth.Value = dataWidth;
        //    //numericUpDownHeight.Value = dataHeight;
        //    InitiateViewSetting();

        //    DrawImage();

        //    InitiateViewSettingAfterDraw();
        //    //pictureBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pictureBox_MouseMove);//отображаем данные по координатам только после загрузки
        //}



        //private void Migration2D()
        //{
        //    //float sum = 0;
        //    //for (int x = 0; x < Data.width; x++)
        //    //{
        //    //    for (int y = 0; y < 1000; y++)
        //    //    {
        //    //        sum += data[x, y];
        //    //        if (data[x, y] != 0)
        //    //            Debug.WriteLine(x + "," + y + "," + data[x, y]);
        //    //    }
        //    //}


        //    DialogResult result = openFileDialogSpeedMap.ShowDialog();
        //    if (result == DialogResult.OK)//потом завернуть все это правильно
        //    {
        //        //if (!openFile(openFileDialog.FileName, true)) return;
        //        Data.OpenSpeedmap(openFileDialogSpeedMap.FileName);
        //    }

        //    float sumValue = 0;
        //    int curSpeed;

        //    int curTime;
        //    int accumulatedTime;
        //    int curY;

        //    float[,] temp = new float[Data.width, Data.maxtime + 1];

        //    int timeOut;

        //    int lastTime = -1;

        //    int calculatedY;

        //    //int lastBorder;

        //    //double distance;
        //    currentSlice = 80;

        //    for (int x = 0; x < Data.smWidth; x++)//для каждой трассы карты скоростей
        //    {
        //        //curTime = 0;
        //        curY = 0;
        //        accumulatedTime = 0;
        //        curSpeed = Data.speedMap[x, 0, currentSlice];//начальная скорость
        //        lastTime = -1;
        //        for (int y = 1; y < Data.smHeight; y++)
        //        {
        //            sumValue = 0;
        //            if (curSpeed != Data.speedMap[x, y, currentSlice])//проверить, правильно ли мы берем скорости
        //            {
        //                accumulatedTime += (int)(((double)y - curY) * Data.dy / curSpeed / Data.dt);
        //                curY = y;
        //            }
        //            curSpeed = Data.speedMap[x, y - 1, currentSlice];//взяли скорость из карты скоростей для данной пространственной точки(точнее, из прошлой)
        //            curTime = (int)(((double)y - curY) * Data.dy / curSpeed / Data.dt) + accumulatedTime;//рассчитываем время
        //            //Debug.WriteLine(y + "," + curSpeed + "," + curTime);

        //            //промежуточные строки по времени?
        //            for (int t = lastTime + 1; t < curTime; t++)//промежуточные времена(пропускаем основные)
        //            {
        //                calculatedY = (int)(((double)t * Data.dt) * curSpeed / Data.dy);
        //                sumValue = Migration2DCalc(new DifPoint(x, t, 0, calculatedY, curSpeed, (t - (calculatedY * Data.dy / curSpeed / Data.dt))), out timeOut);
        //                if (sumValue != 0)
        //                {
        //                    temp[x, timeOut] += sumValue;
        //                    Debug.WriteLine(calculatedY + "," + curSpeed + "," + timeOut);
        //                }
        //                //нужно расстояние, есть время и скорость(проверить, что происходит на границах сред)
        //            }
        //            lastTime = curTime;
        //            sumValue = Migration2DCalc(new DifPoint(x, curTime, 0, y, curSpeed, (curTime - (y * Data.dy / curSpeed / Data.dt))), out timeOut);
        //            //if (timeOut == 273 && sumValue != 0)
        //            //    timeOut = 273;
        //            if (sumValue != 0)
        //            {
        //                temp[x, timeOut] += sumValue;
        //                Debug.WriteLine(y + "," + curSpeed + "," + timeOut);
        //            }
        //        }
        //    }
        //    data = temp;
        //    DrawImage();
        //}

        //private void Migration3D()
        //{
        //    DialogResult result = openFileDialogSpeedMap.ShowDialog();
        //    if (result == DialogResult.OK)//потом завернуть все это правильно
        //    {
        //        //if (!openFile(openFileDialog.FileName, true)) return;
        //        Data.OpenSpeedmap(openFileDialogSpeedMap.FileName);
        //    }

        //    float sumValue = 0;
        //    int curSpeed;

        //    int curTime;
        //    int accumulatedTime;
        //    int curY;

        //    float[, ,] temp = new float[Data.width, Data.maxtime + 1, Data.depth];

        //    int timeOut;

        //    int lastTime = -1;

        //    int calculatedY;

        //    //int lastBorder;
            

        //    //double distance;
        //    for (int k = 0; k < Data.smDepth; k++)
        //    {
        //        for (int x = 0; x < Data.smWidth; x++)//для каждой трассы карты скоростей
        //        {
        //            //curTime = 0;
        //            curY = 0;
        //            accumulatedTime = 0;
        //            curSpeed = Data.speedMap[x, 0, k];//начальная скорость
        //            lastTime = -1;
        //            for (int y = 1; y < Data.smHeight; y++)
        //            {
        //                sumValue = 0;
        //                if (curSpeed != Data.speedMap[x, y, k])//проверить, правильно ли мы берем скорости
        //                {
        //                    accumulatedTime += (int)(((double)y - curY) * Data.dy / curSpeed / Data.dt);
        //                    curY = y;
        //                }
        //                curSpeed = Data.speedMap[x, y - 1, k];//взяли скорость из карты скоростей для данной пространственной точки(точнее, из прошлой)
        //                curTime = (int)(((double)y - curY) * Data.dy / curSpeed / Data.dt) + accumulatedTime;//рассчитываем время
        //                //Debug.WriteLine(y + "," + curSpeed + "," + curTime);

        //                //промежуточные строки по времени?
        //                for (int t = lastTime + 1; t < curTime; t++)//промежуточные времена(пропускаем основные)
        //                {
        //                    calculatedY = (int)(((double)t * Data.dt) * curSpeed / Data.dy);
        //                    sumValue = Migration3DCalc(new DifPoint(x, t, k, calculatedY, curSpeed, (t - (calculatedY * Data.dy / curSpeed / Data.dt))), out timeOut);
        //                    if (sumValue != 0)
        //                    {
        //                        temp[x, timeOut, k] += sumValue;
        //                        Debug.WriteLine(calculatedY + "," + curSpeed + "," + timeOut);
        //                    }
        //                    //нужно расстояние, есть время и скорость(проверить, что происходит на границах сред)
        //                }
        //                lastTime = curTime;
        //                sumValue = Migration3DCalc(new DifPoint(x, curTime, k, y, curSpeed, (curTime - (y * Data.dy / curSpeed / Data.dt))), out timeOut);
        //                if (sumValue != 0)
        //                {
        //                    temp[x, timeOut, k] += sumValue;
        //                    Debug.WriteLine(y + "," + curSpeed + "," + timeOut);
        //                }
        //            }
        //        }
        //    }
        //    Data.data = temp;
        //    //data = temp;
        //    DrawImage();
        //    Data.Save("migrated.data", temp, Data.maxtime);
        //    Console.Beep();
        //}

        //public struct DifPoint
        //{
        //    public int x;
        //    public int time;
        //    public int slice;
        //    //public double value;
        //    public int h;
        //    public int speed;
        //    public double delta;
        //    //public double coef;

        //    public DifPoint(int x_i, int time_i, int slice_i, /*double val_i, */int h_i, int speed_i, double delta_i)//, double coef_i)
        //    {
        //        x = x_i;
        //        time = time_i;
        //        slice = slice_i;
        //        //value = val_i;
        //        h = h_i;
        //        speed = speed_i;
        //        delta = delta_i;
        //        //coef = coef_i;
        //    }
        //}

        //private float Migration2DCalc(DifPoint point, out int timeOut)
        //{
        //    float difSummary = 0;

        //    int difx, difk, difsum;
        //    int time;
        //    timeOut = -1;

        //    double fcoef = (double)1 / point.speed / Data.dt;
        //    double correctedTime = point.delta + point.time;
        //    int h2 = point.h * point.h;

        //    double distance, R;
        //    //for (int k = 0; k <= difRadius; k++)
        //    //{
        //    for (int x = 0; x <= Data.difRadius; x++)
        //    {
        //        difx = x * x;
        //        //difk = k * k;
        //        difsum = difx;// +difk;
        //        if (difsum > Data.difRadius * Data.difRadius) continue;//точка вне заданной окружности

        //        R = Math.Sqrt(difsum);//отклонение от центра
        //        distance = Math.Sqrt(difsum * Data.dx * Data.dx + h2 * Data.dy * Data.dy);//расстояние до приёмника(по x и z считаем шаг одинаковым) в метрах
        //        time = (int)(distance * fcoef + correctedTime);//считаем суммарное время
        //        if (time > Data.maxtime)
        //            continue;
        //        if (difsum == 0)//сама точка дифракции
        //        {
        //            difSummary += Data.data[point.x, time, point.slice];//нужно ли?
        //            timeOut = time;
        //            //data[point.x, time, point.slice] += (float)(point.value * point.coef / 2);//"=0.5*коэф.отраж"
        //        }
        //        else if (time <= Data.maxtime)//отсекаем превышения по времени
        //        {
        //            //difValue = (float)(point.value * point.coef / 2 / R);// * (difRadius - R) / difRadius);//???
        //            if (point.x + x < Data.width)
        //            {
        //                //if (point.slice + k < depth)
        //                //{
        //                difSummary += Data.data[point.x + x, time, point.slice /*+ k*/];
        //                //data[point.x + x, time, point.slice /*+ k*/] += difValue;
        //                //}
        //                //if (point.slice - k >= 0)
        //                //{
        //                //    data[point.x + x, time, point.slice - k] += difValue;
        //                //}
        //            }
        //            if (point.x - x >= 0)
        //            {
        //                //if (point.slice + k < depth)
        //                //{
        //                difSummary += Data.data[point.x - x, time, point.slice /*+ k*/];
        //                //    data[point.x - x, time, point.slice + k] += difValue;
        //                //}
        //                //if (point.slice - k >= 0)
        //                //{
        //                //    data[point.x - x, time, point.slice - k] += difValue;
        //                //}
        //            }
        //        }
        //        //}
        //    }
        //    return difSummary;
        //}
        //private float Migration3DCalc(DifPoint point, out int timeOut)
        //{
        //    float difSummary = 0;

        //    int difx, difk, difsum;
        //    int time;
        //    timeOut = -1;

        //    double fcoef = (double)1 / point.speed / Data.dt;
        //    double correctedTime = point.delta + point.time;
        //    int h2 = point.h * point.h;

        //    double distance, R;
        //    for (int k = 0; k <= Data.difRadius; k++)
        //    {
        //        for (int x = 0; x <= Data.difRadius; x++)
        //        {
        //            difx = x * x;
        //            difk = k * k;
        //            difsum = difx + difk;
        //            if (difsum > Data.difRadius * Data.difRadius) continue;//точка вне заданной окружности

        //            R = Math.Sqrt(difsum);//отклонение от центра
        //            distance = Math.Sqrt(difsum * Data.dx * Data.dx + h2 * Data.dy * Data.dy);//расстояние до приёмника(по x и z считаем шаг одинаковым) в метрах
        //            time = (int)(distance * fcoef + correctedTime);//считаем суммарное время
        //            if (time > Data.maxtime)
        //                continue;
        //            if (difsum == 0)//сама точка дифракции
        //            {
        //                difSummary += Data.data[point.x, time, point.slice];//нужно ли?
        //                timeOut = time;
        //            }
        //            else if (time <= Data.maxtime)//отсекаем превышения по времени
        //            {
        //                if (point.x + x < Data.width)
        //                {
        //                    if (point.slice + k < Data.depth)
        //                    {
        //                        difSummary += Data.data[point.x + x, time, point.slice + k];
        //                        //data[point.x + x, time, point.slice /*+ k*/] += difValue;
        //                    }
        //                    if (point.slice - k >= 0)
        //                    {
        //                        difSummary += Data.data[point.x + x, time, point.slice - k];
        //                        //    data[point.x + x, time, point.slice - k] += difValue;
        //                    }
        //                }
        //                if (point.x - x >= 0)
        //                {
        //                    if (point.slice + k < Data.depth)
        //                    {
        //                        difSummary += Data.data[point.x - x, time, point.slice + k];
        //                        //    data[point.x - x, time, point.slice + k] += difValue;
        //                    }
        //                    if (point.slice - k >= 0)
        //                    {
        //                        difSummary += Data.data[point.x - x, time, point.slice - k];
        //                        //    data[point.x - x, time, point.slice - k] += difValue;
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    return difSummary;
        //}

        //private void migrationToolStripMenuItem_Click(object sender, EventArgs e)
        //{
        //    //Migration2D();
        //}

        

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Data.useDeconvolvedData = ((CheckBox)sender).Checked;
            Data.RefreshSlicesData();
            LoadNewSlice(currentSlice);
            DrawImage();
        }

        private void открытьToolStripMenuItem_Click(object sender, EventArgs e)
        {

            DialogResult result = openFileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                if (!openFile(openFileDialog.FileName)) return;
            }
        }

        private void сохранитьToolStripMenuItem_Click(object sender, EventArgs e)
        {

            saveFileDialog1.FileName = "img";
            saveFileDialog1.DefaultExt = "jpg";
            saveFileDialog1.Filter = "JPG images (*.jpg)|*.jpg";
            Bitmap img = curImg.Bitmap;
            if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK && img != null)
            {
                var fileName = saveFileDialog1.FileName;

                img.Save(fileName, System.Drawing.Imaging.ImageFormat.Jpeg);
            }
        }



        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            RotateImg(90);
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            RotateImg(180);
        }

        private void RotateImg(double grad)
        {
            ImgEdit(() => curImg.RotationGrad = grad);


            toolStripMenuItem5.Checked = false;
            toolStripMenuItem2.Checked = false;
            toolStripMenuItem3.Checked = false;
            toolStripMenuItem4.Checked = false;
            выбратьToolStripMenuItem.Checked = false;

            if (curImg.RotationGrad == 0)
            {
                toolStripMenuItem5.Checked = true;
            }
            else if (curImg.RotationGrad == 90)
            {
                toolStripMenuItem2.Checked = true;
            }
            else if (curImg.RotationGrad == 180)
            {
                toolStripMenuItem3.Checked = true;
            }
            else if (curImg.RotationGrad == 270)
            {
                toolStripMenuItem4.Checked = true;
            }
            else
            {
                выбратьToolStripMenuItem.Checked = true;
            }
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            RotateImg(270);
        }

        private void выбратьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            double grad;
            string input = Interaction.InputBox("Поворот в градусах", "Трансформация изображения", curImg.RotationGrad.ToString());
            if (!Double.TryParse(input, out grad))
            {
                grad = 0;
            }
            RotateImg(grad);
        }

        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            RotateImg(0);
        }

        private void ScaleImg(double scaleX, double scaleY)
        {
            ImgEdit(() => {
                curImg.ScaleX = scaleX;
                curImg.ScaleY = scaleY;
            });
        } 

        private void масштабToolStripMenuItem_Click(object sender, EventArgs e)
        {
            double scaleX;
            string input = Interaction.InputBox("Введите масштаб по X", "Трансформация изображения", curImg.ScaleX.ToString());
            if (!Double.TryParse(input, out scaleX))
            {
                scaleX = 1;
            }
            double scaleY;
            input = Interaction.InputBox("Введите масштаб по Y", "Трансформация изображения", curImg.ScaleY.ToString());
            if (!Double.TryParse(input, out scaleY))
            {
                scaleY = 1;
            }
            ScaleImg(scaleX, scaleY);
        }

        private void безОтраженияToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FlipImg(false, false);
        }

        private void xToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FlipImg(true, false);
        }

        private void yToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FlipImg(false, true);
        }

        private void xToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            double scale;
            string input = Interaction.InputBox("Введите масштаб по X", "Трансформация изображения", curImg.ScaleX.ToString());
            if (!Double.TryParse(input, out scale))
            {
                scale = 1;
            }
            ScaleImg(scale, 1);
        }

        private void yToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            double scale;
            string input = Interaction.InputBox("Введите масштаб по Y", "Трансформация изображения", curImg.ScaleY.ToString());
            if (!Double.TryParse(input, out scale))
            {
                scale = 1;
            }
            ScaleImg(1, scale);
        }

        private void SetFormingTypeMenu(SliceFormingType type)
        {
            нулевойСдвигToolStripMenuItem.Checked = false;
            общийПунктВозбужденияToolStripMenuItem.Checked = false;
            общийПунктПриемаToolStripMenuItem.Checked = false;
            общаяСредняяТочкаToolStripMenuItem.Checked = false;

            switch(type)
            {
                case SliceFormingType.ns:
                    нулевойСдвигToolStripMenuItem.Checked = true;
                    break;
                case SliceFormingType.opv:
                    общийПунктВозбужденияToolStripMenuItem.Checked = true;
                    break;
                case SliceFormingType.opp:
                    общийПунктПриемаToolStripMenuItem.Checked = true;
                    break;
                case SliceFormingType.ost:
                    общаяСредняяТочкаToolStripMenuItem.Checked = true;
                    break;
            }
        }

        private void нулевойСдвигToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Data.RefreshSlicesData(SliceFormingType.ns);
            LoadNewSlice();
            DrawImage();
            SetFormingTypeMenu(SliceFormingType.ns);
        }

        private void общийПунктВозбужденияToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Data.RefreshSlicesData(SliceFormingType.opv);
            LoadNewSlice();
            DrawImage();
            SetFormingTypeMenu(SliceFormingType.opv);
        }

        private void общийПунктПриемаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Data.RefreshSlicesData(SliceFormingType.opp);
            LoadNewSlice();
            DrawImage();
            SetFormingTypeMenu(SliceFormingType.opp);
        }

        private void общаяСредняяТочкаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Data.RefreshSlicesData(SliceFormingType.ost);
            LoadNewSlice();
            DrawImage();
            SetFormingTypeMenu(SliceFormingType.ost);
        }

        private void среднееToolStripMenuItem_Click(object sender, EventArgs e)
        {
            curGroupStatus = GroupStatus.avg;
            LoadNewSlice();
            DrawImage();
        }

        private void суммаToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            curGroupStatus = GroupStatus.sum;
            LoadNewSlice();
            DrawImage();
        }

        private void осветлитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            double bright;
            string input = Interaction.InputBox("Введите множитель осветления", "Изменение изображения", curImg.Bright.ToString());
            if (!Double.TryParse(input, out bright))
            {
                return;
            }
            ImgEdit(() => curImg.Bright = bright);
        }

        private void слойToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            data = Data.Migration(data);
            DrawImage();
            MessageBox.Show("Готово");
        }

        private void всеToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            Data.MigrationAll();
            LoadNewSlice(currentSlice);
            DrawImage();
            MessageBox.Show("Готово");
        }

        private void деконволюцияToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void обновитьДеконволюциюToolStripMenuItem_Click(object sender, EventArgs e)
        {
            double a;
            string input = Interaction.InputBox("Введите параметр a", "Деконволюция", Data.curDecA.ToString());
            if (!Double.TryParse(input, out a))
            {
                return;
            }
            Data.curDecA = a;
            Data.DeconvolveAllTraces();
            Data.RefreshSlicesData();
            LoadNewSlice(currentSlice);
            DrawImage();
        }

        private void фНЧToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            string input;
            input = Interaction.InputBox("Введите частоту среза", "ФНЧ построчно", "0");
            double fcut;
            if (!Double.TryParse(input, out fcut))
            {
                return;
            }
            if (fcut > 0)
            {
                input = Interaction.InputBox("Введите m", "ФНЧ построчно", "32");
                int m;
                if (!Int32.TryParse(input, out m))
                {
                    return;
                }
                var filter = FuncProc.Lpf((float)fcut, m);

                var modalFuncs1 = new TrendModal(FuncProc.GetDiscreteSpectre(filter));
                modalFuncs1.Show();


                var msgBoxResult = Interaction.MsgBox("На все данные?", MsgBoxStyle.YesNo);
                switch (msgBoxResult)
                {
                    case MsgBoxResult.Yes:
                        Data.WholeApplyFilter(filter);
                        LoadNewSlice();
                        break;
                    case MsgBoxResult.No:
                        data = FuncProc.ApplyFilter(data, filter);
                        break;
                }
                DrawImage();
            }
        }

        private void фВЧToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            string input;
            input = Interaction.InputBox("Введите частоту среза", "ФВЧ построчно", "");
            double fcut;
            if (!Double.TryParse(input, out fcut))
            {
                fcut = 0;
            }
            if (fcut > 0)
            {
                input = Interaction.InputBox("Введите m", "ФВЧ построчно", "");
                int m;
                if (!Int32.TryParse(input, out m))
                {
                    m = 32;
                }

                var filter = FuncProc.Hpf((float)fcut, m);

                var modalFuncs1 = new TrendModal(FuncProc.GetDiscreteSpectre(filter));
                modalFuncs1.Show();


                var msgBoxResult = Interaction.MsgBox("На все данные?", MsgBoxStyle.YesNo);
                switch (msgBoxResult)
                {
                    case MsgBoxResult.Yes:
                        Data.WholeApplyFilter(filter);
                        LoadNewSlice();
                        break;
                    case MsgBoxResult.No:
                        data = FuncProc.ApplyFilter(data, filter);
                        break;
                }
                DrawImage();
            }
        }

        private void xToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            string input;
            input = Interaction.InputBox("Введите номер столбца", "", "");
            int col;
            if (!Int32.TryParse(input, out col))
            {
                col = -1;
            }
            if (col >= 0 && Data.width > col)
            {
                var func = new float[data.GetLength(1)];
                for (int j = 0; j < func.Length; j++)
                {
                    func[j] = curImg.GetValue(new Tuple<int, int>(col, j));
                }

                var funcs = new Dictionary<string, float[]>();
                var spectr = FuncProc.GetDiscreteSpectre(func);
                funcs.Add("Спектр", spectr);

                var modalFuncs = new TrendModal(funcs);
                modalFuncs.Show();
            }
        }

        private void timeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string input;
            input = Interaction.InputBox("Введите номер строки", "", "");
            int row;
            if (!Int32.TryParse(input, out row))
            {
                row = -1;
            }
            if (row >= 0 && Data.maxtime > row)
            {
                var func = new float[data.GetLength(0)];
                for (int i = 0; i < func.Length; i++)
                {
                    func[i] = curImg.GetValue(new Tuple<int, int>(i, row));
                }

                var funcs = new Dictionary<string, float[]>();
                var spectr = FuncProc.GetDiscreteSpectre(func);
                funcs.Add("Спектр", spectr);

                var modalFuncs = new TrendModal(funcs);
                modalFuncs.Show();
            }
        }
        
        private bool ImgEdit(Action action)
        {
            var old = curImg.Clone();
            action();
            if (curImg.Bitmap != null)
            {
                imgBox.Image = curImg.Bitmap;
                imgBox.Size = imgBox.Image.Size;
                return true;
            }
            else
            {
                curImg = old;
                return false;
            }
        }

        private void сольПерецToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Data.curNoiseType = FuncProc.NoiseType.sp;
        }

        private void гаусToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Data.curNoiseType = FuncProc.NoiseType.sp;
        }

        private void убратьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Data.curNoiseType = FuncProc.NoiseType.none;
        }

        private void показатьСигналToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            data = new float[1, Data.signal.Length];
            for (int i = 0; i < Data.signal.Length; i++)
            {
                data[0, i] = Data.signal[i];
            }
            var modalFuncs1 = new TrendModal(Data.signal);
            modalFuncs1.Show();
            DrawImage();
        }

        private void всеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Data.curNoiseType = FuncProc.NoiseType.rnd;
            Data.AddNoise();
            LoadNewSlice();
            DrawImage();
        }

        private void слойToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Data.curNoiseType = FuncProc.NoiseType.rnd;
            data = FuncProc.AddNoise(data, Data.curNoiseType);
            DrawImage();
        }

        private void всеToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            Data.curNoiseType = FuncProc.NoiseType.sp;
            Data.AddNoise();
            LoadNewSlice();
            DrawImage();
        }

        private void слойToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            Data.curNoiseType = FuncProc.NoiseType.sp;
            data = FuncProc.AddNoise(data, Data.curNoiseType);
            DrawImage();
        }

        private void слойToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            string input;
            input = Interaction.InputBox("Введите скорость", "", "-1");
            int speed;
            if (!Int32.TryParse(input, out speed))
            {
                return;
            }
            data = Data.KinematicFixes(data, currentSlice, speed);
            DrawImage();
            MessageBox.Show("Готово");
        }

        private void всеToolStripMenuItem4_Click(object sender, EventArgs e)
        {
            string input;
            input = Interaction.InputBox("Введите скорость", "", "-1");
            int speed;
            if (!Int32.TryParse(input, out speed))
            {
                return;
            }
            Data.KinematicFixesAll(speed);
            LoadNewSlice();
            DrawImage();
            MessageBox.Show("Готово");
        }

        private void поТрассамToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Data.KinematicFixesTraces();
            LoadNewSlice();
            DrawImage();
        }

        private void вклвыклToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var cb = (ToolStripMenuItem)sender;
            cb.Checked = !cb.Checked;
            Data.useDeconvolvedData = cb.Checked;
            Data.RefreshSlicesData();
            LoadNewSlice(currentSlice);
            DrawImage();
        }

        private void вертиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string input;
            input = Interaction.InputBox("Введите dV", "", "100");
            int dV;
            if (!Int32.TryParse(input, out dV))
            {
                return;
            }
            data = Data.CreateSpeedMatrix(data, currentSlice, dV, 10000);
            var maxData = new float[Data.maxtime];
            for (int t = 0; t < Data.maxtime; t++)
            {
                var max = (float)Double.NegativeInfinity;
                //float sum = 0;
                //float weight = 0;
                for (int i = 0; i < data.GetLength(0); i++)
                {
                    //sum += i * dV * data[i, t];
                    //weight += data[i, t];
                    if (data[i, t] > max)
                    {
                        max = data[i, t];
                        maxData[t] = i * dV;
                    }
                }
                //maxData[t] = sum / weight;
            }
            //maxData = FuncProc.AntiSpike(maxData);
            TrendModal.CreateAndShow(maxData);
            DrawImage(false);
        }

        private void всеToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            string input;
            input = Interaction.InputBox("Введите dV", "", "100");
            int dV;
            if (!Int32.TryParse(input, out dV))
            {
                return;
            }
            data = Data.CreateSpeedMatrixAll(dV, 10000);
            DrawImage();
        }

        private void спайкиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string input;
            input = Interaction.InputBox("Введите верхнюю границу (оставьте пустым для автоопределения)", "", "");
            double acut;
            if (!Double.TryParse(input, out acut))
            {
                acut = Double.PositiveInfinity;
            }

        }

        private void всеИзображениеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            data = FuncProc.GetDiscreteSpectre(data);
            DrawImage();
        }

        private void оПВToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string input;
            input = Interaction.InputBox("Введите скорость", "", "-1");
            int speed;
            if (!Int32.TryParse(input, out speed))
            {
                return;
            }
            data = Data.MigrationOpv(data, currentSlice, speed);
            DrawImage();
            MessageBox.Show("Готово");
        }

        private void оПВВсеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string input;
            input = Interaction.InputBox("Введите скорость", "", "-1");
            int speed;
            if (!Int32.TryParse(input, out speed))
            {
                return;
            }
            Data.MigrationOpvAll(speed);
            LoadNewSlice();
            DrawImage();
            MessageBox.Show("Готово");
        }

        private void переводВДальностьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            data = Data.TimeToDist(data);
            DrawImage();
            ScaleImg(1, 1);
        }

        private void показатьSpeedmapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            data = Data.GetFloatSpeedMap();
            DrawImage();
            ScaleImg(1, 1);
        }

        private void линейноToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string input;
            input = Interaction.InputBox("Введите коэф", "", "1");
            double k;
            if (!Double.TryParse(input, out k))
            {
                return;
            }
            var msgBoxResult = Interaction.MsgBox("На все данные?", MsgBoxStyle.YesNo);
            switch (msgBoxResult)
            {
                case MsgBoxResult.Yes:
                    Data.WholeAmplify(AmplifyType.linear, k);
                    LoadNewSlice();
                    break;
                case MsgBoxResult.No:
                    data = Data.Amplify(data, AmplifyType.linear, k);
                    break;
            }
            DrawImage();
        }

        private void логарифмическиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string input;
            input = Interaction.InputBox("Введите основание логарифма", "", "1");
            double k;
            if (!Double.TryParse(input, out k))
            {
                return;
            }
            var msgBoxResult = Interaction.MsgBox("На все данные?", MsgBoxStyle.YesNo);
            switch (msgBoxResult)
            {
                case MsgBoxResult.Yes:
                    Data.WholeAmplify(AmplifyType.log, k);
                    LoadNewSlice();
                    break;
                case MsgBoxResult.No:
                    data = Data.Amplify(data, AmplifyType.log, k);
                    break;
            }
            DrawImage();
        }

        private void экспоненциальноToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string input;
            input = Interaction.InputBox("Введите основание степени", "", "1");
            double k;
            if (!Double.TryParse(input, out k))
            {
                return;
            }
            var msgBoxResult = Interaction.MsgBox("На все данные?", MsgBoxStyle.YesNo);
            switch (msgBoxResult)
            {
                case MsgBoxResult.Yes:
                    Data.WholeAmplify(AmplifyType.pow, k);
                    LoadNewSlice();
                    break;
                case MsgBoxResult.No:
                    data = Data.Amplify(data, AmplifyType.pow, k);
                    break;
            }
            DrawImage();
        }

        private void поУмолчаниюToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ScaleImg(scX, scY);
        }

        private void сохранитьДанныеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog2.FileName = Data.modelName;
            saveFileDialog2.DefaultExt = "info";
            saveFileDialog2.Filter = "Сонограммы (*.info)|*.info";
            if (saveFileDialog2.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var fileName = saveFileDialog2.FileName;
                string modelName = fileName.Substring(0, fileName.IndexOf(".info"));

                for (int z = 0; z < Data.numberOfSonograms; z++)
                {
                    Data.saveDataFile(Data.slices[z], modelName + "_" + z + ".data");
                }
                Data.saveInfoFile(fileName, Data.numberOfSonograms);
                Data.saveSpeedmap(Path.GetDirectoryName(fileName) + "\\speedmap.bmp");
            }
            MessageBox.Show("Сохранено");
        }

        private void xYToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FlipImg(true, true);
        }

        private void FlipImg(bool flipX, bool flipY)
        {
            ImgEdit(() =>
            {
                curImg.FlipX = flipX;
                curImg.FlipY = flipY;
            });

            if (curImg.FlipX && curImg.FlipY)
            {
                безОтраженияToolStripMenuItem.Checked = false;
                xYToolStripMenuItem.Checked = true;
                xToolStripMenuItem.Checked = false;
                yToolStripMenuItem.Checked = false;
            }
            else if(curImg.FlipX || curImg.FlipY)
            {
                безОтраженияToolStripMenuItem.Checked = false;
                xYToolStripMenuItem.Checked = false;
                xToolStripMenuItem.Checked = curImg.FlipX;
                yToolStripMenuItem.Checked = curImg.FlipY;
            }
            else
            {
                безОтраженияToolStripMenuItem.Checked = true;
                xYToolStripMenuItem.Checked = false;
                xToolStripMenuItem.Checked = false;
                yToolStripMenuItem.Checked = false;
            }
        }

        private void лимитToolStripMenuItem_Click(object sender, EventArgs e)
        {
            double limit;
            string input = Interaction.InputBox("Введите разряд амплитуры, меньше которого информация отсеится", "Фильтрация изображения", curImg.LimitPow.ToString());
            if (!Double.TryParse(input, out limit))
            {
                return;
            }
            ImgEdit(() => curImg.LimitPow = limit);
        }
    }

}

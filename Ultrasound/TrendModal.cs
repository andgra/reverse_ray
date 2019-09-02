using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Ultrasound
{
    public partial class TrendModal : Form
    {
        private void Trend(Series ser, float[] col)
        {
            ser.Points.Clear();
            for (int i = 0; i < col.Length; i++)
            {
                ser.Points.AddXY(i, col[i]);
            }
        }

        public TrendModal(float[] func, SeriesChartType type = SeriesChartType.Line)
        {
            InitializeComponent();

            ch.Series.Add("Func");
            ch.Series[0].ChartType = type;
            Trend(ch.Series[0], func);
        }
        public TrendModal(Dictionary<string, float[]> funcList, SeriesChartType type = SeriesChartType.Line)
        {
            InitializeComponent();

            int i = 0;
            foreach (var kp in funcList)
            {
                ch.Series.Add(kp.Key);
                ch.Series[i].ChartType = type;
                Trend(ch.Series[i], kp.Value);
                i++;
            }
        }

        public static TrendModal CreateAndShow(float[] func, SeriesChartType type = SeriesChartType.Line)
        {
            var modal = new TrendModal(func, type);
            modal.Show();
            return modal;
        }

        public static TrendModal CreateAndShow(Dictionary<string, float[]> funcList, SeriesChartType type = SeriesChartType.Line)
        {
            var modal = new TrendModal(funcList, type);
            modal.Show();
            return modal;
        }

        public static void chData_MouseWheel(object sender, MouseEventArgs e)
        {
            try
            {
                System.Windows.Forms.DataVisualization.Charting.Chart ch = (System.Windows.Forms.DataVisualization.Charting.Chart)sender;
                ch.ChartAreas[0].AxisX.ScaleView.Zoomable = true;
                if (e.Delta < 0)
                {
                    ch.ChartAreas[0].AxisX.ScaleView.ZoomReset();
                    ch.ChartAreas[0].AxisY.ScaleView.ZoomReset();
                }

                if (e.Delta > 0)
                {
                    double xMin = ch.ChartAreas[0].AxisX.ScaleView.ViewMinimum;
                    double xMax = ch.ChartAreas[0].AxisX.ScaleView.ViewMaximum;
                    double yMin = ch.ChartAreas[0].AxisY.ScaleView.ViewMinimum;
                    double yMax = ch.ChartAreas[0].AxisY.ScaleView.ViewMaximum;

                    double posXStart = ch.ChartAreas[0].AxisX.PixelPositionToValue(e.Location.X) - (xMax - xMin) / 4;
                    double posXFinish = ch.ChartAreas[0].AxisX.PixelPositionToValue(e.Location.X) + (xMax - xMin) / 4;
                    double posYStart = ch.ChartAreas[0].AxisY.PixelPositionToValue(e.Location.Y) - (yMax - yMin) / 4;
                    double posYFinish = ch.ChartAreas[0].AxisY.PixelPositionToValue(e.Location.Y) + (yMax - yMin) / 4;

                    ch.ChartAreas[0].AxisX.ScaleView.Zoom(posXStart, posXFinish);
                    ch.ChartAreas[0].AxisY.ScaleView.Zoom(posYStart, posYFinish);
                }
            }
            catch { }
        }
    }
}

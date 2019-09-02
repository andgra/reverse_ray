namespace Ultrasound
{
    partial class TrendModal
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            this.ch = new System.Windows.Forms.DataVisualization.Charting.Chart();
            ((System.ComponentModel.ISupportInitialize)(this.ch)).BeginInit();
            this.SuspendLayout();
            // 
            // ch
            // 
            chartArea1.Name = "ChartArea1";
            this.ch.ChartAreas.Add(chartArea1);
            legend1.Name = "Legend1";
            this.ch.Legends.Add(legend1);
            this.ch.Location = new System.Drawing.Point(16, 15);
            this.ch.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.ch.Name = "ch";
            this.ch.Size = new System.Drawing.Size(905, 608);
            this.ch.TabIndex = 11;
            this.ch.Text = "chart1";
            this.ch.MouseWheel += chData_MouseWheel;
            //this.ch.MouseWheel += MPED.chData_MouseWheel;
            // 
            // TrendModal
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(937, 638);
            this.Controls.Add(this.ch);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "TrendModal";
            this.Text = "TrendModal";
            ((System.ComponentModel.ISupportInitialize)(this.ch)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataVisualization.Charting.Chart ch;
    }
}
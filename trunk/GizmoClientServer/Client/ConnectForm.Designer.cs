namespace Client
{
    partial class ConnectForm
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
            this.butConnect = new System.Windows.Forms.Button();
            this.tbIp = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.numericUpDownPort = new System.Windows.Forms.NumericUpDown();
            this.tbName = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.btnRecieveBroadcastAddr = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.cmbClientIp = new System.Windows.Forms.ComboBox();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPort)).BeginInit();
            this.SuspendLayout();
            // 
            // butConnect
            // 
            this.butConnect.Location = new System.Drawing.Point(116, 189);
            this.butConnect.Name = "butConnect";
            this.butConnect.Size = new System.Drawing.Size(93, 23);
            this.butConnect.TabIndex = 0;
            this.butConnect.Text = "Подключиться";
            this.butConnect.UseVisualStyleBackColor = true;
            this.butConnect.Click += new System.EventHandler(this.butConnect_Click);
            // 
            // tbIp
            // 
            this.tbIp.Location = new System.Drawing.Point(39, 148);
            this.tbIp.Name = "tbIp";
            this.tbIp.Size = new System.Drawing.Size(202, 20);
            this.tbIp.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(39, 129);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(17, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "IP";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(244, 129);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(32, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Порт";
            // 
            // numericUpDownPort
            // 
            this.numericUpDownPort.Location = new System.Drawing.Point(248, 147);
            this.numericUpDownPort.Maximum = new decimal(new int[] {
            65534,
            0,
            0,
            0});
            this.numericUpDownPort.Name = "numericUpDownPort";
            this.numericUpDownPort.Size = new System.Drawing.Size(54, 20);
            this.numericUpDownPort.TabIndex = 3;
            // 
            // tbName
            // 
            this.tbName.Location = new System.Drawing.Point(36, 48);
            this.tbName.Name = "tbName";
            this.tbName.Size = new System.Drawing.Size(202, 20);
            this.tbName.TabIndex = 1;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(36, 32);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(60, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "Ваше имя:";
            // 
            // btnRecieveBroadcastAddr
            // 
            this.btnRecieveBroadcastAddr.Location = new System.Drawing.Point(39, 83);
            this.btnRecieveBroadcastAddr.Name = "btnRecieveBroadcastAddr";
            this.btnRecieveBroadcastAddr.Size = new System.Drawing.Size(199, 23);
            this.btnRecieveBroadcastAddr.TabIndex = 0;
            this.btnRecieveBroadcastAddr.Text = "Определить адрес автоматически";
            this.btnRecieveBroadcastAddr.UseVisualStyleBackColor = true;
            this.btnRecieveBroadcastAddr.Click += new System.EventHandler(this.btnRecieveBroadcastAddr_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(282, 32);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(67, 13);
            this.label4.TabIndex = 2;
            this.label4.Text = "Интерфейс:";
            // 
            // cmbClientIp
            // 
            this.cmbClientIp.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbClientIp.FormattingEnabled = true;
            this.cmbClientIp.Location = new System.Drawing.Point(285, 48);
            this.cmbClientIp.Name = "cmbClientIp";
            this.cmbClientIp.Size = new System.Drawing.Size(162, 21);
            this.cmbClientIp.TabIndex = 4;
            // 
            // ConnectForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(479, 227);
            this.Controls.Add(this.cmbClientIp);
            this.Controls.Add(this.numericUpDownPort);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tbName);
            this.Controls.Add(this.tbIp);
            this.Controls.Add(this.btnRecieveBroadcastAddr);
            this.Controls.Add(this.butConnect);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "ConnectForm";
            this.Text = "Подключение к серверу...";
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownPort)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button butConnect;
        public System.Windows.Forms.TextBox tbIp;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        public System.Windows.Forms.NumericUpDown numericUpDownPort;
        public System.Windows.Forms.TextBox tbName;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnRecieveBroadcastAddr;
        private System.Windows.Forms.Label label4;
        public System.Windows.Forms.ComboBox cmbClientIp;
    }
}
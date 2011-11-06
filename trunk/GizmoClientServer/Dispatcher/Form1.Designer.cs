namespace Dispatcher
{
    partial class Form1
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.lbMsgServers = new System.Windows.Forms.ListBox();
            this.lbFileServers = new System.Windows.Forms.ListBox();
            this.lbFiles = new System.Windows.Forms.ListBox();
            this.lbClients = new System.Windows.Forms.ListBox();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.tbLog = new System.Windows.Forms.TextBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.lbMsgServers);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(218, 238);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Сервера сообщений";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.lbFileServers);
            this.groupBox2.Location = new System.Drawing.Point(236, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(218, 238);
            this.groupBox2.TabIndex = 0;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Сервера файлов";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.lbFiles);
            this.groupBox3.Location = new System.Drawing.Point(460, 12);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(218, 238);
            this.groupBox3.TabIndex = 0;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Файлы";
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.lbClients);
            this.groupBox4.Location = new System.Drawing.Point(684, 12);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(218, 238);
            this.groupBox4.TabIndex = 0;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Клиенты";
            // 
            // lbMsgServers
            // 
            this.lbMsgServers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbMsgServers.FormattingEnabled = true;
            this.lbMsgServers.Location = new System.Drawing.Point(3, 16);
            this.lbMsgServers.Name = "lbMsgServers";
            this.lbMsgServers.Size = new System.Drawing.Size(212, 219);
            this.lbMsgServers.TabIndex = 0;
            // 
            // lbFileServers
            // 
            this.lbFileServers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbFileServers.FormattingEnabled = true;
            this.lbFileServers.Location = new System.Drawing.Point(3, 16);
            this.lbFileServers.Name = "lbFileServers";
            this.lbFileServers.Size = new System.Drawing.Size(212, 219);
            this.lbFileServers.TabIndex = 0;
            // 
            // lbFiles
            // 
            this.lbFiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbFiles.FormattingEnabled = true;
            this.lbFiles.Location = new System.Drawing.Point(3, 16);
            this.lbFiles.Name = "lbFiles";
            this.lbFiles.Size = new System.Drawing.Size(212, 219);
            this.lbFiles.TabIndex = 0;
            // 
            // lbClients
            // 
            this.lbClients.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbClients.FormattingEnabled = true;
            this.lbClients.Location = new System.Drawing.Point(3, 16);
            this.lbClients.Name = "lbClients";
            this.lbClients.Size = new System.Drawing.Size(212, 219);
            this.lbClients.TabIndex = 0;
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.tbLog);
            this.groupBox5.Location = new System.Drawing.Point(12, 256);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(887, 206);
            this.groupBox5.TabIndex = 1;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Лог";
            // 
            // tbLog
            // 
            this.tbLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbLog.Location = new System.Drawing.Point(3, 16);
            this.tbLog.Multiline = true;
            this.tbLog.Name = "tbLog";
            this.tbLog.Size = new System.Drawing.Size(881, 187);
            this.tbLog.TabIndex = 0;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(912, 474);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "Диспетчер";
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox4.ResumeLayout(false);
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ListBox lbMsgServers;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.ListBox lbFileServers;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.ListBox lbFiles;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.ListBox lbClients;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.TextBox tbLog;

    }
}


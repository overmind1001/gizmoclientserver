namespace FileServer
{
    partial class FileServerForm
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
            this.m_mainMenuStrip = new System.Windows.Forms.MenuStrip();
            this.m_menuServer = new System.Windows.Forms.ToolStripMenuItem();
            this.m_menuRun = new System.Windows.Forms.ToolStripMenuItem();
            this.m_menuStop = new System.Windows.Forms.ToolStripMenuItem();
            this.m_statusStrip = new System.Windows.Forms.StatusStrip();
            this.m_statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.m_tab = new System.Windows.Forms.TabControl();
            this.m_pageFiles = new System.Windows.Forms.TabPage();
            this.m_tabLog = new System.Windows.Forms.TabPage();
            this.m_mainMenuStrip.SuspendLayout();
            this.m_statusStrip.SuspendLayout();
            this.m_tab.SuspendLayout();
            this.SuspendLayout();
            // 
            // m_mainMenuStrip
            // 
            this.m_mainMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menuServer});
            this.m_mainMenuStrip.Location = new System.Drawing.Point(0, 0);
            this.m_mainMenuStrip.Name = "m_mainMenuStrip";
            this.m_mainMenuStrip.Size = new System.Drawing.Size(433, 24);
            this.m_mainMenuStrip.TabIndex = 0;
            this.m_mainMenuStrip.Text = "menuStrip1";
            // 
            // m_menuServer
            // 
            this.m_menuServer.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menuRun,
            this.m_menuStop});
            this.m_menuServer.Name = "m_menuServer";
            this.m_menuServer.Size = new System.Drawing.Size(59, 20);
            this.m_menuServer.Text = "Сервер";
            // 
            // m_menuRun
            // 
            this.m_menuRun.Name = "m_menuRun";
            this.m_menuRun.Size = new System.Drawing.Size(152, 22);
            this.m_menuRun.Text = "Поднять";
            this.m_menuRun.Click += new System.EventHandler(this.m_menuRun_Click);
            // 
            // m_menuStop
            // 
            this.m_menuStop.Name = "m_menuStop";
            this.m_menuStop.Size = new System.Drawing.Size(152, 22);
            this.m_menuStop.Text = "Уронить";
            this.m_menuStop.Click += new System.EventHandler(this.m_menuStop_Click);
            // 
            // m_statusStrip
            // 
            this.m_statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_statusLabel});
            this.m_statusStrip.Location = new System.Drawing.Point(0, 306);
            this.m_statusStrip.Name = "m_statusStrip";
            this.m_statusStrip.Size = new System.Drawing.Size(433, 22);
            this.m_statusStrip.TabIndex = 1;
            this.m_statusStrip.Text = "statusStrip1";
            // 
            // m_statusLabel
            // 
            this.m_statusLabel.Name = "m_statusLabel";
            this.m_statusLabel.Size = new System.Drawing.Size(104, 17);
            this.m_statusLabel.Text = "Статус: отключён";
            // 
            // m_tab
            // 
            this.m_tab.Controls.Add(this.m_pageFiles);
            this.m_tab.Controls.Add(this.m_tabLog);
            this.m_tab.Dock = System.Windows.Forms.DockStyle.Fill;
            this.m_tab.Location = new System.Drawing.Point(0, 24);
            this.m_tab.Name = "m_tab";
            this.m_tab.SelectedIndex = 0;
            this.m_tab.Size = new System.Drawing.Size(433, 282);
            this.m_tab.TabIndex = 2;
            // 
            // m_pageFiles
            // 
            this.m_pageFiles.Location = new System.Drawing.Point(4, 22);
            this.m_pageFiles.Name = "m_pageFiles";
            this.m_pageFiles.Padding = new System.Windows.Forms.Padding(3);
            this.m_pageFiles.Size = new System.Drawing.Size(425, 256);
            this.m_pageFiles.TabIndex = 0;
            this.m_pageFiles.Text = "Файлы";
            this.m_pageFiles.UseVisualStyleBackColor = true;
            // 
            // m_tabLog
            // 
            this.m_tabLog.Location = new System.Drawing.Point(4, 22);
            this.m_tabLog.Name = "m_tabLog";
            this.m_tabLog.Padding = new System.Windows.Forms.Padding(3);
            this.m_tabLog.Size = new System.Drawing.Size(425, 256);
            this.m_tabLog.TabIndex = 1;
            this.m_tabLog.Text = "Журнал";
            this.m_tabLog.UseVisualStyleBackColor = true;
            // 
            // FileServerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(433, 328);
            this.Controls.Add(this.m_tab);
            this.Controls.Add(this.m_statusStrip);
            this.Controls.Add(this.m_mainMenuStrip);
            this.MainMenuStrip = this.m_mainMenuStrip;
            this.Name = "FileServerForm";
            this.Text = "Файловый сервер";
            this.m_mainMenuStrip.ResumeLayout(false);
            this.m_mainMenuStrip.PerformLayout();
            this.m_statusStrip.ResumeLayout(false);
            this.m_statusStrip.PerformLayout();
            this.m_tab.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip m_mainMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem m_menuServer;
        private System.Windows.Forms.ToolStripMenuItem m_menuRun;
        private System.Windows.Forms.ToolStripMenuItem m_menuStop;
        private System.Windows.Forms.StatusStrip m_statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel m_statusLabel;
        private System.Windows.Forms.TabControl m_tab;
        private System.Windows.Forms.TabPage m_pageFiles;
        private System.Windows.Forms.TabPage m_tabLog;
    }
}


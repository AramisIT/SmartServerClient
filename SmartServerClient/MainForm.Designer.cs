namespace SmartServerClient
    {
    partial class MainForm
        {
        /// <summary>
        /// Требуется переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
            {
            if ( disposing && ( components != null ) )
                {
                components.Dispose();
                }
            base.Dispose(disposing);
            }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Обязательный метод для поддержки конструктора - не изменяйте
        /// содержимое данного метода при помощи редактора кода.
        /// </summary>
        private void InitializeComponent()
            {
            this.Status = new System.Windows.Forms.GroupBox();
            this.Log = new System.Windows.Forms.RichTextBox();
            this.Status.SuspendLayout();
            this.SuspendLayout();
            // 
            // Status
            // 
            this.Status.Controls.Add(this.Log);
            this.Status.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Status.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Status.Location = new System.Drawing.Point(0, 0);
            this.Status.Name = "Status";
            this.Status.Size = new System.Drawing.Size(736, 268);
            this.Status.TabIndex = 2;
            this.Status.TabStop = false;
            this.Status.Text = "Лог";
            // 
            // Log
            // 
            this.Log.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Log.Location = new System.Drawing.Point(3, 16);
            this.Log.Name = "Log";
            this.Log.ReadOnly = true;
            this.Log.Size = new System.Drawing.Size(730, 249);
            this.Log.TabIndex = 1;
            this.Log.Text = "";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(736, 268);
            this.Controls.Add(this.Status);
            this.ForeColor = System.Drawing.Color.Red;
            this.Name = "MainForm";
            this.Text = "SmartServerClient";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Status.ResumeLayout(false);
            this.ResumeLayout(false);

            }

        #endregion

        private System.Windows.Forms.GroupBox Status;
        private System.Windows.Forms.RichTextBox Log;



        }
    }


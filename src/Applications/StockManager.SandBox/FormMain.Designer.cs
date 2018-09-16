namespace StockManager.SandBox
{
	partial class FormMain
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
			this.buttonRunTest = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// buttonRunTest
			// 
			this.buttonRunTest.Location = new System.Drawing.Point(67, 35);
			this.buttonRunTest.Name = "buttonRunTest";
			this.buttonRunTest.Size = new System.Drawing.Size(156, 81);
			this.buttonRunTest.TabIndex = 0;
			this.buttonRunTest.Text = "Run Test";
			this.buttonRunTest.UseVisualStyleBackColor = true;
			this.buttonRunTest.Click += new System.EventHandler(this.OnRunTestClick);
			// 
			// FormMain
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(290, 161);
			this.Controls.Add(this.buttonRunTest);
			this.Name = "FormMain";
			this.Text = "SandBox";
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button buttonRunTest;
	}
}


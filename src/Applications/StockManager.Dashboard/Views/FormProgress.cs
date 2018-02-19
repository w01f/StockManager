using System;
using DevExpress.XtraWaitForm;

namespace StockManager.Dashboard.Views
{
	public partial class FormProgress : WaitForm
	{
		public FormProgress()
		{
			InitializeComponent();
			this.progressPanel.AutoHeight = true;
		}

		#region Overrides

		public override void SetCaption(string caption)
		{
			base.SetCaption(caption);
			this.progressPanel.Caption = caption;
		}
		public override void SetDescription(string description)
		{
			base.SetDescription(description);
			this.progressPanel.Description = description;
		}
		public override void ProcessCommand(Enum cmd, object arg)
		{
			base.ProcessCommand(cmd, arg);
		}

		#endregion

		public enum WaitFormCommand
		{
		}
	}
}
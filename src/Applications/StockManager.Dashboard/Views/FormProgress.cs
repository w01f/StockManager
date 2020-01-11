using DevExpress.XtraWaitForm;

namespace StockManager.Dashboard.Views
{
	public partial class FormProgress : WaitForm
	{
		public FormProgress()
		{
			InitializeComponent();
			progressPanel.AutoHeight = true;
		}

		#region Overrides

		public override void SetCaption(string caption)
		{
			base.SetCaption(caption);
			progressPanel.Caption = caption;
		}
		public override void SetDescription(string description)
		{
			base.SetDescription(description);
			progressPanel.Description = description;
		}

		#endregion

		public enum WaitFormCommand
		{
		}
	}
}
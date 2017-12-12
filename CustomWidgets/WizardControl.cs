﻿using MatterHackers.Agg;
using MatterHackers.Agg.UI;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.CustomWidgets;
using System;
using System.Collections.Generic;

namespace MatterHackers.MatterControl
{
	public class WizardControlPage : GuiWidget
	{
		private string stepDescription = "";

		public string StepDescription { get { return stepDescription; } set { stepDescription = value; } }

		public WizardControlPage(string stepDescription)
		{
			StepDescription = stepDescription;
		}

		public virtual void PageIsBecomingActive()
		{
		}

		public virtual void PageIsBecomingInactive()
		{
		}
	}

	public class WizardControl : GuiWidget
	{
		double extraTextScaling = 1;
		
		private FlowLayoutWidget bottomToTopLayout;
		private List<WizardControlPage> pages = new List<WizardControlPage>();
		private int pageIndex = 0;
		public Button backButton;
		public Button nextButton;
		private Button doneButton;
		private Button cancelButton;

		private TextWidget stepDescriptionWidget;

		public string StepDescription
		{
			get { return stepDescriptionWidget.Text; }
			set { stepDescriptionWidget.Text = value; }
		}

		public WizardControl()
		{
			var buttonFactory = ApplicationController.Instance.Theme.ButtonFactory;

			FlowLayoutWidget topToBottom = new FlowLayoutWidget(FlowDirection.TopToBottom);
			topToBottom.AnchorAll();
			topToBottom.Padding = new BorderDouble(3, 0, 3, 5);

			FlowLayoutWidget headerRow = new FlowLayoutWidget(FlowDirection.LeftToRight);
			headerRow.HAnchor = HAnchor.Stretch;
			headerRow.Margin = new BorderDouble(0, 3, 0, 0);
			headerRow.Padding = new BorderDouble(0, 3, 0, 3);

			{
				stepDescriptionWidget = new TextWidget("", pointSize: 14 * extraTextScaling);
				stepDescriptionWidget.AutoExpandBoundsToText = true;
				stepDescriptionWidget.TextColor = ActiveTheme.Instance.PrimaryTextColor;
				stepDescriptionWidget.HAnchor = HAnchor.Stretch;
				stepDescriptionWidget.VAnchor = Agg.UI.VAnchor.Bottom;

				headerRow.AddChild(stepDescriptionWidget);
			}

			topToBottom.AddChild(headerRow);

			AnchorAll();
			BackgroundColor = ActiveTheme.Instance.PrimaryBackgroundColor;

			bottomToTopLayout = new FlowLayoutWidget(FlowDirection.BottomToTop);
			bottomToTopLayout.BackgroundColor = ActiveTheme.Instance.SecondaryBackgroundColor;
			bottomToTopLayout.Padding = new BorderDouble(3);

			topToBottom.AddChild(bottomToTopLayout);
			topToBottom.Margin = new BorderDouble(bottom: 3);

			{
				FlowLayoutWidget buttonBar = new FlowLayoutWidget();
				buttonBar.HAnchor = Agg.UI.HAnchor.Stretch;
				buttonBar.Padding = new BorderDouble(0, 3);

				backButton = buttonFactory.Generate("Back".Localize());
				backButton.Click += back_Click;

				nextButton = buttonFactory.Generate("Next".Localize());
				nextButton.Name = "Next Button";
				nextButton.Click += next_Click;

				doneButton = buttonFactory.Generate("Done".Localize());
				doneButton.Name = "Done Button";
				doneButton.Click += done_Click;

				cancelButton = buttonFactory.Generate("Cancel".Localize());
				cancelButton.Click += done_Click;
				cancelButton.Name = "Cancel Button";

				buttonBar.AddChild(backButton);
				buttonBar.AddChild(nextButton);
				buttonBar.AddChild(new HorizontalSpacer());
				buttonBar.AddChild(doneButton);
				buttonBar.AddChild(cancelButton);

				topToBottom.AddChild(buttonBar);
			}

			bottomToTopLayout.AnchorAll();

			AddChild(topToBottom);
		}

		private void done_Click(object sender, EventArgs mouseEvent)
		{
			GuiWidget windowToClose = this;
			while (windowToClose != null && windowToClose as SystemWindow == null)
			{
				windowToClose = windowToClose.Parent;
			}

			SystemWindow topSystemWindow = windowToClose as SystemWindow;
			if (topSystemWindow != null)
			{
				topSystemWindow.CloseOnIdle();
			}
		}

		public override void OnClosed(ClosedEventArgs e)
		{
			ApplicationController.Instance.ReloadAll();
			base.OnClosed(e);
		}

		private void next_Click(object sender, EventArgs mouseEvent)
		{
			pageIndex = Math.Min(pages.Count - 1, pageIndex + 1);
			SetPageVisibility();
		}

		private void back_Click(object sender, EventArgs mouseEvent)
		{
			pageIndex = Math.Max(0, pageIndex - 1);
			SetPageVisibility();
		}

		private void SetPageVisibility()
		{
			// we set these before we call becoming active or inactive so that they can override these if needed.
			{
				// if the first page
				if (pageIndex == 0)
				{
					backButton.Enabled = false;
					nextButton.Enabled = true;

					doneButton.Visible = false;
					cancelButton.Visible = true;
				}
				// if the last page
				else if (pageIndex >= pages.Count - 1)
				{
					backButton.Enabled = true;
					nextButton.Enabled = false;

					doneButton.Visible = true;
					cancelButton.Visible = false;
				}
				else // in the middle
				{
					backButton.Enabled = true;
					nextButton.Enabled = true;

					doneButton.Visible = false;
					cancelButton.Visible = true;
				}
			}

			for (int i = 0; i < pages.Count; i++)
			{
				if (i == pageIndex)
				{
					pages[i].Visible = true;
					pages[i].PageIsBecomingActive();
					StepDescription = pages[i].StepDescription;
				}
				else
				{
					if (pages[i].Visible)
					{
						pages[i].Visible = false;
						pages[i].PageIsBecomingInactive();
					}
				}
			}
		}

		public void AddPage(WizardControlPage widgetForPage)
		{
			pages.Add(widgetForPage);
			pages[pages.Count-1].Visible = false;
			bottomToTopLayout.AddChild(widgetForPage);
			SetPageVisibility();
		}
	}
}
﻿using MatterHackers.Agg;
using MatterHackers.Agg.UI;

namespace MatterHackers.MatterControl.CustomWidgets
{
	public class SectionWidget : FlowLayoutWidget
	{
		public SectionWidget(string sectionTitle, Color textColor, GuiWidget sectionContent, GuiWidget rightAlignedContent = null)
			: base (FlowDirection.TopToBottom)
		{
			this.HAnchor = HAnchor.Stretch;
			this.VAnchor = VAnchor.Fit;

			var theme = ApplicationController.Instance.Theme;

			// Add heading
			var textWidget = new TextWidget(sectionTitle, pointSize: theme.H1PointSize, textColor: textColor, bold: false)
			{
				Margin = new BorderDouble(0, 3, 0, 6)
			};

			if (rightAlignedContent == null)
			{
				this.AddChild(textWidget);
			}
			else
			{
				var headingRow = new FlowLayoutWidget()
				{
					HAnchor = HAnchor.Stretch
				};
				headingRow.AddChild(textWidget);
				headingRow.AddChild(new HorizontalSpacer());
				headingRow.AddChild(rightAlignedContent);
				this.AddChild(headingRow);
			}

			// Add heading separator
			this.AddChild(new HorizontalLine(25)
			{
				Margin = new BorderDouble(0)
			});

			// Force padding and add content widget
			sectionContent.Padding = 8;
			sectionContent.HAnchor = HAnchor.Stretch;
			sectionContent.BackgroundColor = ApplicationController.Instance.Theme.MinimalShade;
			this.AddChild(sectionContent);
		}
	}
}
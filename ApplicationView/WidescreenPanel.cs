﻿/*
Copyright (c) 2014, Kevin Pope
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The views and conclusions contained in the software and documentation are those
of the authors and should not be interpreted as representing official policies,
either expressed or implied, of the FreeBSD Project.
*/

using System.IO;
using System.Linq;
using MatterHackers.Agg;
using MatterHackers.Agg.ImageProcessing;
using MatterHackers.Agg.PlatformAbstract;
using MatterHackers.Agg.UI;
using MatterHackers.MatterControl.ConfigurationPage;
using MatterHackers.MatterControl.DataStorage;
using MatterHackers.MatterControl.PartPreviewWindow;
using MatterHackers.MatterControl.PrintLibrary;
using MatterHackers.MatterControl.PrintQueue;

namespace MatterHackers.MatterControl
{
	public class WidescreenPanel : FlowLayoutWidget
	{
		public WidescreenPanel()
		{
		}

		public override void Initialize()
		{
			base.Initialize();

			this.AnchorAll();
			this.Name = "WidescreenPanel";

			// HACK: Long term we need a better solution which does not rely on ActivePrintItem/PrintItemWrapper
			if (ApplicationController.Instance.ActivePrintItem == null)
			{
				// Find the last used bed plate mcx
				var directoryInfo = new DirectoryInfo(ApplicationDataStorage.Instance.PlatingDirectory);
				var firstFile = directoryInfo.GetFileSystemInfos("*.mcx").OrderByDescending(fl => fl.CreationTime).FirstOrDefault();

				// Set as the current item - should be restored as the Active scene in the MeshViewer
				if (firstFile != null)
				{
					ApplicationController.Instance.ActivePrintItem = new PrintItemWrapper(new PrintItem(firstFile.Name, firstFile.FullName));
				}
			}

            // Clear if not assigned above
            if (ApplicationController.Instance.ActivePrintItem == null)
            {
                ApplicationController.Instance.ClearPlate();
            }

			var library3DViewSplitter = new Splitter()
			{
				Padding = new BorderDouble(4),
				SplitterDistance = 254,
				SplitterWidth = ApplicationController.Instance.Theme.SplitterWidth,
				SplitterBackground = ApplicationController.Instance.Theme.SplitterBackground
			};
			library3DViewSplitter.AnchorAll();

			this.AddChild(library3DViewSplitter);

			var leftNav = new FlowLayoutWidget(FlowDirection.TopToBottom);
			leftNav.AnchorAll();

			leftNav.AddChild(new BrandMenuButton()
			{
				MinimumSize = new VectorMath.Vector2(0, 40),
				HAnchor = HAnchor.Stretch,
				VAnchor = VAnchor.Fit
			});

			leftNav.AddChild(new PrintLibraryWidget());

			// put in the left column
			library3DViewSplitter.Panel1.AddChild(leftNav);

			// put in the right column
			library3DViewSplitter.Panel2.AddChild(new PartPreviewContent(ApplicationController.Instance.ActivePrintItem)
			{
				VAnchor = VAnchor.Bottom | VAnchor.Top,
				HAnchor = HAnchor.Left | HAnchor.Right
			});
		}
	}

	public class BrandMenuButton : GuiWidget
	{
		public BrandMenuButton()
		{
			var buttonView = new FlowLayoutWidget()
			{
				HAnchor = HAnchor.Stretch,
				VAnchor = VAnchor.Fit,
				Margin = 8
			};
			buttonView.AfterDraw += (s, e) =>
			{
				//e.graphics2D.Render(directionArrow, buttonView.LocalBounds.Right - arrowHeight * 2 - 2, buttonView.LocalBounds.Center.y + arrowHeight / 2, ActiveTheme.Instance.SecondaryTextColor);
			};

			var icon = StaticData.Instance.LoadIcon(Path.Combine("..", "Images", "mh-logo.png"), 32, 32);
			if (!ActiveTheme.Instance.IsDarkTheme)
			{
				icon = icon.InvertLightness();
			}

			buttonView.AddChild(new ImageWidget(icon));

			buttonView.AddChild(new TextWidget("MatterControl 2.0", textColor: ActiveTheme.Instance.PrimaryTextColor)
			{
				Margin = new BorderDouble(left: 6),
				VAnchor = VAnchor.Center
			});

			var popupButton = new PopupButton(buttonView)
			{
				VAnchor = VAnchor.Center,
				HAnchor = HAnchor.Stretch,
				Margin = 0
			};
			popupButton.PopupContent = new ApplicationSettingsWidget(ApplicationController.Instance.Theme.MenuButtonFactory)
			{
				HAnchor = HAnchor.Absolute,
				VAnchor = VAnchor.Fit,
				Width = 500,
				BackgroundColor = RGBA_Bytes.White
			};

			this.AddChild(popupButton);
		}
	}

	public class UpdateNotificationMark : GuiWidget
	{
		public UpdateNotificationMark()
			: base(12, 12)
		{
		}

		public override void OnDraw(Graphics2D graphics2D)
		{
			graphics2D.Circle(Width / 2, Height / 2, Width / 2, RGBA_Bytes.White);
			graphics2D.Circle(Width / 2, Height / 2, Width / 2 - 1, RGBA_Bytes.Red);
			graphics2D.FillRectangle(Width / 2 - 1, Height / 2 - 3, Width / 2 + 1, Height / 2 + 3, RGBA_Bytes.White);
			base.OnDraw(graphics2D);
		}
	}
}
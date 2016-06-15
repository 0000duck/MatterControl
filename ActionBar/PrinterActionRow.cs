﻿/*
Copyright (c) 2016, Lars Brubaker
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

using MatterHackers.Agg;
using MatterHackers.Agg.UI;
using MatterHackers.GuiAutomation;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.PrinterCommunication;
using MatterHackers.MatterControl.PrinterControls.PrinterConnections;
using MatterHackers.MatterControl.SlicerConfiguration;
using MatterHackers.VectorMath;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MatterHackers.MatterControl.ActionBar
{
	public class PrinterActionRow : ActionRowBase
	{
		private TextImageButtonFactory actionBarButtonFactory = new TextImageButtonFactory();
		private Button connectPrinterButton;
		private string disconnectAndCancelMessage = "Disconnect and cancel the current print?".Localize();
		private string disconnectAndCancelTitle = "WARNING: Disconnecting will cancel the print.".Localize();
		private Button disconnectPrinterButton;
		private Button resetConnectionButton;
		private PrinterSelector printerSelector;

		private event EventHandler unregisterEvents;
		static EventHandler staticUnregisterEvents;

		public static void OpenConnectionWindow(bool connectAfterSelection = false)
		{
			if (connectAfterSelection)
			{
				ActiveSliceSettings.ActivePrinterChanged.RegisterEvent(ConnectToActivePrinter, ref staticUnregisterEvents);
			}

			WizardWindow.Show();
		}

		public override void OnClosed(EventArgs e)
		{
			unregisterEvents?.Invoke(this, null);
			base.OnClosed(e);
		}

		protected override void AddChildElements()
		{
			actionBarButtonFactory.invertImageLocation = false;
			actionBarButtonFactory.borderWidth = 1;
			if (ActiveTheme.Instance.IsDarkTheme)
			{
				actionBarButtonFactory.normalBorderColor = new RGBA_Bytes(77, 77, 77);
			}
			else
			{
				actionBarButtonFactory.normalBorderColor = new RGBA_Bytes(190, 190, 190);
			}
			actionBarButtonFactory.hoverBorderColor = new RGBA_Bytes(128, 128, 128);

			string connectString = "Connect".Localize().ToUpper();
			connectPrinterButton = actionBarButtonFactory.Generate(connectString, "icon_power_32x32.png");
			connectPrinterButton.ToolTipText = "Connect to the currently selected printer".Localize();
			if (ApplicationController.Instance.WidescreenMode)
			{
				connectPrinterButton.Margin = new BorderDouble(0, 0, 3, 3);
			}
			else
			{
				connectPrinterButton.Margin = new BorderDouble(6, 0, 3, 3);
			}
			connectPrinterButton.VAnchor = VAnchor.ParentTop;
			connectPrinterButton.Cursor = Cursors.Hand;

			string disconnectString = "Disconnect".Localize().ToUpper();
			disconnectPrinterButton = actionBarButtonFactory.Generate(disconnectString, "icon_power_32x32.png");
			disconnectPrinterButton.ToolTipText = "Disconnect from current printer".Localize();
			if (ApplicationController.Instance.WidescreenMode)
			{
				disconnectPrinterButton.Margin = new BorderDouble(0, 0, 3, 3);
			}
			else
			{
				disconnectPrinterButton.Margin = new BorderDouble(6, 0, 3, 3);
			}
			disconnectPrinterButton.VAnchor = VAnchor.ParentTop;
			disconnectPrinterButton.Cursor = Cursors.Hand;

			string resetConnectionText = "Reset\nConnection".Localize().ToUpper();
			resetConnectionButton = actionBarButtonFactory.Generate(resetConnectionText, "e_stop4.png");
			if (ApplicationController.Instance.WidescreenMode)
			{
				resetConnectionButton.Margin = new BorderDouble(0, 0, 3, 3);
			}
			else
			{
				resetConnectionButton.Margin = new BorderDouble(6, 0, 3, 3);
			}

			// Bind connect button states to active printer state
			this.SetConnectionButtonVisibleState();

			actionBarButtonFactory.invertImageLocation = true;

			this.AddChild(connectPrinterButton);
			this.AddChild(disconnectPrinterButton);

			FlowLayoutWidget printerSelectorAndEditButton = new FlowLayoutWidget()
			{
				HAnchor = HAnchor.ParentLeftRight,
			};

			int rightMarginForWideScreenMode = ApplicationController.Instance.WidescreenMode ? 6 : 0;
			printerSelector = new PrinterSelector()
			{
				HAnchor = HAnchor.ParentLeftRight,
				Cursor = Cursors.Hand,
				Margin = new BorderDouble(0, 6, rightMarginForWideScreenMode, 3)
			};
			printerSelector.AddPrinter += (s, e) => WizardWindow.Show();
			printerSelector.MinimumSize = new Vector2(printerSelector.MinimumSize.x, connectPrinterButton.MinimumSize.y);
			printerSelectorAndEditButton.AddChild(printerSelector);

			Button editButton = TextImageButtonFactory.GetThemedEditButton();
			editButton.VAnchor = VAnchor.ParentCenter;
			editButton.Click += UiNavigation.GoToEditPrinter_Click;
			printerSelectorAndEditButton.AddChild(editButton);
			this.AddChild(printerSelectorAndEditButton);

			this.AddChild(resetConnectionButton);
		}

		protected override void AddHandlers()
		{
			ActiveSliceSettings.ActivePrinterChanged.RegisterEvent(onActivePrinterChanged, ref unregisterEvents);
			PrinterConnectionAndCommunication.Instance.EnableChanged.RegisterEvent(onPrinterStatusChanged, ref unregisterEvents);
			PrinterConnectionAndCommunication.Instance.CommunicationStateChanged.RegisterEvent(onPrinterStatusChanged, ref unregisterEvents);

			connectPrinterButton.Click += new EventHandler(onConnectButton_Click);
			disconnectPrinterButton.Click += new EventHandler(onDisconnectButtonClick);
			resetConnectionButton.Click += new EventHandler(resetConnectionButton_Click);

			base.AddHandlers();
		}

		protected override void Initialize()
		{
			actionBarButtonFactory.normalTextColor = ActiveTheme.Instance.PrimaryTextColor;
			actionBarButtonFactory.hoverTextColor = ActiveTheme.Instance.PrimaryTextColor;
			actionBarButtonFactory.pressedTextColor = ActiveTheme.Instance.PrimaryTextColor;

			actionBarButtonFactory.disabledTextColor = ActiveTheme.Instance.TabLabelUnselected;
			actionBarButtonFactory.disabledFillColor = ActiveTheme.Instance.PrimaryBackgroundColor;
			actionBarButtonFactory.disabledBorderColor = ActiveTheme.Instance.PrimaryBackgroundColor;

			actionBarButtonFactory.hoverFillColor = ActiveTheme.Instance.PrimaryBackgroundColor;

			actionBarButtonFactory.invertImageLocation = true;
			actionBarButtonFactory.borderWidth = 0;
			this.BackgroundColor = ActiveTheme.Instance.PrimaryBackgroundColor;
		}

		static public void ConnectToActivePrinter(object sender, EventArgs e)
		{
			if (staticUnregisterEvents != null)
			{
				staticUnregisterEvents(null, e);
				staticUnregisterEvents = null;
			}
			PrinterConnectionAndCommunication.Instance.HaltConnectionThread();
			PrinterConnectionAndCommunication.Instance.ConnectToActivePrinter();
		}

		private void onActivePrinterChanged(object sender, EventArgs e)
		{
			connectPrinterButton.Enabled = true;
		}

		private void onConfirmStopPrint(bool messageBoxResponse)
		{
			if (messageBoxResponse)
			{
				PrinterConnectionAndCommunication.Instance.Stop();
				PrinterConnectionAndCommunication.Instance.Disable();
				printerSelector.Invalidate();
			}
		}

		private void onConnectButton_Click(object sender, EventArgs mouseEvent)
		{
			Button buttonClicked = ((Button)sender);
			if (buttonClicked.Enabled)
			{
				if (ActiveSliceSettings.Instance == null)
				{
					OpenConnectionWindow(true);
				}
				else
				{
					ConnectToActivePrinter(null, null);
				}
			}
		}

		private void onDisconnectButtonClick(object sender, EventArgs e)
		{
			UiThread.RunOnIdle(OnIdleDisconnect);
		}

		private void OnIdleDisconnect()
		{
			if (PrinterConnectionAndCommunication.Instance.PrinterIsPrinting)
			{
				StyledMessageBox.ShowMessageBox(onConfirmStopPrint, disconnectAndCancelMessage, disconnectAndCancelTitle, StyledMessageBox.MessageType.YES_NO);
			}
			else
			{
				PrinterConnectionAndCommunication.Instance.Disable();
				printerSelector.Invalidate();
			}
		}

		private void onPrinterStatusChanged(object sender, EventArgs e)
		{
			UiThread.RunOnIdle(SetConnectionButtonVisibleState);
		}

		private void onSelectActivePrinterButton_Click(object sender, EventArgs mouseEvent)
		{
			OpenConnectionWindow();
		}

		private void resetConnectionButton_Click(object sender, EventArgs mouseEvent)
		{
			PrinterConnectionAndCommunication.Instance.RebootBoard();
		}
		private void SetConnectionButtonVisibleState()
		{
			if (PrinterConnectionAndCommunication.Instance.PrinterIsConnected)
			{
				disconnectPrinterButton.Visible = true;
				connectPrinterButton.Visible = false;
			}
			else
			{
				disconnectPrinterButton.Visible = false;
				connectPrinterButton.Visible = true;
			}

			var communicationState = PrinterConnectionAndCommunication.Instance.CommunicationState;

			// Ensure connect buttons are locked while long running processes are executing to prevent duplicate calls into said actions
			connectPrinterButton.Enabled = communicationState != PrinterConnectionAndCommunication.CommunicationStates.AttemptingToConnect;
			disconnectPrinterButton.Enabled = communicationState != PrinterConnectionAndCommunication.CommunicationStates.Disconnecting;
			resetConnectionButton.Visible = ActiveSliceSettings.Instance.GetValue<bool>("show_reset_connection");
		}
	}
}
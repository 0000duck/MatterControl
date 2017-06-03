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
using MatterHackers.Agg.ImageProcessing;
using MatterHackers.Agg.PlatformAbstract;
using MatterHackers.Agg.UI;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.ConfigurationPage.PrintLeveling;
using MatterHackers.MatterControl.DataStorage;
using MatterHackers.MatterControl.PartPreviewWindow;
using MatterHackers.MatterControl.PrinterCommunication;
using MatterHackers.MatterControl.PrintQueue;
using MatterHackers.MatterControl.SlicerConfiguration;

#if __ANDROID__
using MatterHackers.SerialPortCommunication.FrostedSerial;
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace MatterHackers.MatterControl.ActionBar
{
	internal class PrintActionRow : FlowLayoutWidget
	{
		private List<Button> activePrintButtons = new List<Button>();
		private Button addButton;
		private Button addPrinterButton;
		private Button selectPrinterButton;
		private List<Button> allPrintButtons = new List<Button>();

		private Button touchScreenConnectButton;
		private Button cancelConnectButton;
		private Button resetConnectionButton;
		private Button resumeButton;

		private Button startButton;
		private Button pauseButton;
		private Button cancelButton;

		private Button finishSetupButton;

		private EventHandler unregisterEvents;


		public PrintActionRow(TextImageButtonFactory buttonFactory, GuiWidget parentWidget)
		{
			this.HAnchor = HAnchor.ParentLeftRight;

			AddChildElements(buttonFactory, parentWidget);

			// Add Handlers
			PrinterConnectionAndCommunication.Instance.ActivePrintItemChanged.RegisterEvent(onStateChanged, ref unregisterEvents);
			PrinterConnectionAndCommunication.Instance.CommunicationStateChanged.RegisterEvent(onStateChanged, ref unregisterEvents);
			ProfileManager.ProfilesListChanged.RegisterEvent(onStateChanged, ref unregisterEvents);
		}

		protected void AddChildElements(TextImageButtonFactory buttonFactory, GuiWidget parentWidget)
		{
			addButton = buttonFactory.GenerateTooltipButton("Add".Localize().ToUpper());
			addButton.ToolTipText = "Add a file to be printed".Localize();
			addButton.Margin = new BorderDouble(6, 6, 6, 3);
			addButton.Click += (s, e) =>
			{
				UiThread.RunOnIdle(AddButtonOnIdle);
			};

			startButton = buttonFactory.GenerateTooltipButton("Print".Localize().ToUpper());
			startButton.Name = "Start Print Button";
			startButton.ToolTipText = "Begin printing the selected item.".Localize();
			startButton.Margin = new BorderDouble(6, 6, 6, 3);
			startButton.Click += onStartButton_Click;

			finishSetupButton = buttonFactory.GenerateTooltipButton("Finish Setup...".Localize());
			finishSetupButton.Name = "Finish Setup Button";
			finishSetupButton.ToolTipText = "Run setup configuration for printer.".Localize();
			finishSetupButton.Margin = new BorderDouble(6, 6, 6, 3);
			finishSetupButton.Click += onStartButton_Click;

			touchScreenConnectButton = buttonFactory.GenerateTooltipButton("Connect".Localize().ToUpper(), StaticData.Instance.LoadIcon("connect.png", 16,16).InvertLightness());
			touchScreenConnectButton.ToolTipText = "Connect to the printer".Localize();
			touchScreenConnectButton.Margin = new BorderDouble(0, 6, 6, 3);
			touchScreenConnectButton.Click += (s, e) =>
			{
				if (ActiveSliceSettings.Instance.PrinterSelected)
				{
#if __ANDROID__
					if (!ActiveSliceSettings.Instance.GetValue<bool>(SettingsKey.enable_network_printing)
					    && !FrostedSerialPort.HasPermissionToDevice())
					{
						// Opens the USB device permissions dialog which will call back into our UsbDevice broadcast receiver to connect
						FrostedSerialPort.RequestPermissionToDevice(RunTroubleShooting);
					}
					else
#endif
					{
						PrinterConnectionAndCommunication.Instance.HaltConnectionThread();
						PrinterConnectionAndCommunication.Instance.ConnectToActivePrinter(true);
					}
				}
			};

			addPrinterButton = buttonFactory.GenerateTooltipButton("Add Printer".Localize().ToUpper());
			addPrinterButton.ToolTipText = "Select and add a new printer.".Localize();
			addPrinterButton.Margin = new BorderDouble(6, 6, 6, 3);
			addPrinterButton.Click += (s, e) =>
			{
				UiThread.RunOnIdle(() => WizardWindow.ShowPrinterSetup(true));
			};

			selectPrinterButton = buttonFactory.GenerateTooltipButton("Select Printer".Localize().ToUpper());
			selectPrinterButton.ToolTipText = "Select an existing printer.".Localize();
			selectPrinterButton.Margin = new BorderDouble(6, 6, 6, 3);
			selectPrinterButton.Click += (s, e) =>
			{
				WizardWindow.Show<SetupOptionsPage>("/SetupOptions", "Setup Wizard");
			};

			resetConnectionButton = buttonFactory.GenerateTooltipButton("Reset".Localize().ToUpper(), StaticData.Instance.LoadIcon("e_stop4.png", 32,32).InvertLightness());
			resetConnectionButton.ToolTipText = "Reboots the firmware on the controller".Localize();
			resetConnectionButton.Margin = new BorderDouble(6, 6, 6, 3);
			resetConnectionButton.Click += (s, e) => UiThread.RunOnIdle(PrinterConnectionAndCommunication.Instance.RebootBoard);

			pauseButton = buttonFactory.GenerateTooltipButton("Pause".Localize().ToUpper());
			pauseButton.ToolTipText = "Pause the current print".Localize();
			pauseButton.Click += (s, e) =>
			{
				UiThread.RunOnIdle(PrinterConnectionAndCommunication.Instance.RequestPause);
				pauseButton.Enabled = false;
			};
			parentWidget.AddChild(pauseButton);
			allPrintButtons.Add(pauseButton);

			cancelConnectButton = buttonFactory.GenerateTooltipButton("Cancel Connect".Localize().ToUpper());
			cancelConnectButton.ToolTipText = "Stop trying to connect to the printer.".Localize();
			cancelConnectButton.Click += (s, e) => UiThread.RunOnIdle(() =>
			{
				ApplicationController.Instance.ConditionalCancelPrint();
				UiThread.RunOnIdle(SetButtonStates);
			});
			

			cancelButton = buttonFactory.GenerateTooltipButton("Cancel".Localize().ToUpper());
			cancelButton.ToolTipText = "Stop the current print".Localize();
			cancelButton.Name = "Cancel Print Button";
			cancelButton.Click += (s, e) => UiThread.RunOnIdle(() =>
			{
				ApplicationController.Instance.ConditionalCancelPrint();
				SetButtonStates();
			});

			resumeButton = buttonFactory.GenerateTooltipButton("Resume".Localize().ToUpper());
			resumeButton.ToolTipText = "Resume the current print".Localize();
			resumeButton.Name = "Resume Button";
			resumeButton.Click += (s, e) =>
			{
				if (PrinterConnectionAndCommunication.Instance.PrinterIsPaused)
				{
					PrinterConnectionAndCommunication.Instance.Resume();
				}
				pauseButton.Enabled = true;
			};

			parentWidget.AddChild(resumeButton);
			allPrintButtons.Add(resumeButton);
			this.Margin = new BorderDouble(0, 0, 10, 0);
			this.HAnchor = HAnchor.FitToChildren;

			parentWidget.AddChild(touchScreenConnectButton);
			allPrintButtons.Add(touchScreenConnectButton);

			parentWidget.AddChild(addPrinterButton);
			allPrintButtons.Add(addPrinterButton);

			parentWidget.AddChild(selectPrinterButton);
			allPrintButtons.Add(selectPrinterButton);

			parentWidget.AddChild(addButton);
			allPrintButtons.Add(addButton);

			parentWidget.AddChild(startButton);
			allPrintButtons.Add(startButton);

			parentWidget.AddChild(finishSetupButton);
			allPrintButtons.Add(finishSetupButton);

			parentWidget.AddChild(cancelButton);
			allPrintButtons.Add(cancelButton);

			parentWidget.AddChild(cancelConnectButton);
			allPrintButtons.Add(cancelConnectButton);

			parentWidget.AddChild(resetConnectionButton);
			allPrintButtons.Add(resetConnectionButton);

			SetButtonStates();

			PrinterSettings.PrintLevelingEnabledChanged.RegisterEvent((s, e) => SetButtonStates(), ref unregisterEvents);
		}

		protected void DisableActiveButtons()
		{
			foreach (Button button in this.activePrintButtons)
			{
				button.Enabled = false;
			}
		}

		protected void EnableActiveButtons()
		{
			foreach (Button button in this.activePrintButtons)
			{
				button.Enabled = true;
			}
		}

		//Set the states of the buttons based on the status of PrinterCommunication
		protected void SetButtonStates()
		{
			this.activePrintButtons.Clear();
			if (!PrinterConnectionAndCommunication.Instance.PrinterIsConnected
				&& PrinterConnectionAndCommunication.Instance.CommunicationState != PrinterConnectionAndCommunication.CommunicationStates.AttemptingToConnect)
			{
				if (!ProfileManager.Instance.ActiveProfiles.Any())
				{
					this.activePrintButtons.Add(addPrinterButton);
				}
				else if (UserSettings.Instance.IsTouchScreen)
				{
					// only on touch screen because desktop has a printer list and a connect button
					if (ActiveSliceSettings.Instance.PrinterSelected)
					{
						this.activePrintButtons.Add(touchScreenConnectButton);
					}
					else // no printer selected
					{
						this.activePrintButtons.Add(selectPrinterButton);
					}
				}

				ShowActiveButtons();
				EnableActiveButtons();
			}
			else
			{
				switch (PrinterConnectionAndCommunication.Instance.CommunicationState)
				{
					case PrinterConnectionAndCommunication.CommunicationStates.AttemptingToConnect:
						this.activePrintButtons.Add(cancelConnectButton);
						EnableActiveButtons();
						break;

					case PrinterConnectionAndCommunication.CommunicationStates.Connected:
						PrintLevelingData levelingData = ActiveSliceSettings.Instance.Helpers.GetPrintLevelingData();
						if (levelingData != null && ActiveSliceSettings.Instance.GetValue<bool>(SettingsKey.print_leveling_required_to_print)
							&& !levelingData.HasBeenRunAndEnabled())
						{
							this.activePrintButtons.Add(finishSetupButton);
						}
						else
						{
							this.activePrintButtons.Add(startButton);
						}

						EnableActiveButtons();
						break;

					case PrinterConnectionAndCommunication.CommunicationStates.PreparingToPrint:
						this.activePrintButtons.Add(cancelButton);
						EnableActiveButtons();
						break;

					case PrinterConnectionAndCommunication.CommunicationStates.PrintingFromSd:
					case PrinterConnectionAndCommunication.CommunicationStates.Printing:
						if (!PrinterConnectionAndCommunication.Instance.PrintWasCanceled)
						{
							this.activePrintButtons.Add(pauseButton);
							this.activePrintButtons.Add(cancelButton);
						}
						else if (UserSettings.Instance.IsTouchScreen)
						{
							this.activePrintButtons.Add(resetConnectionButton);
						}

						EnableActiveButtons();
						break;

					case PrinterConnectionAndCommunication.CommunicationStates.Paused:
						this.activePrintButtons.Add(resumeButton);
						this.activePrintButtons.Add(cancelButton);
						EnableActiveButtons();
						break;

					case PrinterConnectionAndCommunication.CommunicationStates.FinishedPrint:
						EnableActiveButtons();
						break;

					default:
						DisableActiveButtons();
						break;
				}
			}

			if (PrinterConnectionAndCommunication.Instance.PrinterIsConnected
				&& ActiveSliceSettings.Instance.GetValue<bool>(SettingsKey.show_reset_connection)
				&& UserSettings.Instance.IsTouchScreen)
			{
				this.activePrintButtons.Add(resetConnectionButton);
				ShowActiveButtons();
				EnableActiveButtons();
			}
			ShowActiveButtons();
		}

		protected void ShowActiveButtons()
		{
			foreach (Button button in this.allPrintButtons)
			{
				if (activePrintButtons.IndexOf(button) >= 0)
				{
					button.Visible = true;
				}
				else
				{
					button.Visible = false;
				}
			}
		}

		private void AddButtonOnIdle()
		{
			FileDialog.OpenFileDialog(
				new OpenFileDialogParams(ApplicationSettings.OpenPrintableFileParams, multiSelect: true),
				(openParams) =>
				{
					if (openParams.FileNames != null)
					{
						foreach (string loadedFileName in openParams.FileNames)
						{
							QueueData.Instance.AddItem(new PrintItemWrapper(new PrintItem(Path.GetFileNameWithoutExtension(loadedFileName), Path.GetFullPath(loadedFileName))));
						}
					}
				});
		}

		void RunTroubleShooting()
		{
			WizardWindow.Show<SetupWizardTroubleshooting>("TroubleShooting", "Trouble Shooting");
		}

		private void onStartButton_Click(object sender, EventArgs mouseEvent)
		{
			UiThread.RunOnIdle(() =>
			{
				PrinterConnectionAndCommunication.Instance.PrintActivePartIfPossible();
			});
		}

		private void onStateChanged(object sender, EventArgs e)
		{
			UiThread.RunOnIdle(SetButtonStates);
		}

		public override void OnClosed(ClosedEventArgs e)
		{
			unregisterEvents?.Invoke(this, null);
			base.OnClosed(e);
		}
	}
}
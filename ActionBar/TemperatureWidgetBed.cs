﻿/*
Copyright (c) 2017, Kevin Pope, John Lewin
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

using System;
using MatterHackers.Agg;
using MatterHackers.Agg.ImageProcessing;
using MatterHackers.Agg.Platform;
using MatterHackers.Agg.UI;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.ConfigurationPage;
using MatterHackers.MatterControl.PrinterCommunication;
using MatterHackers.MatterControl.SlicerConfiguration;

namespace MatterHackers.MatterControl.ActionBar
{
	internal class TemperatureWidgetBed : TemperatureWidgetBase
	{
		private string sliceSettingsNote = "Note: Slice Settings are applied before the print actually starts. Changes while printing will not effect the active print.".Localize();
		private string waitingForBedToHeatMessage = "The bed is currently heating and its target temperature cannot be changed until it reaches {0}°C.\n\nYou can set the starting bed temperature in SETTINGS -> Filament -> Temperatures.\n\n{1}".Localize();
		private string waitingForBedToHeatTitle = "Waiting For Bed To Heat".Localize();

		private TextWidget settingsTemperature;

		public TemperatureWidgetBed(PrinterConnection printerConnection)
			: base(printerConnection, "150.3°")
		{
			this.DisplayCurrentTemperature();
			this.ToolTipText = "Current bed temperature".Localize();

			var icon = AggContext.StaticData.LoadIcon("bed.png");
			if (ActiveTheme.Instance.IsDarkTheme)
			{
				icon = icon.InvertLightness();
			}

			this.ImageWidget.Image = icon;

			this.PopupContent = this.GetPopupContent();

			printerConnection.BedTemperatureRead.RegisterEvent((s, e) => DisplayCurrentTemperature(), ref unregisterEvents);
		}

		protected override int TargetTemperature => (int)printerConnection.TargetBedTemperature;

		protected override int ActualTemperature => (int)printerConnection.ActualBedTemperature;

		protected override void SetTargetTemperature(double targetTemp)
		{
			double goalTemp = (int)(targetTemp + .5);
			if (printerConnection.PrinterIsPrinting
				&& printerConnection.DetailedPrintingState == DetailedPrintingState.HeatingBed
				&& goalTemp != printerConnection.TargetBedTemperature)
			{
				string message = string.Format(waitingForBedToHeatMessage, printerConnection.TargetBedTemperature, sliceSettingsNote);
				StyledMessageBox.ShowMessageBox(null, message, waitingForBedToHeatTitle);
			}
			else
			{
				printerConnection.TargetBedTemperature = (int)(targetTemp + .5);
			}
		}

		protected override GuiWidget GetPopupContent()
		{
			var widget = new IgnoredPopupWidget()
			{
				Width = 300,
				HAnchor = HAnchor.Absolute,
				VAnchor = VAnchor.Fit,
				BackgroundColor = RGBA_Bytes.White,
				Padding = new BorderDouble(12, 0)
			};

			var container = new FlowLayoutWidget(FlowDirection.TopToBottom)
			{
				HAnchor = HAnchor.Stretch,
				VAnchor = VAnchor.Fit | VAnchor.Top,
				BackgroundColor = RGBA_Bytes.White
			};
			widget.AddChild(container);

			container.AddChild(new SettingsItem(
				"Heated Bed".Localize(),
				new SettingsItem.ToggleSwitchConfig()
				{
					Checked = false,
					ToggleAction = (itemChecked) =>
					{
						var goalTemp = itemChecked ? printerConnection.PrinterSettings.GetValue<double>(SettingsKey.bed_temperature) : 0;

						if (printerConnection.PrinterIsPrinting
							&& printerConnection.DetailedPrintingState == DetailedPrintingState.HeatingBed
							&& goalTemp != printerConnection.TargetBedTemperature)
						{
							string sliceSettingsNote = "Note: Slice Settings are applied before the print actually starts. Changes while printing will not effect the active print.";
							string message = string.Format(
								"The bed is currently heating and its target temperature cannot be changed until it reaches {0}°C.\n\nYou can set the starting bed temperature in 'Slice Settings' -> 'Filament'.\n\n{1}",
								printerConnection.TargetBedTemperature,
								sliceSettingsNote);

							StyledMessageBox.ShowMessageBox(null, message, "Waiting For Bed To Heat");
						}
						else
						{
							if (itemChecked)
							{
								SetTargetTemperature(printerConnection.PrinterSettings.GetValue<double>(SettingsKey.bed_temperature));
							}
							else
							{
								SetTargetTemperature(0);
							}
						}
					}
				},
				enforceGutter: false));

			settingsTemperature = new TextWidget(printerConnection.PrinterSettings.GetValue<double>(SettingsKey.bed_temperature).ToString())
			{
				AutoExpandBoundsToText = true
			};

			container.AddChild(new SettingsItem(
				"Temperature".Localize(),
				settingsTemperature,
				enforceGutter: false));

			ActiveSliceSettings.MaterialPresetChanged += ActiveSliceSettings_MaterialPresetChanged;

			return widget;
		}

		private void ActiveSliceSettings_MaterialPresetChanged(object sender, EventArgs e)
		{
			if (settingsTemperature != null && printerConnection.PrinterSettings != null)
			{
				settingsTemperature.Text = printerConnection.PrinterSettings.GetValue(SettingsKey.bed_temperature);
			}
		}

		public override void OnClosed(ClosedEventArgs e)
		{
			ActiveSliceSettings.MaterialPresetChanged -= ActiveSliceSettings_MaterialPresetChanged;
			base.OnClosed(e);
		}
	}
}
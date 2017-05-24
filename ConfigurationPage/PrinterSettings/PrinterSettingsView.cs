﻿using MatterHackers.Agg;
using MatterHackers.Agg.Image;
using MatterHackers.Agg.ImageProcessing;
using MatterHackers.Agg.PlatformAbstract;
using MatterHackers.Agg.UI;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.ConfigurationPage.PrintLeveling;
using MatterHackers.MatterControl.CustomWidgets;
using MatterHackers.MatterControl.EeProm;
using MatterHackers.MatterControl.PrinterCommunication;
using MatterHackers.MatterControl.SlicerConfiguration;
using System;
using System.IO;

namespace MatterHackers.MatterControl.ConfigurationPage
{
	public class HardwareSettingsWidget : SettingsViewBase
	{
		private Button openCameraButton;

		private EventHandler unregisterEvents;

		public HardwareSettingsWidget()
			: base("Hardware".Localize())
		{
			DisableableWidget cameraContainer = new DisableableWidget();
			cameraContainer.AddChild(GetCameraControl());

			if (ApplicationSettings.Instance.get(ApplicationSettingsKey.HardwareHasCamera) == "true")
			{
				mainContainer.AddChild(new HorizontalLine(50));
				mainContainer.AddChild(cameraContainer);
			}

			AddChild(mainContainer);
			AddHandlers();
			SetEnabledStates();
		}

		public override void OnClosed(ClosedEventArgs e)
		{
			unregisterEvents?.Invoke(this, null);
			base.OnClosed(e);
		}

		private FlowLayoutWidget GetCameraControl()
		{
			FlowLayoutWidget buttonRow = new FlowLayoutWidget();
			buttonRow.HAnchor = HAnchor.ParentLeftRight;
			buttonRow.Margin = new BorderDouble(0, 4);

			ImageBuffer cameraIconImage = StaticData.Instance.LoadIcon("camera-24x24.png",24,24).InvertLightness();
			cameraIconImage.SetRecieveBlender(new BlenderPreMultBGRA());
			int iconSize = (int)(24 * GuiWidget.DeviceScale);

			if (!ActiveTheme.Instance.IsDarkTheme)
			{
				cameraIconImage.InvertLightness();
			}

			ImageWidget cameraIcon = new ImageWidget(cameraIconImage);
			cameraIcon.Margin = new BorderDouble(right: 6);

			TextWidget cameraLabel = new TextWidget("Camera Monitoring".Localize());
			cameraLabel.AutoExpandBoundsToText = true;
			cameraLabel.TextColor = ActiveTheme.Instance.PrimaryTextColor;
			cameraLabel.VAnchor = VAnchor.ParentCenter;

			openCameraButton = textImageButtonFactory.Generate("Preview".Localize().ToUpper());
			openCameraButton.Click += openCameraPreview_Click;
			openCameraButton.Margin = new BorderDouble(left: 6);

			buttonRow.AddChild(cameraIcon);
			buttonRow.AddChild(cameraLabel);
			buttonRow.AddChild(new HorizontalSpacer());
			buttonRow.AddChild(openCameraButton);

			if (ApplicationSettings.Instance.get(ApplicationSettingsKey.HardwareHasCamera) == "true")
			{
				GuiWidget publishImageSwitchContainer = new FlowLayoutWidget();
				publishImageSwitchContainer.VAnchor = VAnchor.ParentCenter;
				publishImageSwitchContainer.Margin = new BorderDouble(left: 16);

				CheckBox toggleSwitch = ImageButtonFactory.CreateToggleSwitch(ActiveSliceSettings.Instance.GetValue<bool>(SettingsKey.publish_bed_image));
				toggleSwitch.CheckedStateChanged += (sender, e) =>
				{
					CheckBox thisControl = sender as CheckBox;
					ActiveSliceSettings.Instance.SetValue(SettingsKey.publish_bed_image, thisControl.Checked ? "1" : "0");
				};
				publishImageSwitchContainer.AddChild(toggleSwitch);

				publishImageSwitchContainer.SetBoundsToEncloseChildren();

				buttonRow.AddChild(publishImageSwitchContainer);
			}

			return buttonRow;
		}


		private void AddHandlers()
		{
			PrinterConnectionAndCommunication.Instance.CommunicationStateChanged.RegisterEvent((e, s) => SetEnabledStates(), ref unregisterEvents);
			PrinterConnectionAndCommunication.Instance.EnableChanged.RegisterEvent((e,s) => SetEnabledStates(), ref unregisterEvents);
		}

		private void openCameraPreview_Click(object sender, EventArgs e)
		{
			MatterControlApplication.Instance.OpenCameraPreview();
		}

		private void SetEnabledStates()
		{
			if (!ActiveSliceSettings.Instance.PrinterSelected)
			{
				//cloudMonitorContainer.SetEnableLevel(DisableableWidget.EnableLevel.Disabled);
			}
			else // we at least have a printer selected
			{
				//cloudMonitorContainer.SetEnableLevel(DisableableWidget.EnableLevel.Enabled);
				switch (PrinterConnectionAndCommunication.Instance.CommunicationState)
				{
					case PrinterConnectionAndCommunication.CommunicationStates.Disconnecting:
					case PrinterConnectionAndCommunication.CommunicationStates.ConnectionLost:
					case PrinterConnectionAndCommunication.CommunicationStates.Disconnected:
					case PrinterConnectionAndCommunication.CommunicationStates.AttemptingToConnect:
					case PrinterConnectionAndCommunication.CommunicationStates.FailedToConnect:
						break;

					case PrinterConnectionAndCommunication.CommunicationStates.FinishedPrint:
					case PrinterConnectionAndCommunication.CommunicationStates.Connected:
						break;

					case PrinterConnectionAndCommunication.CommunicationStates.PrintingFromSd:
						break;

					case PrinterConnectionAndCommunication.CommunicationStates.PreparingToPrint:
					case PrinterConnectionAndCommunication.CommunicationStates.Printing:
						switch (PrinterConnectionAndCommunication.Instance.PrintingState)
						{
							case PrinterConnectionAndCommunication.DetailedPrintingState.HomingAxis:
							case PrinterConnectionAndCommunication.DetailedPrintingState.HeatingBed:
							case PrinterConnectionAndCommunication.DetailedPrintingState.HeatingExtruder:
							case PrinterConnectionAndCommunication.DetailedPrintingState.Printing:
								break;

							default:
								throw new NotImplementedException();
						}
						break;

					case PrinterConnectionAndCommunication.CommunicationStates.Paused:
						break;

					default:
						throw new NotImplementedException();
				}
			}

			this.Invalidate();
		}
	}
}
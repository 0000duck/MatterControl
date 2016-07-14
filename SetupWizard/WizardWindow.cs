﻿using MatterHackers.Agg;
using MatterHackers.Agg.UI;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.DataStorage;
using MatterHackers.MatterControl.PrinterCommunication;
using MatterHackers.MatterControl.PrinterControls.PrinterConnections;
using MatterHackers.MatterControl.SlicerConfiguration;
using MatterHackers.VectorMath;
using System;
using System.Collections.Generic;

namespace MatterHackers.MatterControl
{
	public class WizardWindow : SystemWindow
	{
		public static Func<bool> ShouldShowAuthPanel { get; set; }
		public static Action ShowAuthDialog;
		public static Action ChangeToAccountCreate;
		private static WizardWindow wizardWindow = null;

		private static Dictionary<string, WizardWindow> allWindows = new Dictionary<string, WizardWindow>();

		private WizardWindow()
			: base(500 * GuiWidget.DeviceScale, 500 * GuiWidget.DeviceScale)
		{

			this.AlwaysOnTopOfMain = true;

			this.BackgroundColor = ActiveTheme.Instance.PrimaryBackgroundColor;
			this.Padding = new BorderDouble(8);
			this.ShowAsSystemWindow();
			this.MinimumSize = new Vector2(350 * GuiWidget.DeviceScale, 400 * GuiWidget.DeviceScale);
		}

		private WizardWindow(bool openToHome = false)
			: base(500 * GuiWidget.DeviceScale, 500 * GuiWidget.DeviceScale)
		{
			this.AlwaysOnTopOfMain = true;

			AlwaysOnTopOfMain = true;
			this.Title = "Setup Wizard".Localize();

			// Todo - detect wifi connectivity
			bool WifiDetected = MatterControlApplication.Instance.IsNetworkConnected();
			if (!WifiDetected)
			{
				ChangeToPage<SetupWizardWifi>();
			}
			else
			{
				ChangeToSetupPrinterForm();
			}

			this.BackgroundColor = ActiveTheme.Instance.PrimaryBackgroundColor;
			this.Padding = new BorderDouble(8);
			this.ShowAsSystemWindow();
			this.MinimumSize = new Vector2(350 * GuiWidget.DeviceScale, 400 * GuiWidget.DeviceScale);
		}

		public static void Close(string uri)
		{
			WizardWindow existingWindow;

			if (allWindows.TryGetValue(uri, out existingWindow))
			{
				existingWindow.Close();
			}
		}

		public static WizardWindow GetSystemWindow(string uri)
		{
			WizardWindow existingWindow;

			if (allWindows.TryGetValue(uri, out existingWindow))
			{
				return existingWindow;
			}

			return null;
		}

		public static void Show<PanelType>(string uri, string title) where PanelType : WizardPage, new()
		{
			WizardWindow wizardWindow = GetWindow(uri);
			wizardWindow.Title = title;
			wizardWindow.ChangeToPage<PanelType>();
		}

		public static void Show(string uri, string title, WizardPage wizardPage)
		{
			WizardWindow wizardWindow = GetWindow(uri);
			wizardWindow.Title = title;
			wizardWindow.ChangeToPage(wizardPage);
		}

		public static void Show(bool openToHome = false)
		{
			if (wizardWindow == null)
			{
				wizardWindow = new WizardWindow(openToHome);
				wizardWindow.Closed += (s, e) => wizardWindow = null;
			}
			else
			{
				wizardWindow.BringToFront();
			}
		}

		private static WizardWindow GetWindow(string uri)
		{
			WizardWindow wizardWindow;

			if (allWindows.TryGetValue(uri, out wizardWindow))
			{
				wizardWindow.BringToFront();
			}
			else
			{
				wizardWindow = new WizardWindow();
				wizardWindow.Closed += (s, e) => allWindows.Remove(uri);
				allWindows[uri] = wizardWindow;
			}

			return wizardWindow;
		}

		public override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
		}

		public void ChangeToSetupPrinterForm()
		{
			bool showAuthPanel = ShouldShowAuthPanel?.Invoke() ?? false;
			if (showAuthPanel)
			{
				ChangeToPage<ShowAuthPanel>();
			}
			else
			{
				ChangeToPage<SetupStepMakeModelName>();
			}
		}

		internal void ChangeToInstallDriverOrComPortOne()
		{
			if (SetupStepInstallDriver.PrinterDrivers().Count > 0)
			{
				ChangeToPage<SetupStepInstallDriver>();
			}
			else
			{
				ChangeToPage<SetupStepComPortOne>();
			}
		}

		internal void ChangeToSetupBaudOrComPortOne()
		{
			if (string.IsNullOrEmpty(PrinterConnectionAndCommunication.Instance?.ActivePrinter?.GetValue(SettingsKey.baud_rate)))
			{
				ChangeToPage<SetupStepBaudRate>();
			}
			else
			{
				ChangeToPage<SetupStepComPortOne>();
			}
		}

		internal void ChangeToPage(WizardPage pageToChangeTo)
		{
			pageToChangeTo.WizardWindow = this;
			UiThread.RunOnIdle(() =>
			{
				this.RemoveAllChildren();
				this.AddChild(pageToChangeTo);
				this.Invalidate();
			});
		}

		internal void ChangeToPage<PanelType>() where PanelType : WizardPage, new()
		{
			UiThread.RunOnIdle(() =>
			{
				this.RemoveAllChildren();
				this.AddChild(new PanelType() { WizardWindow = this });
				this.Invalidate();
			});
		}
	}
}
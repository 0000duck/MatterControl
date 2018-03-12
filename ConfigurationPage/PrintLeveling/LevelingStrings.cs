﻿/*
Copyright (c) 2014, Lars Brubaker
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
using MatterHackers.Localizations;
using MatterHackers.MatterControl.PrinterCommunication;
using MatterHackers.MatterControl.SlicerConfiguration;

namespace MatterHackers.MatterControl.ConfigurationPage.PrintLeveling
{
	public class LevelingStrings
	{
		public string HomingPageStepText = "Homing The Printer".Localize();
		public string WaitingForTempPageStepText = "Waiting For Bed To Heat".Localize();
		public string initialPrinterSetupStepText = "Initial Printer Setup".Localize();
		public string materialStepText = "Select Material".Localize();
		public string requiredPageInstructions1 = "Congratulations on connecting to your new printer. Before starting your first print we need to run a simple calibration procedure.";
		public string requiredPageInstructions2 = "The next few screens will walk your through the print leveling wizard.";
		public string materialPageInstructions1 = "The temperature of the bed can have a significant effect on the quality of leveling.";
		public string materialPageInstructions2 = "Please select the material you will be printing, so we can adjust the temperature before calibrating.";
		public string stepTextBeg = "Step".Localize();
		public string stepTextEnd = "of".Localize();
		private string doneLine1 = "Congratulations!";
		private string doneLine1b = "Auto Print Leveling is now configured and enabled.".Localize();
		private string doneLine2 = "Remove the paper".Localize();
		private string doneLine3 = "To re-calibrate the printer, or to turn off Auto Print Leveling, the print leveling controls can be found under 'Options'->'Calibration'.";
		private string doneLine3b = "Click 'Done' to close this window.".Localize();
		private string homingLine1 = "The printer should now be 'homing'. Once it is finished homing we will heat the bed.";
		private string homingLine1b = "To complete the next few steps you will need".Localize();
		private string homingLine2 = "A standard sheet of paper".Localize();
		private string homingLine3 = "We will use this paper to measure the distance between the extruder and the bed.";
		private int stepNumber = 1;
		private string probeWelcomeLine1 = "Welcome to the probe calibration wizard. Here is a quick overview on what we are going to do.".Localize();
		private string welcomeLine1 = "Welcome to the print leveling wizard. Here is a quick overview on what we are going to do.".Localize();
		private string selectMaterial = "Select the material you are printing".Localize();
		private string homeThePrinter = "Home the printer".Localize();
		private string heatTheBed = "Heat the bed".Localize();
		private string sampelAtPoints = "Sample the bed at {0} points".Localize();
		private string turnOnLeveling = "Turn auto leveling on".Localize();
		private string timeToDone = "We should be done in less than {0} minutes.".Localize();
		private string cleanExtruder = "Note: Be sure the tip of the extruder is clean and the bed is clear.".Localize();
		private string clickNext = "Click 'Next' to continue.".Localize();

		PrinterSettings printerSettings;
		public LevelingStrings(PrinterSettings printerSettings)
		{
			this.printerSettings = printerSettings;
		}

		public string DoneInstructions
		{
			get
			{
				if (printerSettings.Helpers.UseZProbe())
				{
					return $"{doneLine1} {doneLine1b}\n\n{doneLine3} {doneLine3b}";
				}
				else
				{
					return $"{doneLine1} {doneLine1b}\n\n\t• {doneLine2}\n\n{doneLine3} {doneLine3b}";
				}
			}
		}

		public string WaitingForTempPageInstructions
		{
			get
			{
				if (printerSettings.Helpers.UseZProbe())
				{
					return "Waiting for the bed to heat up.".Localize() + "\n"
						+ "This will improve the accuracy of print leveling.".Localize();
				}
				else
				{
					return "Waiting for the bed to heat up.".Localize() + "\n"
						+ "This will improve the accuracy of print leveling.".Localize() + "\n" + "\n"
						+ "Click 'Next' when the bed reaches temp.";
				}
			}
		}

		public string HomingPageInstructions(bool useZProbe)
		{
			if (useZProbe)
			{
				return homingLine1;
			}
			else
			{
				return "{0}\n\n{1}:\n\n\t• {2}\n\n{3}\n\n{4}".FormatWith(homingLine1, homingLine1b, homingLine2, homingLine3, clickNext);
			}
		}

		public string OverviewText { get; } = "Print Leveling Overview".Localize();

		string setZHeightLower = "Press [Z-] until there is resistance to moving the paper".Localize();
		string setZHeightRaise = "Press [Z+] once to release the paper".Localize();
		string setZHeightNext = "Finally click 'Next' to continue.".Localize();

		public string CoarseInstruction1 => "Using the [Z] controls on this screen, we will now take a coarse measurement of the extruder height at this position.".Localize();
		public string CoarseInstruction2
		{
			get
			{
				string setZHeightCourseInstructTextOne = "Place the paper under the extruder".Localize();
				string setZHeightCourseInstructTextTwo = "Using the above controls".Localize();
				return string.Format("\t• {0}\n\t• {1}\n\t• {2}\n\t• {3}\n\n{4}", setZHeightCourseInstructTextOne, setZHeightCourseInstructTextTwo, setZHeightLower, setZHeightRaise, setZHeightNext);
			}
		}

		public string FineInstruction1 => "We will now refine our measurement of the extruder height at this position.".Localize();
		public string FineInstruction2
		{
			get
			{
				return string.Format("\t• {0}\n\t• {1}\n\n{2}", setZHeightLower, setZHeightRaise, setZHeightNext);
			}
		}

		public string UltraFineInstruction1 => "We will now finalize our measurement of the extruder height at this position.".Localize();

		public string GetStepString(int totalSteps)
		{
			return "{0} {1} {2} {3}:".FormatWith(stepTextBeg, stepNumber++, stepTextEnd, totalSteps);
		}

		public string CalibrateProbeWelcomText()
		{
			return "{0}\n\n\t• {1}\n\t• {2}\n\t• {3}\n\n{4}\n\n{5}\n\n{6}".FormatWith(
				this.probeWelcomeLine1,
				this.homeThePrinter,
				"Probe the bed at the center".Localize(),
				"Manually measure the extruder at the center".Localize(),
				this.WelcomeLine7(1),
				this.cleanExtruder,
				this.clickNext);
		}

		public string WelcomeText(int numberOfSteps, int numberOfMinutes)
		{
			if (printerSettings.Helpers.UseZProbe())
			{
				numberOfMinutes = 2;
			}

			if (printerSettings.GetValue<bool>(SettingsKey.has_heated_bed))
			{
				return "{0}\n\n\t• {1}\n\t• {2}\n\t• {3}\n\t• {4}\n\t• {5}\n\n{6}\n\n{7}\n\n{8}".FormatWith(
					this.welcomeLine1,
					this.selectMaterial,
					this.homeThePrinter,
					this.heatTheBed,
					this.WelcomeLine5(numberOfSteps),
					this.turnOnLeveling,
					this.WelcomeLine7(numberOfMinutes),
					this.cleanExtruder,
					this.clickNext);
			}
			else
			{
				return "{0}\n\n\t• {1}\n\t• {2}\n\t• {3}\n\n{4}\n\n{5}\n\n{6}".FormatWith(
					this.welcomeLine1,
					this.homeThePrinter,
					this.WelcomeLine5(numberOfSteps),
					this.turnOnLeveling,
					this.WelcomeLine7(numberOfMinutes),
					this.cleanExtruder,
					this.clickNext);
			}
		}

		private string WelcomeLine5(int numberOfPoints)
		{
			return sampelAtPoints.FormatWith(numberOfPoints);
		}

		private string WelcomeLine7(int numberOfMinutes)
		{
			return timeToDone.FormatWith(numberOfMinutes);
		}
	}
}
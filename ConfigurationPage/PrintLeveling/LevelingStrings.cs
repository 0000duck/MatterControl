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
using MatterHackers.MatterControl.SlicerConfiguration;

namespace MatterHackers.MatterControl.ConfigurationPage.PrintLeveling
{
	public class LevelingStrings
	{
		public string homingPageStepText = "Homing The Printer".Localize();
		public string initialPrinterSetupStepText = "Initial Printer Setup".Localize();
		public string requiredPageInstructions1 = "Congratulations on connecting to your new printer. Before starting your first print we need to run a simple calibration procedure.";
		public string requiredPageInstructions2 = "The next few screens will walk your through the print leveling wizard.";
		public string stepTextBeg = "Step".Localize();
		public string stepTextEnd = "of".Localize();
		private string doneLine1 = "Congratulations!";
		private string doneLine1b = "Auto Print Leveling is now configured and enabled.".Localize();
		private string doneLine2 = "Remove the paper".Localize();
		private string doneLine3 = "To re-calibrate the printer, or to turn off Auto Print Leveling, the print leveling controls can be found under 'Options'->'Calibration'.";
		private string doneLine3b = "Click 'Done' to close this window.".Localize();
		private string homingLine1 = "The printer should now be 'homing'. Once it is finished homing we will move it to the first point to sample.";
		private string homingLine1b = "To complete the next few steps you will need".Localize();
		private string homingLine2 = "A standard sheet of paper".Localize();
		private string homingLine3 = "We will use this paper to measure the distance between the extruder and the bed.";
		private string homingLine3b = "Click 'Next' to continue.".Localize();
		private int stepNumber = 1;
		private string welcomeLine1 = "Welcome to the print leveling wizard. Here is a quick overview on what we are going to do.".Localize();
		private string welcomeLine2 = "Home the printer".Localize();
		private string welcomeLine3 = "Sample the bed at {0} points".Localize();
		private string welcomeLine4 = "Turn auto leveling on".Localize();
		private string welcomeLine5 = "We should be done in less than {0} minutes.".Localize();
		private string welcomeLine6 = "Note: Be sure the tip of the extruder is clean and the bed is clear.".Localize();
		private string welcomeLine7 = "Click 'Next' to continue.".Localize();

		public string DoneInstructions
		{
			get
			{
				if (ActiveSliceSettings.Instance.GetValue<bool>(SettingsKey.use_g30_for_bed_probe))
				{
					return "{0}{1}\n\n{2}{3}".FormatWith(doneLine1, doneLine1b, doneLine3, doneLine3b);
				}
				else
				{
					return "{0}{1}\n\n\t• {2}\n\n{3}{4}".FormatWith(doneLine1, doneLine1b, doneLine2, doneLine3, doneLine3b);
				}
			}
		}

		public string homingPageInstructions
		{
			get
			{
				if (ActiveSliceSettings.Instance.GetValue<bool>(SettingsKey.use_g30_for_bed_probe))
				{
					return "{0}\n\n{1}".FormatWith(homingLine1, homingLine3b);
				}
				else
				{
					return "{0}\n\n{1}:\n\n\t• {2}\n\n{3}\n\n{4}".FormatWith(homingLine1, homingLine1b, homingLine2, homingLine3, homingLine3b);
				}
			}
		}

		public string OverviewText { get; } = "Print Leveling Overview".Localize();

		public string GetStepString(int totalSteps)
		{
			return "{0} {1} {2} {3}:".FormatWith(stepTextBeg, stepNumber++, stepTextEnd, totalSteps);
		}

		public string WelcomeText(int numberOfSteps, int numberOfMinutes)
		{
			if (ActiveSliceSettings.Instance.GetValue<bool>(SettingsKey.use_g30_for_bed_probe))
			{
				numberOfMinutes = 1;
			}

			return "{0}\n\n\t• {1}\n\t• {2}\n\t• {3}\n\n{4}\n\n{5}\n\n{6}".FormatWith(
				this.welcomeLine1,
				this.welcomeLine2,
				this.WelcomeLine3(numberOfSteps),
				this.welcomeLine4,
				this.WelcomeLine5(numberOfMinutes),
				this.welcomeLine6,
				this.welcomeLine7);
		}

		private string WelcomeLine3(int numberOfPoints)
		{
			return welcomeLine3.FormatWith(numberOfPoints);
		}

		private string WelcomeLine5(int numberOfMinutes)
		{
			return welcomeLine5.FormatWith(numberOfMinutes);
		}
	}
}
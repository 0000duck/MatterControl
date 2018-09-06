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

using MatterHackers.Agg;
using MatterHackers.Agg.UI;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.DataStorage;
using MatterHackers.MatterControl.PrinterCommunication;
using MatterHackers.MatterControl.PrintQueue;
using MatterHackers.MatterControl.SlicerConfiguration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MatterHackers.MatterControl.PrintHistory
{
	public static class PrintRecovery
	{
		public static void CheckIfNeedToRecoverPrint(PrinterConfig printer)
		{
			string recoverPrint = "Recover Print".Localize();
			string cancelRecovery = "Cancel".Localize();
			string printRecoveryWarningMessage = "WARNING: In order to perform print recovery, your printer must move down to reach its home position.\nIf your print is too large, part of your printer may collide with it when moving down.\nMake sure it is safe to perform this operation before proceeding.".Localize();
			string printRecoveryMessage = "It appears your last print failed to complete.\n\nWould your like to attempt to recover from the last know position?".Localize();
			string recoverPrintTitle = "Recover Last Print".Localize();

			PrintTask lastPrint = PrintHistoryData.Instance.GetHistoryForPrinter(printer.Settings.ID.GetHashCode()).FirstOrDefault();
			if (lastPrint != null)
			{
				if (!lastPrint.PrintComplete // Top Print History Item is not complete
					&& !string.IsNullOrEmpty(lastPrint.PrintingGCodeFileName) // PrintingGCodeFileName is set
					&& File.Exists(lastPrint.PrintingGCodeFileName) // PrintingGCodeFileName is still on disk
					&& lastPrint.PercentDone > 0 // we are actually part way into the print
					&& printer.Settings.GetValue<bool>(SettingsKey.recover_is_enabled)
					&& !printer.Settings.GetValue<bool>(SettingsKey.has_hardware_leveling))
				{
					bool safeHomingDirection = printer.Settings.GetValue<bool>(SettingsKey.z_homes_to_max);

					StyledMessageBox.ShowMessageBox(
						(messageBoxResponse) =>
						{
							if (messageBoxResponse)
							{
								UiThread.RunOnIdle(async () =>
								{
									if (printer.Connection.CommunicationState == CommunicationStates.Connected)
									{
										printer.Connection.CommunicationState = CommunicationStates.PreparingToPrint;
										await printer.Connection.StartPrint(lastPrint.PrintingGCodeFileName, lastPrint);
										ApplicationController.Instance.MonitorPrintTask(printer);
									}
								});
							}
							else // the recovery has been canceled
							{
								lastPrint.PrintingGCodeFileName = null;
								lastPrint.Commit();
							}
						},
						(safeHomingDirection) ? printRecoveryMessage : printRecoveryMessage + "\n\n" + printRecoveryWarningMessage,
						recoverPrintTitle,
						StyledMessageBox.MessageType.YES_NO,
						recoverPrint,
						cancelRecovery);
				}
			}
		}
	}

	public class PrintHistoryData
	{
		public static readonly int RecordLimit = 20;
		public RootedObjectEventHandler HistoryCleared = new RootedObjectEventHandler();
		public bool ShowTimestamp;
		private static PrintHistoryData instance;

		public static PrintHistoryData Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new PrintHistoryData();
				}
				return instance;
			}
		}

		public IEnumerable<DataStorage.PrintTask> GetHistoryItems(int recordCount)
		{
			string query;
			if (UserSettings.Instance.get(UserSettingsKey.PrintHistoryFilterShowCompleted) == "true")
			{
				query = string.Format("SELECT * FROM PrintTask WHERE PrintComplete = 1 ORDER BY PrintStart DESC LIMIT {0};", recordCount);
			}
			else
			{
				query = string.Format("SELECT * FROM PrintTask ORDER BY PrintStart DESC LIMIT {0};", recordCount);
			}

			return Datastore.Instance.dbSQLite.Query<PrintTask>(query);
		}

		public IEnumerable<DataStorage.PrintTask> GetHistoryForPrinter(int printerID)
		{
			string query = string.Format("SELECT * FROM PrintTask WHERE PrinterID={0} ORDER BY PrintStart DESC LIMIT 1;", printerID);
			return Datastore.Instance.dbSQLite.Query<PrintTask>(query);
		}

		internal void ClearHistory()
		{
			Datastore.Instance.dbSQLite.ExecuteScalar<PrintTask>("DELETE FROM PrintTask;");
			HistoryCleared.CallEvents(this, null);
		}
	}
}
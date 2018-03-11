﻿/*
Copyright (c) 2018, Lars Brubaker, John Lewin
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
using System.Collections.Generic;
using System.IO;
using MatterHackers.Agg;
using MatterHackers.Agg.UI;
using MatterHackers.MatterControl.DataStorage;
using MatterHackers.MatterControl.PrintQueue;

namespace MatterHackers.MatterControl
{

	public static class CacheDirectory
	{
		private static readonly Point2D BigRenderSize = new Point2D(460, 460);

		private static readonly string ThumbnailsPath = Path.Combine(ApplicationDataStorage.ApplicationUserDataPath, "data", "temp", "thumbnails");

		private static HashSet<string> folderNamesToPreserve = new HashSet<string>()
		{
			"profiles",
		};

		public static void DeleteCacheData(int daysOldToDelete)
		{
			// TODO: Enable once the cache mechanism is scene graph aware
			return;

			// delete everything in the GCodeOutputPath
			//   AppData\Local\MatterControl\data\gcode
			// delete everything in the temp data that is not in use
			//   AppData\Local\MatterControl\data\temp
			//     plateImages
			//     project-assembly
			//     project-extract
			//     stl
			// delete all unreferenced models in Library
			//   AppData\Local\MatterControl\Library
			// delete all old update downloads
			//   AppData\updates

			// start cleaning out unused data
			// MatterControl\data\gcode
			HashSet<string> referencedFilePaths = new HashSet<string>();
			CleanDirectory(ApplicationDataStorage.Instance.GCodeOutputPath, referencedFilePaths, daysOldToDelete);

			string userDataPath = ApplicationDataStorage.ApplicationUserDataPath;
			RemoveDirectory(Path.Combine(userDataPath, "updates"));

			// Get a list of all the stl and amf files referenced in the queue.
			foreach (PrintItemWrapper printItem in QueueData.Instance.PrintItems)
			{
				string fileLocation = printItem.FileLocation;
				if (!referencedFilePaths.Contains(fileLocation))
				{
					referencedFilePaths.Add(fileLocation);
					referencedFilePaths.Add(GetImageFileName(printItem));
				}
			}

			// NOTE: Why exclude PrintItemCollectionID == 0 items from these results
			var allPrintItems = Datastore.Instance.dbSQLite.Query<PrintItem>("SELECT * FROM PrintItem WHERE PrintItemCollectionID != 0;");

			// Add in all the stl and amf files referenced in the library.
			foreach (PrintItem printItem in allPrintItems)
			{
				var printItemWrapper = new PrintItemWrapper(printItem);
				if (!referencedFilePaths.Contains(printItem.FileLocation))
				{
					referencedFilePaths.Add(printItem.FileLocation);
					referencedFilePaths.Add(GetImageFileName(printItemWrapper));
				}
			}

			// If the count is less than 0 then we have never run and we need to populate the library and queue still. So don't delete anything yet.
			if (referencedFilePaths.Count > 0)
			{
				CleanDirectory(userDataPath, referencedFilePaths, daysOldToDelete);
			}
		}

		private static int CleanDirectory(string path, HashSet<string> referencedFilePaths, int daysOldToDelete)
		{
			int contentCount = 0;
			foreach (string directory in Directory.EnumerateDirectories(path))
			{
				int directoryContentCount = CleanDirectory(directory, referencedFilePaths, daysOldToDelete);
				if (directoryContentCount == 0
					&& !folderNamesToPreserve.Contains(Path.GetFileName(directory)))
				{
					try
					{
						Directory.Delete(directory);
					}
					catch (Exception)
					{
						GuiWidget.BreakInDebugger();
					}
				}
				else
				{
					// it has a directory that has content
					contentCount++;
				}
			}

			foreach (string file in Directory.EnumerateFiles(path, "*.*"))
			{
				bool fileIsNew = new FileInfo(file).LastAccessTime > DateTime.Now.AddDays(-daysOldToDelete);

				switch (Path.GetExtension(file).ToUpper())
				{
					case ".STL":
					case ".AMF":
					case ".GCODE":
					case ".PNG":
					case ".TGA":
						if (referencedFilePaths.Contains(file)
							|| fileIsNew)
						{
							contentCount++;
						}
						else
						{
							try
							{
								File.Delete(file);
							}
							catch (Exception)
							{
								GuiWidget.BreakInDebugger();
							}
						}
						break;

					case ".JSON":
						// may want to clean these up eventually
						contentCount++; // if we delete these we should not increment this
						break;

					default:
						// we have something in the directory that we are not going to delete
						contentCount++;
						break;
				}
			}

			return contentCount;
		}

		private static void RemoveDirectory(string directoryToRemove)
		{
			try
			{
				if (Directory.Exists(directoryToRemove))
				{
					Directory.Delete(directoryToRemove, true);
				}
			}
			catch (Exception)
			{
				GuiWidget.BreakInDebugger();
			}
		}

		private static string GetImageFileName(PrintItemWrapper item)
		{
			return GetImageFileName(item.FileHashCode);
		}

		private static string GetImageFileName(string stlHashCode)
		{
			string imageFileName = Path.Combine(ThumbnailsPath, "{0}_{1}x{2}.png".FormatWith(stlHashCode, BigRenderSize.x, BigRenderSize.y));

			string folderToSavePrintsTo = Path.GetDirectoryName(imageFileName);

			if (!Directory.Exists(folderToSavePrintsTo))
			{
				Directory.CreateDirectory(folderToSavePrintsTo);
			}

			return imageFileName;
		}
	}
}
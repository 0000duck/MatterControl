﻿/*
Copyright (c) 2017, Lars Brubaker, John Lewin
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MatterControl.Printing;
using MatterHackers.Agg;
using MatterHackers.Agg.Image;
using MatterHackers.Agg.Platform;
using MatterHackers.Agg.UI;
using MatterHackers.DataConverters3D;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.PrinterCommunication.Io;
using MatterHackers.MatterControl.PrintQueue;
using MatterHackers.MatterControl.SlicerConfiguration;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl.Library.Export
{
	public class GCodeExport : IExportPlugin, IExportWithOptions
	{
		private bool ForceSpiralVase;
		private PrinterConfig printer;

		public string ButtonText => "Machine File (G-Code)".Localize();

		public string FileExtension => ".gcode";

		public string ExtensionFilter => "Export GCode|*.gcode";

		public ImageBuffer Icon { get; } = AggContext.StaticData.LoadIcon(Path.Combine("filetypes", "gcode.png"));

		public void Initialize(PrinterConfig printer)
		{
			this.printer = printer;
		}

		public bool Enabled
		{
			// TODO: Someday we should show details on why we aren't enabled...
			//if (!showExportGCodeButton)
			//	var noGCodeMessage = "Note".Localize() + ": " + "To enable GCode export, select a printer profile.".Localize(),

			get => printer.Settings.PrinterSelected
				&& !printer.Settings.GetValue<bool>("enable_sailfish_communication");
		}

		public bool ExportPossible(ILibraryAsset libraryItem) => true;

		public GuiWidget GetOptionsPanel()
		{
			var container = new FlowLayoutWidget()
			{
				Margin = new BorderDouble(left: 40, bottom: 10),
			};

			var spiralVaseCheckbox = new CheckBox("Spiral Vase".Localize(), ActiveTheme.Instance.PrimaryTextColor, 10)
			{
				Checked = false,
				Cursor = Cursors.Hand,
			};
			spiralVaseCheckbox.CheckedStateChanged += (s, e) =>
			{
				this.ForceSpiralVase = spiralVaseCheckbox.Checked;
			};
			container.AddChild(spiralVaseCheckbox);

			// If print leveling is enabled then add in a check box 'Apply Leveling During Export' and default checked.
			if (printer.Settings.GetValue<bool>(SettingsKey.print_leveling_enabled))
			{
				var levelingCheckbox = new CheckBox("Apply leveling to G-Code during export".Localize(), ActiveTheme.Instance.PrimaryTextColor, 10)
				{
					Checked = true,
					Cursor = Cursors.Hand,
					Margin = new BorderDouble(left: 10)
				};
				levelingCheckbox.CheckedStateChanged += (s, e) =>
				{
					this.ApplyLeveling = levelingCheckbox.Checked;
				};
				container.AddChild(levelingCheckbox);
			}

			return container;
		}

		public async Task<bool> Generate(IEnumerable<ILibraryItem> libraryItems, string outputPath)
		{
			var firstItem = libraryItems.OfType<ILibraryAsset>().FirstOrDefault();
			if (firstItem != null)
			{
				IObject3D loadedItem = null;

				bool centerOnBed = true;

				if (firstItem.AssetPath == printer.Bed.EditContext.SourceFilePath)
				{
					// If item is bedplate, save any pending changes before starting the print
					await ApplicationController.Instance.Tasks.Execute("Saving".Localize(), printer.Bed.SaveChanges);
					loadedItem = printer.Bed.Scene;
					centerOnBed = false;
				}
				else if (firstItem is ILibraryObject3D object3DItem)
				{
					loadedItem = await object3DItem.CreateContent(null);
				}
				else if (firstItem is ILibraryAssetStream assetStream)
				{
					loadedItem = await assetStream.CreateContent(null);
				}

				if (loadedItem != null)
				{
					// Necessary to ensure scene or non-persisted ILibraryObject3D content is on disk before slicing
					loadedItem.PersistAssets(null);

					try
					{
						string sourceExtension = $".{firstItem.ContentType}";
						string assetPath = await AssetObject3D.AssetManager.StoreMcx(loadedItem, false);

						string fileHashCode = Path.GetFileNameWithoutExtension(assetPath);

						string gcodePath = PrintItemWrapper.GCodePath(fileHashCode);

						if (ApplicationSettings.ValidFileExtensions.IndexOf(sourceExtension, StringComparison.OrdinalIgnoreCase) >= 0
							|| string.Equals(sourceExtension, ".mcx", StringComparison.OrdinalIgnoreCase))
						{
							if (centerOnBed)
							{
								// Get Bounds
								var aabb = loadedItem.GetAxisAlignedBoundingBox(Matrix4X4.Identity);

								// Move to bed center
								var bedCenter = printer.Bed.BedCenter;
								loadedItem.Matrix *= Matrix4X4.CreateTranslation((double)-aabb.Center.X, (double)-aabb.Center.Y, (double)-aabb.minXYZ.Z) * Matrix4X4.CreateTranslation(bedCenter.X, bedCenter.Y, 0);
								loadedItem.Color = loadedItem.Color;
							}

							string originalSpiralVase = printer.Settings.GetValue(SettingsKey.spiral_vase);

							// Slice
							try
							{
								if (this.ForceSpiralVase)
								{
									printer.Settings.SetValue(SettingsKey.spiral_vase, "1");
								}

								await ApplicationController.Instance.Tasks.Execute("Slicing Item".Localize() + " " + loadedItem.Name, (reporter, cancellationToken) =>
								{
									return Slicer.SliceItem(loadedItem, gcodePath, printer, reporter, cancellationToken);
								});
							}
							finally
							{
								if (this.ForceSpiralVase)
								{
									printer.Settings.SetValue(SettingsKey.spiral_vase, originalSpiralVase);
								}
							}
						}

						if (File.Exists(gcodePath))
						{
							ApplyStreamPipelineAndExport(gcodePath, outputPath);
							return true;
						}
					}
					catch
					{
					}
				}
			}

			return false;
		}

		public bool ApplyLeveling { get; set; } = true;

		private void ApplyStreamPipelineAndExport(GCodeFileStream gCodeFileStream, string outputPath)
		{
			try
			{
				bool addLevelingStream = printer.Settings.GetValue<bool>(SettingsKey.print_leveling_enabled) && this.ApplyLeveling;
				var queueStream = new QueuedCommandsStream(gCodeFileStream);

				// this is added to ensure we are rewriting the G0 G1 commands as needed
				GCodeStream finalStream = addLevelingStream
					? new ProcessWriteRegexStream(printer.Settings, new PrintLevelingStream(printer.Settings, queueStream, false), queueStream)
					: new ProcessWriteRegexStream(printer.Settings, queueStream, queueStream);

				// Run each line from the source gcode through the loaded pipeline and dump to the output location
				using (var file = new StreamWriter(outputPath))
				{
					string nextLine = finalStream.ReadLine();
					while (nextLine != null)
					{
						if (nextLine.Trim().Length > 0)
						{
							file.WriteLine(nextLine);
						}

						nextLine = finalStream.ReadLine();
					}
				}
			}
			catch (Exception e)
			{
				UiThread.RunOnIdle(() =>
				{
					StyledMessageBox.ShowMessageBox(e.Message, "Couldn't save file".Localize());
				});
			}
		}

		private void ApplyStreamPipelineAndExport(string gcodeFilename, string outputPath)
		{
			try
			{
				this.ApplyStreamPipelineAndExport(
					new GCodeFileStream(
						GCodeFile.Load(
							gcodeFilename,
							new Vector4(),
							new Vector4(),
							new Vector4(),
							Vector4.One,
							CancellationToken.None)),
					outputPath);
			}
			catch (Exception e)
			{
				UiThread.RunOnIdle(() =>
				{
					StyledMessageBox.ShowMessageBox(e.Message, "Couldn't load file".Localize());
				});
			}
		}
	}
}

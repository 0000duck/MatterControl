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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MatterHackers.Agg;
using MatterHackers.Agg.Platform;
using MatterHackers.Agg.UI;
using MatterHackers.DataConverters3D;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.CustomWidgets;
using MatterHackers.MatterControl.DataStorage;
using MatterHackers.MatterControl.Library;
using MatterHackers.MatterControl.PrinterControls.PrinterConnections;
using MatterHackers.MatterControl.PrintLibrary;
using MatterHackers.MatterControl.SlicerConfiguration;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl.PartPreviewWindow
{
	public enum ViewControls3DButtons
	{
		Rotate,
		Scale,
		Translate,
		PartSelect
	}

	public enum PartViewMode
	{
		Layers2D,
		Layers3D,
		Model
	}

	public class ViewModeChangedEventArgs : EventArgs
	{
		public PartViewMode ViewMode { get; set; }
	}

	public class TransformStateChangedEventArgs : EventArgs
	{
		public ViewControls3DButtons TransformMode { get; set; }
	}

	public class ViewControls3D : OverflowBar
	{
		public event EventHandler ResetView;

		public event EventHandler<TransformStateChangedEventArgs> TransformStateChanged;

		private RadioIconButton translateButton;
		private RadioIconButton rotateButton;
		private RadioIconButton scaleButton;
		private RadioIconButton partSelectButton;

		private View3DWidget view3DWidget;
		private BedConfig sceneContext;

		private ViewControls3DButtons activeTransformState = ViewControls3DButtons.PartSelect;
		private List<(GuiWidget button, SceneSelectionOperation operation)> operationButtons;

		public NamedAction[] MenuActions { get; private set; }

		internal void NotifyResetView()
		{
			this.ResetView.Invoke(this, null);
		}

		public bool IsPrinterMode { get; }

		public ViewControls3DButtons ActiveButton
		{
			get
			{
				return activeTransformState;
			}
			set
			{
				this.activeTransformState = value;
				switch (this.activeTransformState)
				{
					case ViewControls3DButtons.Rotate:
						if (rotateButton != null)
						{
							rotateButton.Checked = true;
						}

						break;

					case ViewControls3DButtons.Translate:
						if (translateButton != null)
						{
							translateButton.Checked = true;
						}

						break;

					case ViewControls3DButtons.Scale:
						if (scaleButton != null)
						{
							scaleButton.Checked = true;
						}

						break;

					case ViewControls3DButtons.PartSelect:
						if (partSelectButton != null)
						{
							partSelectButton.Checked = true;
						}
						break;
				}

				TransformStateChanged?.Invoke(this, new TransformStateChangedEventArgs()
				{
					TransformMode = activeTransformState
				});
			}
		}

		internal void SetView3DWidget(View3DWidget view3DWidget)
		{
			this.view3DWidget = view3DWidget;
		}

		public ViewControls3D(BedConfig sceneContext, ThemeConfig theme, UndoBuffer undoBuffer, bool isPrinterType, bool showPrintButton)
			: base(theme)
		{
			this.ActionArea.Click += (s, e) =>
			{
				view3DWidget.InteractionLayer.Focus();
			};

			this.IsPrinterMode = isPrinterType;
			this.sceneContext = sceneContext;

			string iconPath;

			this.AddChild(CreateAddButton(sceneContext, theme));

			this.AddChild(new ToolbarSeparator(theme));

			var optionsButton = new PopupMenuButton(AggContext.StaticData.LoadIcon("bed.png", 16, 16, theme.InvertIcons), theme)
			{
				Name = "Bed Options Menu",
				//ToolTipText = "Options",
				Enabled = true,
				Margin = theme.ButtonSpacing,
				VAnchor = VAnchor.Center,
				DrawArrow = true
			};
			optionsButton.DynamicPopupContent = () =>
			{
				var popupMenu = new PopupMenu(ApplicationController.Instance.MenuTheme);
				theme.CreateMenuItems(popupMenu, this.BedMenuActions(sceneContext, theme), emptyMenu: false);

				return popupMenu;
			};
			this.AddChild(optionsButton);

			this.AddChild(new ToolbarSeparator(theme));

			var undoButton = new IconButton(AggContext.StaticData.LoadIcon("Undo_grey_16x.png", 16, 16, theme.InvertIcons), theme)
			{
				Name = "3D View Undo",
				ToolTipText = "Undo",
				Enabled = false,
				Margin = theme.ButtonSpacing,
				VAnchor = VAnchor.Center
			};
			undoButton.Click += (sender, e) =>
			{
				var selectedItem = sceneContext.Scene.SelectedItem;
				sceneContext.Scene.SelectedItem = null;
				undoBuffer.Undo();
				view3DWidget.InteractionLayer.Focus();
				// if the item we had selected is still in the scene, re-select it
				if(sceneContext.Scene.Children.Contains(selectedItem))
				{
					sceneContext.Scene.SelectedItem = selectedItem;
				}
			};
			this.AddChild(undoButton);

			var redoButton = new IconButton(AggContext.StaticData.LoadIcon("Redo_grey_16x.png", 16, 16, theme.InvertIcons), theme)
			{
				Name = "3D View Redo",
				Margin = theme.ButtonSpacing,
				ToolTipText = "Redo",
				Enabled = false,
				VAnchor = VAnchor.Center
			};
			redoButton.Click += (sender, e) =>
			{
				var selectedItem = sceneContext.Scene.SelectedItem;
				sceneContext.Scene.SelectedItem = null;
				undoBuffer.Redo();
				view3DWidget.InteractionLayer.Focus();
				// if the item we had selected is still in the scene, re-select it
				if (sceneContext.Scene.Children.Contains(selectedItem))
				{
					sceneContext.Scene.SelectedItem = selectedItem;
				}
			};
			this.AddChild(redoButton);

			this.AddChild(CreateSaveButton(theme));

			if (showPrintButton)
			{
				var printButton = new TextButton("Print", theme)
				{
					Name = "Print Button",
					BackgroundColor = theme.AccentMimimalOverlay
				};
				printButton.Click += (s, e) =>
				{
					if (ProfileManager.Instance.Profiles.Count <= 0)
					{
						//Launch window to prompt user to sign in
						UiThread.RunOnIdle(() => DialogWindow.Show(new SetupStepMakeModelName()));
					}
					// If no active printer but profiles exist, show select printer
					// If printer exists, stash plate with undo operation, then load this scene onto the printer bed
					else if (ApplicationController.Instance.ActivePrinter is PrinterConfig printer && printer.Settings.PrinterSelected)
					{
						Task.Run(async () =>
						{
							await ApplicationController.Instance.Tasks.Execute("Saving".Localize(), sceneContext.SaveChanges);

							// Clear bed to get new MCX on disk for this item
							printer.Bed.ClearPlate();

							var editContext = sceneContext.EditContext;

							await printer.Bed.LoadContent(editContext);

							// Switch to printer
							ApplicationController.Instance.AppView.TabControl.SelectedTabKey = printer.Settings.GetValue(SettingsKey.printer_name);

							// Slice and print
							await ApplicationController.Instance.PrintPart(
								editContext,
								printer,
								null,
								CancellationToken.None);
						});
					}
					
				};
				this.AddChild(printButton);
			}

			this.AddChild(new ToolbarSeparator(theme));

			undoBuffer.Changed += (sender, e) =>
			{
				undoButton.Enabled = undoBuffer.UndoCount > 0;
				redoButton.Enabled = undoBuffer.RedoCount > 0;
			};

			undoButton.Enabled = undoBuffer.UndoCount > 0;
			redoButton.Enabled = undoBuffer.RedoCount > 0;

			var buttonGroupA = new ObservableCollection<GuiWidget>();

			if (UserSettings.Instance.IsTouchScreen)
			{
				iconPath = Path.Combine("ViewTransformControls", "rotate.png");
				rotateButton = new RadioIconButton(AggContext.StaticData.LoadIcon(iconPath, 32, 32, theme.InvertIcons), theme)
				{
					SiblingRadioButtonList = buttonGroupA,
					ToolTipText = "Rotate (Alt + Left Mouse)".Localize(),
					Margin = theme.ButtonSpacing
				};
				rotateButton.Click += (s, e) => this.ActiveButton = ViewControls3DButtons.Rotate;
				buttonGroupA.Add(rotateButton);
				AddChild(rotateButton);

				iconPath = Path.Combine("ViewTransformControls", "translate.png");
				translateButton = new RadioIconButton(AggContext.StaticData.LoadIcon(iconPath, 32, 32, theme.InvertIcons), theme)
				{
					SiblingRadioButtonList = buttonGroupA,
					ToolTipText = "Move (Shift + Left Mouse)".Localize(),
					Margin = theme.ButtonSpacing
				};
				translateButton.Click += (s, e) => this.ActiveButton = ViewControls3DButtons.Translate;
				buttonGroupA.Add(translateButton);
				AddChild(translateButton);

				iconPath = Path.Combine("ViewTransformControls", "scale.png");
				scaleButton = new RadioIconButton(AggContext.StaticData.LoadIcon(iconPath, 32, 32, theme.InvertIcons), theme)
				{
					SiblingRadioButtonList = buttonGroupA,
					ToolTipText = "Zoom (Ctrl + Left Mouse)".Localize(),
					Margin = theme.ButtonSpacing
				};
				scaleButton.Click += (s, e) => this.ActiveButton = ViewControls3DButtons.Scale;
				buttonGroupA.Add(scaleButton);
				AddChild(scaleButton);

				rotateButton.Checked = true;

				// Add vertical separator
				this.AddChild(new ToolbarSeparator(theme));

				iconPath = Path.Combine("ViewTransformControls", "partSelect.png");
				partSelectButton = new RadioIconButton(AggContext.StaticData.LoadIcon(iconPath, 32, 32, theme.InvertIcons), theme)
				{
					SiblingRadioButtonList = buttonGroupA,
					ToolTipText = "Select Part".Localize(),
					Margin = theme.ButtonSpacing
				};
				partSelectButton.Click += (s, e) => this.ActiveButton = ViewControls3DButtons.PartSelect;
				buttonGroupA.Add(partSelectButton);
				AddChild(partSelectButton);
			}

			operationButtons = new List<(GuiWidget, SceneSelectionOperation)>();

			// Add Selected IObject3D -> Operations to toolbar
			foreach (var namedAction in ApplicationController.Instance.RegisteredSceneOperations)
			{
				if (namedAction is SceneSelectionSeparator)
				{
					this.AddChild(new ToolbarSeparator(theme));
					continue;
				}

				GuiWidget button;

				if (namedAction.Icon != null)
				{
					button = new IconButton(namedAction.Icon, theme)
					{
						Name = namedAction.Title + " Button",
						ToolTipText = namedAction.Title,
						Margin = theme.ButtonSpacing,
						BackgroundColor = theme.ToolbarButtonBackground,
						HoverColor = theme.ToolbarButtonHover,
						MouseDownColor = theme.ToolbarButtonDown,
					};
				}
				else
				{
					button = new TextButton(namedAction.Title, theme)
					{
						Name = namedAction.Title + " Button",
						Margin = theme.ButtonSpacing,
						BackgroundColor = theme.ToolbarButtonBackground,
						HoverColor = theme.ToolbarButtonHover,
						MouseDownColor = theme.ToolbarButtonDown,
					};
				}

				operationButtons.Add((button, namedAction));

				button.Click += (s, e) =>
				{
					UiThread.RunOnIdle(() =>
					{
						namedAction.Action.Invoke(sceneContext.Scene);
						var partTab = button.Parents<PartTabPage>().FirstOrDefault();
						var view3D = partTab.Descendants<View3DWidget>().FirstOrDefault();
						view3D.InteractionLayer.Focus();
					});
				};
				this.AddChild(button);
			}

			sceneContext.Scene.SelectionChanged += Scene_SelectionChanged;

			// Run on load
			Scene_SelectionChanged(null, null);
		}

		private void Scene_SelectionChanged(object sender, EventArgs e)
		{
			// Set enabled level based on operation rules
			foreach(var item in operationButtons)
			{
				item.button.Enabled = item.operation.IsEnabled?.Invoke(sceneContext.Scene) ?? false;
			}
		}

		private PartPreviewContent partPreviewContent = null;

		private GuiWidget CreateAddButton(BedConfig sceneContext, ThemeConfig theme)
		{
			var buttonView = new TextIconButton(
				"",
				AggContext.StaticData.LoadIcon("cube_add.png", 16, 16, theme.InvertIcons),
				theme);

			// Remove right Padding for drop style
			buttonView.Padding = buttonView.Padding.Clone(right: 0);

			var popupMenu = new PopupMenu(ApplicationController.Instance.MenuTheme)
			{
				HAnchor = HAnchor.Fit,
				VAnchor = VAnchor.Fit,
			};

			// Construct and store menu actions
			this.MenuActions = this.BedMenuActions(sceneContext, ApplicationController.Instance.MenuTheme);

			PopupMenuButton libraryPopup = null;

			libraryPopup = new PopupMenuButton(buttonView, theme)
			{
				MakeScrollable = false,
				Name = "Add Content Menu",
				ToolTipText = "Add Content".Localize(),
				AlwaysKeepOpen = true,
				DynamicPopupContent = () =>
				{
					if (partPreviewContent == null)
					{
						partPreviewContent = this.Parents<PartPreviewContent>().FirstOrDefault();
					}

					var verticalResizeContainer = new VerticalResizeContainer(theme, GrabBarSide.Right)
					{
						BackgroundColor = theme.TabBarBackground,
						Padding = new BorderDouble(theme.DefaultContainerPadding / 2, 0),
						MinimumSize = new Vector2(120, 50),
						Height = libraryPopup.TransformToScreenSpace(libraryPopup.Position).Y,
					};

					double.TryParse(UserSettings.Instance.get(UserSettingsKey.PopupLibraryWidth), out double controlWidth);
					if (controlWidth == 0)
					{
						controlWidth = 400;
					}
					verticalResizeContainer.Width = controlWidth;

					verticalResizeContainer.BoundsChanged += (s2, e2) =>
					{
						UserSettings.Instance.set(UserSettingsKey.PopupLibraryWidth, verticalResizeContainer.Width.ToString());
					};


					var systemWindow = this.Parents<SystemWindow>().FirstOrDefault();

					var printLibraryWidget = new PrintLibraryWidget(partPreviewContent, theme)
					{
						HAnchor = HAnchor.Stretch,
						VAnchor = VAnchor.Absolute,
						Height = libraryPopup.TransformToScreenSpace(libraryPopup.Position).Y,
						Margin = new BorderDouble(left: verticalResizeContainer.SplitterWidth)
					};

					systemWindow.SizeChanged += (s, e) =>
					{
						printLibraryWidget.Height = libraryPopup.TransformToScreenSpace(libraryPopup.Position).Y;
					};

					verticalResizeContainer.AddChild(printLibraryWidget);

					systemWindow.MouseDown += systemWindownMouseDown;

					void systemWindownMouseDown(object s2, MouseEventArgs mouseEvent)
					{
						// MouseUp on our SystemWindow outside of our bounds should call close
						var resizeContainerMousePosition = verticalResizeContainer.TransformFromScreenSpace(mouseEvent.Position);
						bool mouseUpOnWidget = resizeContainerMousePosition.X >= 0 && resizeContainerMousePosition.X <= verticalResizeContainer.Width
							&& resizeContainerMousePosition.Y >= 0 && resizeContainerMousePosition.Y <= verticalResizeContainer.Height;

						if (!mouseUpOnWidget)
						{
							libraryPopup.CloseMenu();
							systemWindow.MouseDown -= systemWindownMouseDown;
						}
					};

					return verticalResizeContainer;
				},
				BackgroundColor = theme.ToolbarButtonBackground,
				HoverColor = theme.ToolbarButtonHover,
				MouseDownColor = theme.ToolbarButtonDown,
				DrawArrow = true,
				Margin = theme.ButtonSpacing,
			};

			libraryPopup.ConfigurePopup += (s, e) =>
			{
				e.HAnchor = HAnchor.Fit;
				e.VAnchor = VAnchor.Fit;
			};

			return libraryPopup;
		}

		private GuiWidget CreateSaveButton(ThemeConfig theme)
		{
			var iconButton = new IconButton(
				AggContext.StaticData.LoadIcon("save_grey_16x.png", 16, 16, theme.InvertIcons),
				theme)
			{
				ToolTipText = "Save".Localize(),
			};

			iconButton.Click += (s, e) =>
			{
				ApplicationController.Instance.Tasks.Execute("Saving".Localize(), sceneContext.SaveChanges).ConfigureAwait(false);
			};

			// Remove right Padding for drop style
			iconButton.Padding = iconButton.Padding.Clone(right: 0);

			var button = new PopupMenuButton(iconButton, theme)
			{
				Name = "Save SplitButton",
				ToolTipText = "Save As".Localize(),
				DynamicPopupContent = () =>
				{
					var popupMenu = new PopupMenu(ApplicationController.Instance.MenuTheme);

					var saveAs = popupMenu.CreateMenuItem("Save As".Localize());
					saveAs.Click += (s, e) => UiThread.RunOnIdle(() =>
					{
						UiThread.RunOnIdle(() =>
						{
							DialogWindow.Show(
								new SaveAsPage(
									async (newName, destinationContainer) =>
									{
										// Save to the destination provider
										if (destinationContainer is ILibraryWritableContainer writableContainer)
										{
											// Wrap stream with ReadOnlyStream library item and add to container
											writableContainer.Add(new[]
												{
											new InMemoryLibraryItem(sceneContext.Scene)
											{
												Name = newName
											}
											});

											destinationContainer.Dispose();
										}
									}));
						});
					});

					return popupMenu;
				},
				BackgroundColor = theme.ToolbarButtonBackground,
				HoverColor = theme.ToolbarButtonHover,
				MouseDownColor = theme.ToolbarButtonDown,
				DrawArrow = true,
				Margin = theme.ButtonSpacing,
			};

			iconButton.Selectable = true;

			return button;
		}

		public static async void LoadAndAddPartsToPlate(GuiWidget originatingWidget, string[] filesToLoad, BedConfig sceneContext)
		{
			if (filesToLoad != null && filesToLoad.Length > 0)
			{
				
				await Task.Run(() => loadAndAddPartsToPlate(filesToLoad, sceneContext));

				if (originatingWidget.HasBeenClosed)
				{
					return;
				}

				var scene = sceneContext.Scene;

				bool addingOnlyOneItem = scene.Children.Count == scene.Children.Count + 1;

				if (scene.HasChildren())
				{
					if (addingOnlyOneItem)
					{
						// if we are only adding one part to the plate set the selection to it
						scene.SelectLastChild();
					}
				}

				scene.Invalidate(new InvalidateArgs(null, InvalidateType.Content, null));
			}
		}

		private static async Task loadAndAddPartsToPlate(string[] filesToLoadIncludingZips, BedConfig sceneContext)
		{
			if (filesToLoadIncludingZips?.Any() == true)
			{
				var scene = sceneContext.Scene;

				// When a single gcode file is selected, swap the plate to the new GCode content
				if (filesToLoadIncludingZips.Count() == 1
					&& filesToLoadIncludingZips.FirstOrDefault() is string firstFilePath
					&& Path.GetExtension(firstFilePath).ToUpper() == ".GCODE")
				{
					// Drop handler for special case of GCode or similar (change loaded scene to new context)
					await sceneContext.LoadContent(
						new EditContext()
						{
							SourceItem = new FileSystemFileItem(firstFilePath),
							// No content store for GCode, otherwise PlatingHistory
							ContentStore = sceneContext.EditContext.ContentStore
						});

					return;
				}

				List<string> filesToLoad = new List<string>();
				foreach (string loadedFileName in filesToLoadIncludingZips)
				{
					string extension = Path.GetExtension(loadedFileName).ToUpper();
					if ((extension != ""
						&& extension != ".ZIP"
						&& extension != ".GCODE"
						&& ApplicationController.Instance.Library.IsContentFileType(loadedFileName))
						)
					{
						filesToLoad.Add(loadedFileName);
					}
					else if (extension == ".ZIP")
					{
						List<PrintItem> partFiles = ProjectFileHandler.ImportFromProjectArchive(loadedFileName);
						if (partFiles != null)
						{
							foreach (PrintItem part in partFiles)
							{
								string itemExtension = Path.GetExtension(part.FileLocation).ToUpper();
								if (itemExtension != ".GCODE")
								{
									filesToLoad.Add(part.FileLocation);
								}
							}
						}
					}
				}

				var itemCache = new Dictionary<string, IObject3D>();

				foreach (string filePath in filesToLoad)
				{
					var libraryItem = new FileSystemFileItem(filePath);

					IObject3D object3D = null;

					await ApplicationController.Instance.Tasks.Execute("Loading".Localize() + " " + Path.GetFileName(filePath), async (progressReporter, cancellationToken) =>
					{
						var progressStatus = new ProgressStatus();

						progressReporter.Report(progressStatus);

						object3D = await libraryItem.CreateContent((double progress0To1, string processingState) =>
						{
							progressStatus.Progress0To1 = progress0To1;
							progressStatus.Status = processingState;
							progressReporter.Report(progressStatus);
						});
					});

					if (object3D != null)
					{
						scene.Children.Modify(list => list.Add(object3D));

						PlatingHelper.MoveToOpenPositionRelativeGroup(object3D, scene.Children);
					}
				}
			}
		}

		private NamedAction[] BedMenuActions(BedConfig sceneContext, ThemeConfig theme)
		{
			return new[]
			{
				new NamedAction()
				{
					ID = "Cut",
					Title = "Cut".Localize(),
					Shortcut = "Ctrl+X",
					Action = () =>
					{
						sceneContext.Scene.Cut();
					},
					IsEnabled = () => sceneContext.Scene.SelectedItem != null
				},
				new NamedAction()
				{
					ID = "Copy",
					Title = "Copy".Localize(),
					Shortcut = "Ctrl+C",
					Action = () =>
					{
						sceneContext.Scene.Copy();
					},
					IsEnabled = () => sceneContext.Scene.SelectedItem != null
				},
				new NamedAction()
				{
					ID = "Paste",
					Title = "Paste".Localize(),
					Shortcut = "Ctrl+V",
					Action = () =>
					{
						sceneContext.Scene.Paste();
					},
					IsEnabled = () => Clipboard.Instance.ContainsImage || Clipboard.Instance.GetText() == "!--IObjectSelection--!"
				},
				new ActionSeparator(),
				new NamedAction()
				{
					ID = "Export",
					Title = "Export".Localize(),
					Icon = AggContext.StaticData.LoadIcon("cube_export.png", 16, 16),
					Action = () =>
					{
						UiThread.RunOnIdle(async () =>
						{
							DialogWindow.Show(
								new ExportPrintItemPage(new[]
								{
									new InMemoryLibraryItem(sceneContext.Scene)
								}, false));
						});
					},
					IsEnabled = () => sceneContext.EditableScene
						|| (sceneContext.EditContext.SourceItem is ILibraryAsset libraryAsset
							&& string.Equals(Path.GetExtension(libraryAsset.FileName) ,".gcode" ,StringComparison.OrdinalIgnoreCase))
				},
				new NamedAction()
				{
					ID = "ArrangeAll",
					Title = "Arrange All Parts".Localize(),
					Action = () =>
					{
						sceneContext.Scene.AutoArrangeChildren(view3DWidget);
					},
					IsEnabled = () => sceneContext.EditableScene
				},
				new ActionSeparator(),
				new NamedAction()
				{
					ID = "ClearBed",
					Title = "Clear Bed".Localize(),
					Action = () =>
					{
						UiThread.RunOnIdle(() =>
						{
							view3DWidget.ClearPlate();
						});
					}
				},
#if DEBUG && false
				new NamedAction()
				{
					Title = "GC.Collect"/* Don't localize debug tool */,
					Action = () =>
					{
						UiThread.RunOnIdle(() =>
						{
							GC.Collect();
						});
					}
				},
#endif
			};
		}

		public override void OnClosed(EventArgs e)
		{
			sceneContext.Scene.SelectionChanged -= Scene_SelectionChanged;
			base.OnClosed(e);
		}
	}
}
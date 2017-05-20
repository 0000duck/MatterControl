﻿/*
Copyright (c) 2017, John Lewin
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

using System.Threading.Tasks;

namespace MatterHackers.MatterControl
{
	using System;
	using System.IO;
	using MatterHackers.Agg;
	using MatterHackers.Agg.Image;
	using MatterHackers.Agg.PlatformAbstract;
	using MatterHackers.DataConverters3D;
	using MatterHackers.MatterControl.Library;
	using MatterHackers.PolygonMesh;
	using MatterHackers.RayTracer;
	using MatterHackers.PolygonMesh.Processors;
	using MatterHackers.MatterControl.PrinterCommunication;
	using MatterHackers.VectorMath;

	/// <summary>
	/// Loads IObject3D and thumbnails for mesh based ILibraryItem objects
	/// </summary>
	public class MeshContentProvider : ISceneContentProvider
	{
		private const int tooBigAndroid = 50000000;
		private const int tooBigDesktop = 250000000;

		private static readonly bool Is32Bit = IntPtr.Size == 4;
		private static readonly int MaxFileSize = (OsInformation.OperatingSystem == OSType.Android) ? tooBigAndroid : tooBigDesktop;
		private static readonly Point2D BigRenderSize = new Point2D(460, 460);

		public ContentResult CreateItem(ILibraryItem item, ReportProgressRatio progressReporter)
		{
			var sceneItem = new Object3D()
			{
				// Initial 'Loading...' mesh
				Mesh = PlatonicSolids.CreateCube(1, 3, 1),
			};

			return new ContentResult()
			{
				Object3D = sceneItem,
				MeshLoaded = Task.Run(async () =>
				{
					IObject3D loadedItem = null;

					try
					{
						var streamInterface = item as ILibraryContentStream;
						if (streamInterface != null)
						{
							using (var contentStream = await streamInterface.GetContentStream(progressReporter))
							{
								if (contentStream != null)
								{
									// TODO: Wire up caching
									loadedItem = Object3D.Load(contentStream.Stream, Path.GetExtension(streamInterface.FileName), null /*itemCache*/, progressReporter);
								}
							}
						}
						else
						{
							var contentInterface = item as ILibraryContentItem;
							loadedItem = await contentInterface?.GetContent(progressReporter);
						}
					}
					catch { }

					if (loadedItem != null)
					{
						var aabb = loadedItem.GetAxisAlignedBoundingBox(Matrix4X4.Identity);

						sceneItem.Mesh = loadedItem.Mesh;
						sceneItem.Children = loadedItem.Children;
						sceneItem.Matrix *= Matrix4X4.CreateTranslation(-aabb.Center.x, -aabb.Center.y, -aabb.minXYZ.z);
					}
				})
			};
		}

		// TODO: Trying out an 8 MB mesh max for thumnbnail generation
		long MaxFileSizeForTracing = 8 * 1000 * 1000;

		public async Task GetThumbnail(ILibraryItem item, int width, int height, Action<ImageBuffer> imageCallback)
		{
			IObject3D object3D = null;

			var contentModel = item as ILibraryContentStream;
			if (contentModel != null 
				&& contentModel.FileSize < MaxFileSizeForTracing)
			{
				// TODO: Wire up limits for thumbnail generation. If content is too big, return null allowing the thumbnail to fall back to content default
				var contentResult = contentModel.CreateContent();
				if (contentModel != null)
				{
					await contentResult.MeshLoaded;
					object3D = contentResult.Object3D;
				}
			}
			else if (item is ILibraryContentItem)
			{
				object3D = await (item as ILibraryContentItem)?.GetContent(null);
			}

			if (object3D != null)
			{
				if (object3D.Mesh == null && object3D.HasChildren)
				{
					object3D = new Object3D()
					{
						ItemType = Object3DTypes.Model,
						Mesh = MeshFileIo.DoMerge(object3D.ToMeshGroupList(), new MeshOutputSettings())
					};
				}

				if (object3D.Mesh == null)
				{
					return;
				}

				var thumbnail = this.TraceThumbnail(object3D, BigRenderSize.x, BigRenderSize.y);
				if (thumbnail != null)
				{
					string cachePath = ApplicationController.Instance.CachePath(item);
					ImageIO.SaveImageData(cachePath, thumbnail);

					imageCallback(thumbnail.MultiplyWithPrimaryAccent());
				}
				else
				{
					imageCallback(DefaultImage.MultiplyWithPrimaryAccent());
				}
			}
		}

		private ImageBuffer TraceThumbnail(IObject3D object3D, int width, int height)
		{
			/* TODO: from PrimitivesSideBar.cs - what's the value in thumb.CopyFrom rather than using directly?
			var thumbnailIcon = new ImageBuffer(width, height);
			thumbnailIcon.SetRecieveBlender(new BlenderPreMultBGRA());
			thumbnailIcon.CopyFrom(tracer.destImage); */

			// TODO - necessary - disabling doesn't seem to change much? 
			object3D.Mesh?.Triangulate();

			var tracer = new ThumbnailTracer(object3D, width, height)
			{
				MultiThreaded = !PrinterConnectionAndCommunication.Instance.PrinterIsPrinting
			};
			tracer.TraceScene();

			var thumbnailIcon = tracer.destImage;
			if (thumbnailIcon != null)
			{
				thumbnailIcon.SetRecieveBlender(new BlenderPreMultBGRA());
			}

			return thumbnailIcon;
		}

		public ImageBuffer DefaultImage => LibraryProviderHelpers.LoadInvertIcon("mesh.png");

		private static bool MeshIsTooBigToLoad(string fileLocation)
		{
			if (Is32Bit && File.Exists(fileLocation))
			{
				// Mesh is too big if the estimated size is greater than Max
				return MeshFileIo.GetEstimatedMemoryUse(fileLocation) > MaxFileSize;
			}

			return false;
		}
	}

	/*
 		// stlHashCode = itemWrapper.FileHashCode.ToString();
		// defaultImage = StaticData.Instance.LoadIcon("part_icon_transparent_100x100.png")  --- for the BigRender PrintingWindow case
	 */

	// TODO - How to handle g-code
	// - Seems like a content provider could supply the icon below
	// - GCode content is invalid for the scene though and shouldn't support drag or double click
	// - GCode supports the 'Printable' action
	/*
	 *
	 *
	 * if (Path.GetExtension(item.FileName).ToUpper() == ".GCODE")
		{
			var center = new Vector2(width / 2.0, height / 2.0);

			var thumbnailImage = new ImageBuffer(width, height);

			var graphics2D = thumbnailImage.NewGraphics2D();
			graphics2D.DrawString("GCode", center.x, center.y, 8 * width / 50, Justification.Center, Baseline.BoundsCenter, color: RGBA_Bytes.White);

			graphics2D.Render(
				new Stroke(
					new Ellipse(center, width / 2 - width / 12),
					width / 12),
				RGBA_Bytes.White);

			return thumbnailImage;
		}
	 */


	/*

	// TODO: Only load items from cache. If the parent container does not have an image and we don't have the asset in our cache, we should show the container default image
	if (itemsNeedingLoad.Any())
	{
		Task.Run(async () =>
		{
			var itemsNeedingGenerate = new List<ListViewItem>();

			foreach (var listItem in itemsNeedingLoad.Where(i => i.Model is ILibraryContentItem))
			{
				string cachePath = CachePath(listItem.Model, width, height);

				// Check the source container for an existing/override image
				var image = await sourceContainer.GetThumbnail(listItem.Model, width, height);

				if (image == null && listItem.Model is IThumbnail)
				{
					image = await (listItem.Model as IThumbnail).GetThumbnail(width, height);
				}

				if (image != null)
				{
					// Persist generated image
					ImageIO.SaveImageData(cachePath, image);
					listItem.ThumbnailTarget.Image = image;
				}
				else
				{
					itemsNeedingGenerate.Add(listItem);
				}
			}

			/*
			foreach (var listItem in itemsNeedingGenerate.Where(i => i.Model is ILibraryContentItem))
			{
				// If not found, do the 
				var model = listItem.Model as ILibraryContentItem;
				var object3D = await model.GetContent(null);
				if (object3D != null)
				{
					var image = ApplicationController.Instance.GenerateThumbnail(model, object3D, width, height);
					if (image != null)
					{
						string cachePath = CachePath(listItem.Model, width, height);
						// Persist generated image
						ImageIO.SaveImageData(cachePath, image);

						listItem.ThumbnailTarget.Image = image;
					}

					// Notify container
					sourceContainer.SetThumbnail(model, width, height, image);
				}

				// Wait one second between thumbnail RayTracings
				await Task.Delay(1000);
			} 
		});
	} */
}
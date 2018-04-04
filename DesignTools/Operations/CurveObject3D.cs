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

using System.ComponentModel;
using System.Linq;
using System.Threading;
using MatterHackers.Agg.UI;
using MatterHackers.DataConverters3D;
using MatterHackers.MatterControl.PartPreviewWindow.View3D;
using MatterHackers.PolygonMesh;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl.DesignTools
{
	public class CurveObject3D : MeshWrapperObject3D, IRebuildable
	{
		[DisplayName("Angle")]
		public double AngleDegrees { get; set; } = 0;

		[DisplayName("Bend Up")]
		public bool BendCW { get; set; } = true;

		public CurveObject3D()
		{
		}

		public void Rebuild(UndoBuffer undoBuffer)
		{
			var meshWrapper = this.Descendants()
				.Where((obj) => obj.OwnerID == this.ID).ToList();

			foreach (var items in meshWrapper.Select((mw) => (Original: mw.Children.First(),
				 Transformed: mw)))
			{
				var transformedMesh = items.Transformed.Mesh;
				var originalMesh = items.Original.Mesh;

				// reset the positions before we take the aabb
				for (int i = 0; i < transformedMesh.Vertices.Count; i++)
				{
					transformedMesh.Vertices[i].Position = originalMesh.Vertices[i].Position;
				}

				transformedMesh.MarkAsChanged();
			}

			var aabb = this.GetAxisAlignedBoundingBox();

			foreach (var items in meshWrapper.Select((mw) => (Original: mw.Children.First(),
				 Transformed: mw)))
			{
				var transformedMesh = items.Transformed.Mesh;
				var originalMesh = items.Original.Mesh;
				var itemMatrix = items.Original.WorldMatrix(this);
				var invItemMatrix = itemMatrix;
				invItemMatrix.Invert();
				// make sure we are working with a copy
				if (transformedMesh == originalMesh)
				{
					// Make sure the mesh we are going to copy is in a good state to be copied (so we maintain vertex count)
					originalMesh.CleanAndMergeMesh(CancellationToken.None);
					transformedMesh = Mesh.Copy(originalMesh, CancellationToken.None);
					items.Transformed.Mesh = transformedMesh;
				}

				if (AngleDegrees > 0)
				{
					// find the radius that will make the x-size sweep out the requested angle
					// c = Tr ; r = c/T
					var angleRadians = MathHelper.DegreesToRadians(AngleDegrees);
					var circumference = aabb.XSize * MathHelper.Tau / angleRadians;
					var radius = circumference / MathHelper.Tau;

					var rotateXyPos = new Vector2(aabb.minXYZ.X, BendCW ? aabb.maxXYZ.Y : aabb.minXYZ.Y);
					if (!BendCW)
					{
						angleRadians = -angleRadians;
					}

					for (int i = 0; i < originalMesh.Vertices.Count; i++)
					{
						var pos = originalMesh.Vertices[i].Position;
						pos = Vector3.Transform(pos, itemMatrix);

						var pos2D = new Vector2(pos);
						Vector2 rotateSpace = pos2D - rotateXyPos;
						var rotateRatio = rotateSpace.X / aabb.XSize;

						rotateSpace.X = 0;
						rotateSpace.Y += BendCW ? -radius : radius;
						rotateSpace.Rotate(angleRadians * rotateRatio);
						rotateSpace.Y += BendCW ? radius : -radius; ;
						rotateSpace += rotateXyPos;

						var newPos = new Vector3(rotateSpace.X, rotateSpace.Y, pos.Z);
						newPos = Vector3.Transform(newPos, invItemMatrix);

						transformedMesh.Vertices[i].Position = newPos;
					}
				}

				transformedMesh.MarkAsChanged();
				transformedMesh.CalculateNormals();
			}
		}
	}
}
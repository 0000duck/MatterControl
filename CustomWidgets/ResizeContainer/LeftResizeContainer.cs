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

using MatterHackers.Agg;
using MatterHackers.Agg.UI;

namespace MatterHackers.MatterControl.CustomWidgets
{
	public class LeftResizeContainer : FlowLayoutWidget
	{
		private double downWidth = 0;
		private bool mouseDownOnBar = false;
		private double mouseDownX;

		private int _splitterWidth;

		internal LeftResizeContainer()
			: base (FlowDirection.TopToBottom)
		{
			this.HAnchor = HAnchor.Absolute;
			this.Cursor = Cursors.VSplit;
			SplitterWidth = 10;
		}

		public Color SpliterBarColor { get; set; } = ActiveTheme.Instance.TertiaryBackgroundColor;

		public int SplitterWidth
		{
			get => _splitterWidth;
			set
			{
				if (_splitterWidth != value)
				{
					_splitterWidth = value;
					this.Padding = new BorderDouble(_splitterWidth, 0, 0, 0);
					this.MinimumSize = new VectorMath.Vector2(_splitterWidth, 0);
				}
			}
		}

		public override void OnDraw(Graphics2D graphics2D)
		{
			graphics2D.FillRectangle(LocalBounds.Left, LocalBounds.Bottom, LocalBounds.Left + this.SplitterWidth, LocalBounds.Top, this.SpliterBarColor);
			base.OnDraw(graphics2D);
		}

		public override void OnMouseDown(MouseEventArgs mouseEvent)
		{
			if (mouseEvent.Position.X < this.SplitterWidth)
			{
				mouseDownOnBar = true;
				mouseDownX = TransformToScreenSpace(mouseEvent.Position).X;
				downWidth = Width;
			}
			base.OnMouseDown(mouseEvent);
		}

		public override void OnMouseMove(MouseEventArgs mouseEvent)
		{
			if (mouseDownOnBar)
			{
				int currentMouseX = (int)TransformToScreenSpace(mouseEvent.Position).X;
				UiThread.RunOnIdle(() =>
				{
					Width = downWidth + mouseDownX - currentMouseX;
				});
			}
			base.OnMouseMove(mouseEvent);
		}

		public override void OnMouseUp(MouseEventArgs mouseEvent)
		{
			mouseDownOnBar = false;
			base.OnMouseUp(mouseEvent);
		}
	}
}
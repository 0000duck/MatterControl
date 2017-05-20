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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MatterHackers.Agg.Image;
using MatterHackers.Agg.UI;

namespace MatterHackers.MatterControl.Library
{
	public enum ContainerActions
	{
		AddItems,
		AddContainers,
		RenameItems,
		RemoveItems
	}

	public abstract class WritableContainer : LibraryContainer, ILibraryWritableContainer
	{
		public virtual void Add(IEnumerable<ILibraryItem> items)
		{
		}

		public virtual void Remove(IEnumerable<ILibraryItem> items)
		{
		}

		public virtual void Rename(ILibraryItem item, string revisedName)
		{
		}

		public virtual void Move(IEnumerable<ILibraryItem> items, ILibraryContainer targetContainer)
		{
		}

		public virtual void SetThumbnail(ILibraryItem item, int width, int height, ImageBuffer imageBuffer)
		{
		}

		public virtual bool AllowAction(ContainerActions containerActions)
		{
			return true;
		}
	}

	public abstract class LibraryContainer : ILibraryContainer
	{
		public event EventHandler Reloaded;

		public string ID { get; set; }
		public string Name { get; set; }

		public GuiWidget DefaultView { get; protected set; }

		public List<ILibraryContainerLink> ChildContainers { get; set; }
		public bool IsProtected => true;

		public virtual Task<ImageBuffer> GetThumbnail(ILibraryItem item, int width, int height)
		{
			return Task.FromResult<ImageBuffer>(null);
		}

		public List<ILibraryItem> Items { get; set; }

		public ILibraryContainer Parent { get; set; }

		public string StatusMessage { get; set; } = "";

		public virtual string KeywordFilter { get; set; } = "";

		protected void OnReloaded()
		{
			this.Reloaded?.Invoke(this, null);
		}

		public virtual void Dispose()
		{
		}

		public virtual void Activate()
		{
		}

		public virtual void Deactivate()
		{
		}
	}
}

﻿//
// DockingView.cs
//
// Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
// Copyright (c) 2013-2017 Jean-Philippe Bruyère
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;

namespace Crow
{
	public class DockWindow : Window
	{
		#region CTOR
		public DockWindow () : base ()
		{
		}
		#endregion

		int undockThreshold = 4;
		bool isDocked = false;
		Alignment docking = Alignment.Center;

		Point lastMousePos;	//last known mouse pos in this control
		Point undockingMousePosOrig; //mouse pos when docking was donne, use for undocking on mouse move
		Rectangle savedSlot;	//last undocked slot recalled when view is undocked
		bool wasResizable;

		public Docker RootDock { get { return LogicalParent as Docker; }}

		public bool IsDocked {
			get { return isDocked; }
			set {
				if (isDocked == value)
					return;
				isDocked = value;
				NotifyValueChanged ("IsDocked", isDocked);
			}
		}
		public Alignment DockingPosition {
			get { return docking; }
			set {
				if (docking == value)
					return;
				docking = value;
				NotifyValueChanged ("DockingPosition", DockingPosition);
			}
		}

		public override bool PointIsIn (ref Point m)
		{			
			if (!base.PointIsIn(ref m))
				return false;

			Group p = Parent as Group;
			if (p != null) {
				lock (p.Children) {
					for (int i = p.Children.Count - 1; i >= 0; i--) {
						if (p.Children [i] == this)
							break;
						if (p.Children [i].IsDragged)
							continue;
						if (p.Children [i].Slot.ContainsOrIsEqual (m)) {						
							return false;
						}
					}
				}
			}
			return Slot.ContainsOrIsEqual(m);
		}

//		public override void OnLayoutChanges (LayoutingType layoutType)
//		{
//			base.OnLayoutChanges (layoutType);
//
//			if (isDocked)
//				return;
			
//			Docker dv = Parent as Docker;
//			if (dv == null)
//				return;
//
//			Rectangle dvCliRect = dv.ClientRectangle;
//
//			if (layoutType == LayoutingType.X) {
//				if (Slot.X < dv.DockingThreshold)
//					dock (Alignment.Left);
//				else if (Slot.Right > dvCliRect.Width - dv.DockingThreshold)
//					dock (Alignment.Right);
//			}else if (layoutType == LayoutingType.Y) {
//				if (Slot.Y < dv.DockingThreshold)
//					dock (Alignment.Top);
//				else if (Slot.Bottom > dvCliRect.Height - dv.DockingThreshold)
//					dock (Alignment.Bottom);
//			}
//		}
//
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			lastMousePos = e.Position;

			if (this.HasFocus && e.Mouse.IsButtonDown (MouseButton.Left) && IsDocked) {
				if (Math.Abs (e.Position.X - undockingMousePosOrig.X) > 10 ||
				    Math.Abs (e.Position.X - undockingMousePosOrig.X) > 10)
					Undock ();
			}

			base.onMouseMove (sender, e);
		}
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			base.onMouseDown (sender, e);

			if (this.HasFocus && IsDocked && e.Button == MouseButton.Left)
				undockingMousePosOrig = e.Position;
		}
		public bool CheckUndock (Point mousePos) {
			if (Math.Abs (mousePos.X - undockingMousePosOrig.X) < undockThreshold ||
			    Math.Abs (mousePos.X - undockingMousePosOrig.X) < undockThreshold)
				return false;
			Undock ();
			return true;
		}

		protected override void onStartDrag (object sender, DragDropEventArgs e)
		{
			base.onStartDrag (sender, e);

			undockingMousePosOrig = IFace.Mouse.Position;
		}
		protected override void onDrop (object sender, DragDropEventArgs e)
		{
			if (!isDocked && DockingPosition != Alignment.Center)
				dock (e.DropTarget as DockStack);
			base.onDrop (sender, e);
		}
		public void Undock () {
			lock (IFace.UpdateMutex) {
				DockStack ds = Parent as DockStack;
				ds.Undock (this);

				RootDock.AddChild (this);

				this.Left = savedSlot.Left;
				this.Top = savedSlot.Top;
				this.Width = savedSlot.Width;
				this.Height = savedSlot.Height;

				IsDocked = false;
				Resizable = wasResizable;
			}
		}
		void dock (DockStack target){			
			lock (IFace.UpdateMutex) {
				IsDocked = true;
				undockingMousePosOrig = lastMousePos;
				savedSlot = this.LastPaintedSlot;
				wasResizable = Resizable;
				Resizable = false;
				LastSlots = LastPaintedSlot = Slot = default(Rectangle);
				Left = Top = 0;

				RootDock.RemoveChild (this);

				target.Dock (this);
			}
		}
		protected override void close ()
		{
			if (isDocked)
				Undock ();
			base.close ();
		}
	}
}

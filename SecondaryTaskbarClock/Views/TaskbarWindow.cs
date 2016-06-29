﻿using SecondaryTaskbarClock.Native;
using SecondaryTaskbarClock.Renderers;
using SecondaryTaskbarClock.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SecondaryTaskbarClock.Views
{
    // ------------
    // TODO:
    // Hook WM_WINDOWPOSCHANGED of the taskbar and
    // reposition the window if necessary
    // Hook WM_EXITSIZEMOVE to monitor size changes
    // ------------

    /// <summary>
    /// A window which gets added to a taskbar as a component
    /// </summary>
    public class TaskbarWindow : Form
    {
        protected TaskbarRef Taskbar { get; private set; }
        protected IWindowContentRenderer ContentRenderer { get; private set; }

        Size TargetSize = new Size(80, 40);
        Size ActualSize = new Size();

        bool isMouseOver = false;        
        bool isMouseDown = false;

        /// <summary>
        /// Constructor for the Visual Studio Designer
        /// </summary>
        public TaskbarWindow()
        {
            // --
        }

        /// <summary>
        /// Creates a new taskbar window and adds it to the given taskbar
        /// </summary>
        /// <param name="targetTaskbar">The taskbar to add this window to</param>
        public TaskbarWindow(TaskbarRef targetTaskbar, IWindowContentRenderer contentRenderer)
        {
            if (targetTaskbar == null)
                throw new ArgumentNullException("targetTaskbar");
            if (contentRenderer == null)
                throw new ArgumentNullException("contentRenderer");

            Taskbar = targetTaskbar;
            ContentRenderer = contentRenderer;

            FormBorderStyle = FormBorderStyle.None;

            // fix flickering
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);            
            
            // since the window is a child of the taskbar, its
            //background becomes transparent automatically             
            BackColor = Color.Black;

            // when the taskbar position or size changes
            // update this window's position/size accordingly
            Taskbar.PositionOrSizeChanged += (s, e) => AttachToTaskbar();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);            
            ContentRenderer?.Render(e.Graphics, new RendererParameters(Width, Height, isMouseOver, isMouseDown));
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // we do not want to interact with the taskbar at designtime
            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
                AttachToTaskbar();
        }

        /// <summary>
        /// Attach this window to the connected taskbar or update the positioning/sizing
        /// after the taskbar size/position has changed
        /// </summary>
        void AttachToTaskbar()
        {
            var taskbarRect = WindowUtils.GetWindowBounds(Taskbar.Handle);

            // Set this window as child of the taskbar's button bar            
            IntPtr btnsHwnd;

            if (Taskbar.IsPrimary)
                btnsHwnd = TaskbarUtils.GetPrimaryTaskButtonsHwnd(Taskbar.Handle);
            else
                btnsHwnd = TaskbarUtils.GetSecondaryTaskButtonsHwnd(Taskbar.Handle);

            NativeImports.SetWindowLong(Handle, NativeImports.GWL_STYLE, NativeImports.GetWindowLong(Handle, NativeImports.GWL_STYLE) | NativeImports.WS_CHILD);
            NativeImports.SetParent(Handle, btnsHwnd);

            // get the size of the button bar to place the clock
            var taskBtnRect = WindowUtils.GetWindowBounds(btnsHwnd);

            switch (Taskbar.DockPosition)
            {
                case TaskbarDockPosition.Top:
                case TaskbarDockPosition.Bottom:
                    ActualSize = new Size(TargetSize.Width, taskbarRect.Height);

                    // place the clock at the far right                       
                    // we use SetWindowPos since setting Left and Top does not seem to work correctly
                    NativeImports.SetWindowPos(Handle, IntPtr.Zero, taskBtnRect.Width - TargetSize.Width, 0, ActualSize.Width, ActualSize.Height, 0);
                    break;

                case TaskbarDockPosition.Left:
                case TaskbarDockPosition.Right:
                    ActualSize = new Size(taskbarRect.Width, TargetSize.Height);

                    // place the clock at the bottom                                        
                    // we use SetWindowPos since setting Left and Top does not seem to work correctly
                    NativeImports.SetWindowPos(Handle, IntPtr.Zero, 0, taskBtnRect.Height - TargetSize.Height, ActualSize.Width, ActualSize.Height, 0);
                    break;
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);

            isMouseOver = true;
            this.Refresh();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            isMouseOver = false;
            this.Refresh();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Left)
            {
                isMouseDown = true;
                this.Refresh();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.Button == MouseButtons.Left)
            {
                isMouseDown = false;
                this.Refresh();
            }
        }
    }
}

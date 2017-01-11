/*
Copyright © 2017 Legistek Corporation
Portions Copyright © 2015 Rafael Rivera

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

using WPFWin10.Interop;

namespace WPFWin10
{
    /// <summary>
    /// Contians various attached properties that allow UIElement's to
    /// utilize Windows 10 visual features.
    /// </summary>
    public partial class Win10Property : DependencyObject
    {
        #region bool BlurWindow dependency property

        /// <summary>
        /// Controls whether the window or popup associated with a particular
        /// <see cref="FrameworkElement"/> will have the blur effect. See remarks.
        /// </summary>
        /// <remarks>
        /// <para>
        /// For any <see cref="Window"/> or <see cref="Popup"/> to have the blur 
        /// effect, it must also have <see cref="Window.AllowsTransparency"/> or
        /// <see cref="Popup.AllowsTransparency"/> set to <c>true</c>. Furthermore,
        /// the window must have a transparent or semi-transparent background.
        /// </para>
        /// <para>
        /// If this property is set on <b>any</b> child element of the Window or 
        /// popup, the entire Window or popup that hosts the child element 
        /// will have the effect enabled. This is because the property is set
        /// based on the internal window handle, and <see cref="UIElement"/>'s 
        /// in WPF generally do not have unique window handles. 
        /// </para>
        /// <para>
        /// When a blurred background is desired with a <see cref="Popup"/>, 
        /// do not set this property on the <see cref="Popup"/> directly; doing
        /// so will effect the <see cref="Popup"/>'s parent element rather than
        /// the <see cref="Popup"/> itself. Instead, set this property for
        /// any child of the <see cref="Popup"/>, such as the <see cref="Grid"/>
        /// or other root child element of the <see cref="Popup"/>.
        /// </para>
        /// </remarks>
        public static readonly DependencyProperty BlurWindowProperty = 
            DependencyProperty.RegisterAttached(
                "BlurWindow", 
                typeof(bool), 
                typeof(Win10Property),
                new PropertyMetadata(false, OnBlurWindowChanged));   
           
         
        /// <summary>
        /// Gets the value of the <see cref="BlurWindowProperty"/> for the
        /// element.
        /// </summary>
        /// <param name="d">The target element. This must derive from 
        /// <see cref="FrameworkElement"/>.
        /// </param>
        /// <returns><c>true</c> if the property is set, otherwise false.</returns>
        public static bool GetBlurWindow(DependencyObject d)
        {
            return (bool)d.GetValue(BlurWindowProperty);
        }        

        /// <summary>
        /// Sets the value of the <see cref="BlurWindowProperty"/> for the
        /// element.
        /// </summary>
        /// <param name="d">The target element. This must derive from
        /// <see cref="FrameworkElement"/>. </param>
        /// <param name="value"><c>true</c> if the property should be set,
        /// otherwise false.</param>
        public static void SetBlurWindow(
            DependencyObject d, 
            bool value)
        {
            d.SetValue(BlurWindowProperty, value);
        }
        private static void OnBlurWindowChanged(
            DependencyObject obj,
            DependencyPropertyChangedEventArgs args)
        {            
            if (!WinAPI.IsWindows10)
                return;

            FrameworkElement fe = obj as FrameworkElement;
            if (fe == null)
                return;

            bool blur = (args.NewValue as bool?).GetValueOrDefault();
            if (fe.IsLoaded)
                SetBlur(fe, blur);
            else
                fe.Loaded += (sender, e) => SetBlur(fe, blur);
        }

        #endregion

        private static void SetBlur (FrameworkElement fe, bool blur)
        {
            HwndSource source = (HwndSource)PresentationSource
                .FromVisual(fe);            
            if (source == null)
                return; // the window might have been closed already

            var accent = new AccentPolicy();
            var accentStructSize = Marshal.SizeOf(accent);
            accent.AccentState = blur
                ? AccentState.ACCENT_ENABLE_BLURBEHIND
                : AccentState.ACCENT_DISABLED;           

            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData();
            data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
            data.SizeOfData = accentStructSize;
            data.Data = accentPtr;

            WinAPI.SetWindowCompositionAttribute(                
                source.Handle,
                ref data);

            System.Diagnostics.Debug.WriteLine(
                $"Blur enabled on window {source.Handle}");

            Marshal.FreeHGlobal(accentPtr);
        }
    }
}
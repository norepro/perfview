// MIT License
//
// Copyright (c) 2021 REghZy
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

// TODO_AVALONIA: ResourceDictionary code-behind is not supported in Avalonia the same way.
// Consider moving window chrome logic to a behavior or attached property.
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace REghZyFramework.Themes
{
    // TODO_AVALONIA: This class was a WPF ResourceDictionary code-behind.
    // In Avalonia, ResourceDictionary does not support code-behind.
    // These window chrome event handlers should be moved to a behavior or attached property.
    public partial class LightTheme : ResourceDictionary
    {
        private void CloseWindow_Event(object sender, RoutedEventArgs e)
        {
            if (e.Source != null)
                try
                {
                    CloseWind(/* TODO_AVALONIA: Window.GetWindow not available. Use visual tree traversal: */
                    ((Control)e.Source).FindAncestorOfType<Window>());
                }
                catch
                {
                }
        }

        private void AutoMinimize_Event(object sender, RoutedEventArgs e)
        {
            if (e.Source != null)
                try
                {
                    MaximizeRestore(/* TODO_AVALONIA: Window.GetWindow not available. Use visual tree traversal: */
                    ((Control)e.Source).FindAncestorOfType<Window>());
                }
                catch
                {
                }
        }

        private void Minimize_Event(object sender, RoutedEventArgs e)
        {
            if (e.Source != null)
                try
                {
                    MinimizeWind(/* TODO_AVALONIA: Window.GetWindow not available. Use visual tree traversal: */
                    ((Control)e.Source).FindAncestorOfType<Window>());
                }
                catch
                {
                }
        }

        public void CloseWind(Window window) => window.Close();

        public void MaximizeRestore(Window window)
        {
            if (window.WindowState == WindowState.Maximized)
                window.WindowState = WindowState.Normal;
            else if (window.WindowState == WindowState.Normal)
                window.WindowState = WindowState.Maximized;
        }

        public void MinimizeWind(Window window) => window.WindowState = WindowState.Minimized;
    }
}
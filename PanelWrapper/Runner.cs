﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace R8
{
    public class PanelWrapper
    {
        public Grid Container { private get; set; }
        public UIElement Backdrop { private get; set; }
        public bool UseBackdrop { private get; set; }
        public int ShowingPanels { get; set; }

        public double ParentHeight { get; set; }

        public PanelWrapper()
        {
        }

        private static Border AddFrame(int index, double mainWindowHeight, Brush backgroundColor)
        {
            if (index <= 0) throw new ArgumentOutOfRangeException(nameof(index));
            if (mainWindowHeight <= 0) throw new ArgumentOutOfRangeException(nameof(mainWindowHeight));

            return new Border
            {
                CornerRadius = new CornerRadius(8, 8, 0, 0),
                Margin = new Thickness(0, mainWindowHeight, 0, 0),
                Opacity = 0,
                Name = $"PageViewContainer{index}",
                Background = backgroundColor,
                Child = new Frame
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    NavigationUIVisibility = NavigationUIVisibility.Hidden,
                    Name = $"PageViewer{index}",
                    Content = null,
                }
            };
        }

        public class PanelSettings
        {
            public Brush CustomBackground { get; set; }
        }

        public void OpenPanel<T>(Func<T, double> height, PanelSettings panelSettings) where T : Page
        {
            OpenPanelCore(height, panelSettings);
        }

        public void OpenPanel<T>(Func<T, double> height) where T : Page
        {
            OpenPanelCore(height, null);
        }

        private void OpenPanelCore<T>(Func<T, double> height, PanelSettings panelSettings) where T : Page
        {
            if (Container == null)
                throw new NotImplementedException($"'{nameof(Container)}' must be implemented");

            if (UseBackdrop && Backdrop == null)
                throw new NotImplementedException($"'{nameof(Backdrop)}' must be implemented when '{nameof(UseBackdrop)}' is true");

            if (height == null) throw new ArgumentNullException(nameof(height));
            if (ParentHeight <= 0) throw new ArgumentOutOfRangeException(nameof(ParentHeight));
            if (!(typeof(T) != typeof(Page))) throw new Exception("T is not an instance of Page()");

            var frontPage = (T)Activator.CreateInstance(typeof(T));

            var border = AddFrame(Container.Children.Count + 1, ParentHeight, frontPage.Background);

            Container.Children.Add(border);
            ((Frame)border.Child).Navigate(frontPage);

            if (UseBackdrop)
                Backdrop.Visibility = Visibility.Visible;

            if (Container.Children.Count <= 0) return;

            RelocatePanels(true, ParentHeight - height.Invoke(frontPage));

            if (UseBackdrop)
                AnimateBackdrop(true);
        }

        private void RelocatePanels(bool forShow, double? frontPanelPosition = null)
        {
            if (Container == null)
                throw new NotImplementedException($"'{nameof(Container)}' must be implemented");

            if (UseBackdrop && Backdrop == null)
                throw new NotImplementedException($"'{nameof(Backdrop)}' must be implemented when '{nameof(UseBackdrop)}' is true");

            if (forShow && frontPanelPosition == null)
                throw new ArgumentNullException($"{nameof(ParentHeight)} cannot be null when forShow is enabled");

            foreach (var panel in Container.Children.OfType<Border>())
            {
                var count = Container.Children.Count;
                var number = Container.Children.IndexOf(panel) + 1;
                var index = count - number;

                if (!(panel.Child is Frame frame))
                    throw new Exception("Why frame is null ?");

                double position;
                if (!forShow)
                {
                    frame.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    position = ParentHeight - (frame.DesiredSize.Height + 16);
                }
                else
                {
                    position = (double)frontPanelPosition;
                }

                Thickness margin;
                var opacity = (double)(100 - (index * 19)) / 100;
                switch (forShow)
                {
                    case false when index == 0:
                        margin = new Thickness
                        {
                            Left = 0,
                            Right = 0,
                            Bottom = 0,
                            Top = ParentHeight
                        };
                        break;

                    case false when index > 0:
                        var newIndex = index - 1;
                        opacity = (double)(100 - (newIndex * 19)) / 100;
                        margin = new Thickness
                        {
                            Left = newIndex * 5,
                            Right = newIndex * 5,
                            Bottom = 0,
                            Top = position - (newIndex * 10)
                        };
                        break;

                    default:
                        margin = new Thickness
                        {
                            Left = index * 5,
                            Right = index * 5,
                            Bottom = 0,
                            Top = position - (index * 10)
                        };
                        break;
                }

                int animationDelayMilliseconds;
                if (count == 1)
                    animationDelayMilliseconds = 0;
                else if (forShow)
                    animationDelayMilliseconds = number != count ? 0 : 400;
                else
                    animationDelayMilliseconds = number == count ? 0 : 400;

                var duration = TimeSpan.FromMilliseconds(150);
                var opacityAnimation = new DoubleAnimation
                {
                    From = panel.Opacity,
                    To = opacity,
                    Duration = duration,
                    BeginTime = TimeSpan.FromMilliseconds(animationDelayMilliseconds)
                };
                panel.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);

                var thicknessAnimation = new ThicknessAnimation
                {
                    From = panel.Margin,
                    To = margin,
                    Duration = duration,
                    BeginTime = TimeSpan.FromMilliseconds(animationDelayMilliseconds),
                };
                panel.BeginAnimation(FrameworkElement.MarginProperty, thicknessAnimation);
            }

            ShowingPanels = Container.Children.Count;
            if (forShow) return;

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            timer.Start();
            timer.Tick += (sender, args) =>
            {
                timer.Stop();
                Container.Children.Remove(Container.Children[Container.Children.Count - 1]);
                ShowingPanels = Container.Children.Count;
            };
        }

        private void AnimateBackdrop(bool show)
        {
            if (Container == null)
                throw new NotImplementedException($"'{nameof(Container)}' must be implemented");

            if (UseBackdrop && Backdrop == null)
                throw new NotImplementedException($"'{nameof(Backdrop)}' must be implemented when '{nameof(UseBackdrop)}' is true");

            var opacity = show ? 0.6 : 0;

            if (show || Backdrop.Visibility == Visibility.Hidden) Backdrop.Visibility = Visibility.Visible;

            var backdropAnimation = new DoubleAnimation(Backdrop.Opacity, opacity, TimeSpan.FromMilliseconds(200));

            if (!show)
                backdropAnimation.Completed += (sender, args) => Backdrop.Visibility = Visibility.Hidden;

            Backdrop.BeginAnimation(UIElement.OpacityProperty, backdropAnimation);
        }

        public void CloseLastPanel()
        {
            if (Container == null)
                throw new NotImplementedException($"'{nameof(Container)}' must be implemented");

            if (UseBackdrop && Backdrop == null)
                throw new NotImplementedException($"'{nameof(Backdrop)}' must be implemented when '{nameof(UseBackdrop)}' is true");

            if (ParentHeight <= 0) throw new ArgumentOutOfRangeException(nameof(ParentHeight));

            var panelCount = Container.Children.Count;
            if (panelCount == 0) return;

            RelocatePanels(false);

            if (UseBackdrop)
            {
                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
                timer.Start();
                timer.Tick += (sender, args) =>
                {
                    timer.Stop();
                    if (Container.Children.Count == 0)
                        AnimateBackdrop(false);
                };
            }

            GC.Collect();
        }
    }
}
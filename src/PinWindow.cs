using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace ScreenshotPin
{
    internal sealed class PinWindow : Window
    {
        private enum ToolMode
        {
            Move,
            Pen,
            Eraser
        }

        private readonly BitmapSource _sourceImage;
        private readonly Action<string> _ignoreClipboardHash;
        private readonly Grid _surface;
        private readonly InkCanvas _inkCanvas;
        private readonly ToggleButton _moveButton;
        private readonly ToggleButton _penButton;
        private readonly ToggleButton _eraserButton;
        private readonly ToggleButton _topmostButton;
        private readonly Stack<StrokeCollection> _undoStack = new Stack<StrokeCollection>();

        private ToolMode _toolMode;
        private bool _undoSnapshotCaptured;
        private double _aspectRatio;

        public PinWindow(BitmapSource image, bool startTopmost, Action<string> ignoreClipboardHash)
        {
            if (image == null)
            {
                throw new ArgumentNullException("image");
            }

            _sourceImage = image;
            _ignoreClipboardHash = ignoreClipboardHash ?? delegate { };

            Title = "Screenshot Pin";
            Topmost = startTopmost;
            ShowInTaskbar = false;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Background = Brushes.White;
            MinWidth = 180;
            MinHeight = 120;
            Focusable = true;

            double surfaceWidth = Math.Max(1, BitmapUtilities.PixelWidthToDips(image));
            double surfaceHeight = Math.Max(1, BitmapUtilities.PixelHeightToDips(image));
            _aspectRatio = surfaceWidth / surfaceHeight;

            _surface = CreateSurface(image, surfaceWidth, surfaceHeight);
            _inkCanvas = CreateInkCanvas(surfaceWidth, surfaceHeight);
            _surface.Children.Add(_inkCanvas);

            var root = new Grid();
            root.Children.Add(new Viewbox
            {
                Child = _surface,
                Stretch = Stretch.Fill
            });

            Border toolbar = CreateToolbar(out _moveButton, out _penButton, out _eraserButton, out _topmostButton);
            root.Children.Add(toolbar);
            root.Children.Add(CreateResizeThumb());

            Content = root;
            SizeChanged += delegate { toolbar.MaxWidth = Math.Max(140, ActualWidth - 16); };

            SetInitialBounds(surfaceWidth, surfaceHeight);
            SetTool(ToolMode.Move);
            SetPinnedTopmost(startTopmost);

            MouseWheel += OnMouseWheel;
            KeyDown += OnKeyDown;
            StateChanged += OnStateChanged;
            Loaded += delegate { Focus(); };
        }

        private static Grid CreateSurface(BitmapSource image, double surfaceWidth, double surfaceHeight)
        {
            var surface = new Grid
            {
                Width = surfaceWidth,
                Height = surfaceHeight,
                ClipToBounds = true,
                SnapsToDevicePixels = true
            };

            surface.Children.Add(new Image
            {
                Source = image,
                Width = surfaceWidth,
                Height = surfaceHeight,
                Stretch = Stretch.Fill,
                SnapsToDevicePixels = true
            });

            return surface;
        }

        private InkCanvas CreateInkCanvas(double surfaceWidth, double surfaceHeight)
        {
            var canvas = new InkCanvas
            {
                Width = surfaceWidth,
                Height = surfaceHeight,
                Background = Brushes.Transparent,
                EditingMode = InkCanvasEditingMode.None,
                UseCustomCursor = true,
                Cursor = Cursors.SizeAll,
                DefaultDrawingAttributes = new DrawingAttributes
                {
                    Color = Colors.Red,
                    Width = 3,
                    Height = 3,
                    FitToCurve = true,
                    IsHighlighter = false,
                    StylusTip = StylusTip.Ellipse
                }
            };

            canvas.PreviewMouseLeftButtonDown += OnInkCanvasMouseLeftButtonDown;
            canvas.PreviewMouseLeftButtonUp += delegate { _undoSnapshotCaptured = false; };
            return canvas;
        }

        public void SetPinnedTopmost(bool isTopmost)
        {
            Topmost = isTopmost;
            if (_topmostButton != null)
            {
                _topmostButton.IsChecked = isTopmost;
            }
        }

        public void RestoreFromMinimized()
        {
            Show();
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }

            ShowInTaskbar = false;
            Activate();
        }

        private Border CreateToolbar(out ToggleButton moveButton, out ToggleButton penButton, out ToggleButton eraserButton, out ToggleButton topmostButton)
        {
            var panel = new WrapPanel
            {
                Orientation = Orientation.Horizontal
            };

            moveButton = CreateToolButton("Move", "移动窗口", delegate { SetTool(ToolMode.Move); });
            penButton = CreateToolButton("Pen", "画笔标注", delegate { SetTool(ToolMode.Pen); });
            eraserButton = CreateToolButton("Erase", "橡皮擦", delegate { SetTool(ToolMode.Eraser); });
            ToggleButton createdTopmostButton = null;
            createdTopmostButton = CreateToolButton("Top", "置于最上层", delegate
            {
                SetPinnedTopmost(createdTopmostButton.IsChecked == true);
            });
            topmostButton = createdTopmostButton;

            panel.Children.Add(moveButton);
            panel.Children.Add(penButton);
            panel.Children.Add(eraserButton);
            panel.Children.Add(topmostButton);
            panel.Children.Add(CreateButton("Undo", "撤销上一步", delegate { Undo(); }));
            panel.Children.Add(CreateButton("Clear", "清空标注", delegate { ClearStrokes(); }));
            panel.Children.Add(CreateButton("Copy", "复制合成后的图片", delegate { CopyMergedImage(); }));
            panel.Children.Add(CreateButton("Save", "保存为 PNG", delegate { SaveMergedImage(); }));
            panel.Children.Add(CreateButton("Min", "最小化", delegate { MinimizeWindow(); }));
            panel.Children.Add(CreateButton("X", "关闭", delegate { Close(); }));

            return new Border
            {
                Child = panel,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(8),
                Padding = new Thickness(5),
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(Color.FromArgb(224, 32, 32, 32))
            };
        }

        private ToggleButton CreateToolButton(string text, string tooltip, RoutedEventHandler click)
        {
            var button = new ToggleButton
            {
                Content = text,
                ToolTip = tooltip,
                Width = 50,
                Height = 28,
                Margin = new Thickness(2, 0, 2, 0),
                Focusable = false
            };
            button.Click += click;
            return button;
        }

        private Button CreateButton(string text, string tooltip, RoutedEventHandler click)
        {
            var button = new Button
            {
                Content = text,
                ToolTip = tooltip,
                MinWidth = 42,
                Height = 28,
                Margin = new Thickness(2, 0, 2, 0),
                Focusable = false
            };
            button.Click += click;
            return button;
        }

        private Thumb CreateResizeThumb()
        {
            var thumb = new Thumb
            {
                Width = 18,
                Height = 18,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Cursor = Cursors.SizeNWSE,
                Opacity = 0.85
            };

            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(190, 32, 32, 32)));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));
            borderFactory.SetValue(Border.BorderBrushProperty, Brushes.White);
            borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            thumb.Template = new ControlTemplate(typeof(Thumb)) { VisualTree = borderFactory };

            thumb.DragDelta += OnResizeDragDelta;
            return thumb;
        }

        private void SetTool(ToolMode mode)
        {
            _toolMode = mode;
            _moveButton.IsChecked = mode == ToolMode.Move;
            _penButton.IsChecked = mode == ToolMode.Pen;
            _eraserButton.IsChecked = mode == ToolMode.Eraser;

            if (mode == ToolMode.Move)
            {
                _inkCanvas.EditingMode = InkCanvasEditingMode.None;
                _inkCanvas.Cursor = Cursors.SizeAll;
            }
            else if (mode == ToolMode.Pen)
            {
                _inkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                _inkCanvas.Cursor = Cursors.Pen;
            }
            else
            {
                _inkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
                _inkCanvas.EraserShape = new EllipseStylusShape(18, 18);
                _inkCanvas.Cursor = Cursors.Cross;
            }
        }

        private void OnInkCanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Focus();

            if (_toolMode == ToolMode.Move)
            {
                if (e.ClickCount >= 2)
                {
                    ResetToNaturalSize();
                    e.Handled = true;
                    return;
                }

                try
                {
                    DragMove();
                }
                catch (InvalidOperationException)
                {
                    // DragMove can throw if Windows has already ended the mouse capture.
                }

                e.Handled = true;
                return;
            }

            CaptureUndoSnapshotOnce();
        }

        private void OnResizeDragDelta(object sender, DragDeltaEventArgs e)
        {
            double widthCandidate = Width + e.HorizontalChange;
            double heightCandidate = Height + e.VerticalChange;
            double widthFromHeight = heightCandidate * _aspectRatio;
            double newWidth = Math.Abs(e.HorizontalChange) >= Math.Abs(e.VerticalChange)
                ? widthCandidate
                : widthFromHeight;

            ResizeKeepingAspect(newWidth);
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            double factor = e.Delta > 0 ? 1.08 : 0.92;
            ResizeKeepingAspect(Width * factor);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.C)
            {
                CopyMergedImage();
                e.Handled = true;
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.S)
            {
                SaveMergedImage();
                e.Handled = true;
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.Z)
            {
                Undo();
                e.Handled = true;
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.T)
            {
                SetPinnedTopmost(!Topmost);
                e.Handled = true;
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.M)
            {
                MinimizeWindow();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        }

        private void MinimizeWindow()
        {
            ShowInTaskbar = true;
            WindowState = WindowState.Minimized;
        }

        private void OnStateChanged(object sender, EventArgs e)
        {
            if (WindowState != WindowState.Minimized)
            {
                ShowInTaskbar = false;
            }
        }

        private void CaptureUndoSnapshotOnce()
        {
            if (_undoSnapshotCaptured)
            {
                return;
            }

            _undoStack.Push(CloneStrokes(_inkCanvas.Strokes));
            _undoSnapshotCaptured = true;
        }

        private void Undo()
        {
            if (_undoStack.Count == 0)
            {
                return;
            }

            _inkCanvas.Strokes = _undoStack.Pop();
        }

        private void ClearStrokes()
        {
            if (_inkCanvas.Strokes.Count == 0)
            {
                return;
            }

            _undoStack.Push(CloneStrokes(_inkCanvas.Strokes));
            _inkCanvas.Strokes.Clear();
        }

        private static StrokeCollection CloneStrokes(StrokeCollection strokes)
        {
            var clone = new StrokeCollection();
            foreach (Stroke stroke in strokes)
            {
                clone.Add(stroke.Clone());
            }

            return clone;
        }

        private void CopyMergedImage()
        {
            BitmapSource bitmap = RenderMergedImage();
            string hash = ImageHasher.Compute(bitmap);
            _ignoreClipboardHash(hash);
            Clipboard.SetImage(bitmap);
        }

        private void SaveMergedImage()
        {
            BitmapSource bitmap = RenderMergedImage();
            var dialog = new SaveFileDialog
            {
                Title = "保存锚定截图",
                Filter = "PNG 图片 (*.png)|*.png",
                FileName = "screenshot-pin-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".png",
                AddExtension = true,
                DefaultExt = ".png"
            };

            bool? result = dialog.ShowDialog(this);
            if (result != true)
            {
                return;
            }

            using (var file = new FileStream(dialog.FileName, FileMode.Create, FileAccess.Write))
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(file);
            }
        }

        private BitmapSource RenderMergedImage()
        {
            var renderSize = new Size(_surface.Width, _surface.Height);
            _surface.Measure(renderSize);
            _surface.Arrange(new Rect(renderSize));
            _surface.UpdateLayout();

            var bitmap = new RenderTargetBitmap(
                _sourceImage.PixelWidth,
                _sourceImage.PixelHeight,
                BitmapUtilities.SafeDpiX(_sourceImage),
                BitmapUtilities.SafeDpiY(_sourceImage),
                PixelFormats.Pbgra32);

            bitmap.Render(_surface);
            if (bitmap.CanFreeze)
            {
                bitmap.Freeze();
            }

            return bitmap;
        }

        private void SetInitialBounds(double naturalWidth, double naturalHeight)
        {
            Rect workArea = ScreenUtilities.GetCursorWorkAreaDips();
            double maxWidth = Math.Max(320, workArea.Width * 0.68);
            double maxHeight = Math.Max(240, workArea.Height * 0.68);
            double scale = Math.Min(1.0, Math.Min(maxWidth / naturalWidth, maxHeight / naturalHeight));

            Width = Math.Max(MinWidth, naturalWidth * scale);
            Height = Math.Max(MinHeight, naturalHeight * scale);

            Left = workArea.Left + Math.Max(0, (workArea.Width - Width) / 2);
            Top = workArea.Top + Math.Max(0, (workArea.Height - Height) / 2);
        }

        private void ResetToNaturalSize()
        {
            Rect workArea = ScreenUtilities.GetCursorWorkAreaDips();
            double naturalWidth = _surface.Width;
            double newWidth = Math.Min(naturalWidth, workArea.Width * 0.9);
            ResizeKeepingAspect(newWidth);
        }

        private void ResizeKeepingAspect(double requestedWidth)
        {
            double workAreaMax = Math.Max(300, ScreenUtilities.GetCursorWorkAreaDips().Width * 1.5);
            double newWidth = Math.Max(MinWidth, Math.Min(workAreaMax, requestedWidth));
            double newHeight = newWidth / _aspectRatio;

            if (newHeight < MinHeight)
            {
                newHeight = MinHeight;
                newWidth = newHeight * _aspectRatio;
            }

            Width = newWidth;
            Height = newHeight;
        }
    }
}

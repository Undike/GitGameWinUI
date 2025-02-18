using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using WinRT.Interop;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.UI.Core;
using System.Globalization;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Shapes;
using Windows.System;

namespace GitGameWinUi
{
    public sealed partial class MainWindow : Window
    {
        private const int CellSize = 50; // ������ ����� ������ �����
        private UIElement selectedElement;
        private Line verticalLine;
        private Line horizontalLine;
        private bool isRightMouseButtonPressed = false;
        private Point lastPointerPosition;

        public MainWindow()
        {
            this.InitializeComponent();

            // �������� ���������� ����
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            // ����������� ���� � ������������� �����
            var presenter = appWindow.Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                presenter.SetBorderAndTitleBar(false, false);
                presenter.Maximize();
            }

            appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);

            this.Content.KeyDown += MainWindow_KeyDown;

            SceneScrollViewer.PointerWheelChanged += SceneScrollViewer_PointerWheelChanged;
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            CenterScrollViewerOnCanvas();
        }

        private void GridCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            CreateGrid();
            GenerateNewObject(); // �������� ����� �������� ������ �������
        }

        #region ��������� ������� ������
        private void MainWindow_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Escape)
            {
                ShowMenu();
            }
        }
        #endregion

        #region ����
        private async void ShowMenu()
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "Menu",
                PrimaryButtonText = "Resume",
                SecondaryButtonText = "Exit",
                XamlRoot = this.Content.XamlRoot
            };
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // ���������� ��������� "Resumed" � ��������� �������� ���������
                ResumedMessageContainer.Opacity = 1;

                // �������� Storyboard �� �������� ��������� Grid
                var rootGrid = (Grid)this.Content;
                if (rootGrid.Resources["FadeOutStoryboard"] is Storyboard fadeOutStoryboard)
                {
                    fadeOutStoryboard.Completed += (s, e) =>
                    {
                        ResumedMessageContainer.Opacity = 0;
                    };
                    fadeOutStoryboard.Begin();
                }
            }
            else if (result == ContentDialogResult.Secondary)
            {
                // ��������� ����������
                Application.Current.Exit();
            }
        }



        #endregion

        #region ��������� ���������� ����
        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                var textBlock = new TextBlock
                {
                    Text = textBox.Text,
                    TextWrapping = TextWrapping.Wrap,
                    Width = textBox.Width,
                    FontSize = textBox.FontSize
                };
                textBlock.Measure(new Size(textBox.Width, Double.PositiveInfinity));

                double initialHeight = 90; // ��������� ������
                double additionalHeight = 10; // �������������� ������������ ��� ������ ����������
                double desiredHeight = textBlock.DesiredSize.Height;

                // ������������ ����� ������
                double newHeight = Math.Max(initialHeight, desiredHeight) + additionalHeight;

                // ������������ ������������ ������
                textBox.Height = Math.Min(newHeight, 600);
            }
        }

        private void SendFromInputToLog(object sender, RoutedEventArgs e)
        {
            // ���������, ���� �� ��� 20 ���������, � ������� ������, ���� ��� ���
            if (OutputStackPanel.Children.Count >= 20)
            {
                OutputStackPanel.Children.RemoveAt(0);
            }

            // ������� ��������� ��� ������ ����� ������
            var container = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 10, 0, 0)
            };

            // ������� ����� ContentPresenter ��� ������ ������
            var text = InputTextBox.Text;

            var newContentPresenter = new ContentPresenter
            {
                Content = new TextBlock
                {
                    Text = text,
                    TextWrapping = TextWrapping.Wrap,
                    Width = 400,
                    FontSize = 16,
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black)
                }
            };

            // ������� ����� ������ ContentPresenter
            var border = new Border
            {
                Child = newContentPresenter,
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Black),
                BorderThickness = new Thickness(1), // ������� �����
                CornerRadius = new CornerRadius(10), // ������������ ����
                Margin = new Thickness(0, 5, 0, 0),
                Padding = new Thickness(5), // ���������� ������
                Background = new SolidColorBrush(Microsoft.UI.Colors.DarkGray) // ��� DarkGray
            };

            container.Children.Add(border);

            // ������� ��� ������� ������ ContentPresenter �� ������ ������
            double CalculateContentPresenterHeight(ContentPresenter contentPresenter)
            {
                if (contentPresenter.Content is TextBlock textBlock)
                {
                    textBlock.Measure(new Size(textBlock.Width, Double.PositiveInfinity));
                    return textBlock.DesiredSize.Height + 10; // ��������� 10 �������� ��� ������� �����������
                }
                return 50; // ����������� ������, ���� Content �� �������� TextBlock
            }

            // ������������� ������ ContentPresenter ����� ��� ��������
            newContentPresenter.Loaded += (s, e) =>
            {
                newContentPresenter.Height = CalculateContentPresenterHeight(newContentPresenter);
            };

            // ������� ��������� ��� ������
            var buttonContainer = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(5, 0, 0, 0)
            };

            // ������ ��� ����������� ������
            var copyButton = new Button
            {
                Content = new FontIcon { Glyph = "\uE8C8" }, // ��� ����� ������ (������ �����������)
                Width = 75,
                Height = 30,
                Margin = new Thickness(0, 5, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Background = new SolidColorBrush(Microsoft.UI.Colors.DarkGray),
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Black)
            };
            copyButton.Click += (s, args) =>
            {
                if (newContentPresenter.Content is TextBlock textBlock)
                {
                    InputTextBox.Text = textBlock.Text;
                }
            };
            buttonContainer.Children.Add(copyButton);

            // ������ ��� �������� �����
            var deleteButton = new Button
            {
                Content = new FontIcon { Glyph = "\uE74D" }, // ������ ��������� �����
                Width = 75,
                Height = 30,
                Margin = new Thickness(0, 5, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Background = new SolidColorBrush(Microsoft.UI.Colors.DarkGray),
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Black)
            };
            deleteButton.Click += (s, args) =>
            {
                OutputStackPanel.Children.Remove(container);
            };
            buttonContainer.Children.Add(deleteButton);

            // ��������� ��������� ��� ������ � ��������� ����� ������
            container.Children.Add(buttonContainer);

            // ��������� ��������� � StackPanel ������ ScrollViewer
            OutputStackPanel.Children.Add(container);

            // ������������ ScrollViewer � ���������� ������������ ��������
            OutputStackPanel.UpdateLayout();
            var lastChild = OutputStackPanel.Children.Last() as FrameworkElement;
            lastChild?.StartBringIntoView();

            // ������� ���������� ����
            InputTextBox.Text = string.Empty;
        }

        private void CopyFromLogToInput(object sender, RoutedEventArgs e)
        {
            // ����������� ������ �� OutputTextBoxLog � InputTextBox
        }

        private void DeleteLastEntry(object sender, RoutedEventArgs e)
        {
            // �������� ���������� ������������ ��������
            if (OutputStackPanel.Children.Count > 0)
            {
                var lastChild = OutputStackPanel.Children.Last();
                OutputStackPanel.Children.Remove(lastChild);
            }
        }
        #endregion


        #region CameraMovement 
        private void CenterScrollViewerOnCanvas()
        {
            if (SceneScrollViewer != null && SceneCanvas != null)
            {
                double horizontalOffset = (SceneCanvas.Width - SceneScrollViewer.ViewportWidth) / 2;
                double verticalOffset = (SceneCanvas.Height - SceneScrollViewer.ViewportHeight) / 2;
                SceneScrollViewer.ChangeView(horizontalOffset, verticalOffset, null);
            }
        }
        private void SceneScrollViewer_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            // ���������� ��������� ������������ ��������������� (��������, 0.2 ��� ����� ��������� �������)
            float zoomFactorChange = e.GetCurrentPoint(SceneScrollViewer).Properties.MouseWheelDelta > 0 ? 0.2f : -0.2f;
            float newZoomFactor = SceneScrollViewer.ZoomFactor + zoomFactorChange;

            // ������������ ����������� ��������������� � �������� MinZoomFactor � MaxZoomFactor
            if (newZoomFactor < SceneScrollViewer.MinZoomFactor)
            {
                newZoomFactor = SceneScrollViewer.MinZoomFactor;
            }
            else if (newZoomFactor > SceneScrollViewer.MaxZoomFactor)
            {
                newZoomFactor = SceneScrollViewer.MaxZoomFactor;
            }

            // ������������� ����� �������� ���������������
            SceneScrollViewer.ChangeView(null, null, newZoomFactor);
        }




        private void SceneScrollViewer_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(SceneScrollViewer).Properties.IsRightButtonPressed)
            {
                isRightMouseButtonPressed = true;
                lastPointerPosition = e.GetCurrentPoint(SceneScrollViewer).Position;
                SceneScrollViewer.CapturePointer(e.Pointer); // ����������� ��������� ��� ��������� ������� PointerMoved � PointerReleased
            }
        }

        private void SceneScrollViewer_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (isRightMouseButtonPressed)
            {
                var currentPointerPosition = e.GetCurrentPoint(SceneScrollViewer).Position;

                // ��������� �������� �������
                double deltaX = currentPointerPosition.X - lastPointerPosition.X;
                double deltaY = currentPointerPosition.Y - lastPointerPosition.Y;

                // ��������������� ���������
                SceneScrollViewer.ChangeView(SceneScrollViewer.HorizontalOffset - deltaX, SceneScrollViewer.VerticalOffset - deltaY, null);

                lastPointerPosition = currentPointerPosition;
            }
        }

        private void SceneScrollViewer_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (isRightMouseButtonPressed && !e.GetCurrentPoint(SceneScrollViewer).Properties.IsRightButtonPressed)
            {
                isRightMouseButtonPressed = false;
                SceneScrollViewer.ReleasePointerCapture(e.Pointer); // ��������� ����������� ���������
            }
        }

        private void SceneScrollViewer_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (isRightMouseButtonPressed && e.Key == VirtualKey.RightButton)
            {
                isRightMouseButtonPressed = false;
            }
        }

        #endregion

        #region GridAndObjectMovement 
        private void CreateGrid()
        {
            if (GridCanvas == null || GridCanvas.Width == 0 || GridCanvas.Height == 0)
                return;

            double centerX = GridCanvas.Width / 2;
            double centerY = GridCanvas.Height / 2;

            // ��������� ���������� ����� �� ����������� � ���������
            int columns = (int)(GridCanvas.Width / CellSize);
            int rows = (int)(GridCanvas.Height / CellSize);

            for (int x = -columns / 2; x <= columns / 2; x++)
            {
                for (int y = -rows / 2; y <= rows / 2; y++)
                {
                    var rect = new Rectangle
                    {
                        Width = CellSize,
                        Height = CellSize,
                        Stroke = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                        StrokeThickness = 1
                    };
                    double left = centerX + x * CellSize - (CellSize / 2);
                    double top = centerY + y * CellSize - (CellSize / 2);
                    Canvas.SetLeft(rect, left);
                    Canvas.SetTop(rect, top);
                    GridCanvas.Children.Add(rect);

                    // ��������� ��������� ����� ��� ��������� �����
                    var textBlock = new TextBlock
                    {
                        Text = $"({x}, {y})",
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    Canvas.SetLeft(textBlock, left + CellSize / 2 - textBlock.ActualWidth / 2);
                    Canvas.SetTop(textBlock, top + CellSize / 2 - textBlock.ActualHeight / 2);

                    GridCanvas.Children.Add(textBlock);
                }
            }
        }

        private void GenerateNewObject()
        {
            int column = 0;
            int row = 0;

            var emptyCell = FindFreeCell(ref column, ref row);

            double centerX = SceneCanvas.Width / 2;
            double centerY = SceneCanvas.Height / 2;

            double left = centerX + column * CellSize - (CellSize / 2);
            double top = centerY + row * CellSize - (CellSize / 2);

            var randomColorSquare = CreateRandomColorSquare();

            Canvas.SetLeft(randomColorSquare, left);
            Canvas.SetTop(randomColorSquare, top);

            SceneCanvas.Children.Add(randomColorSquare);

            DrawConnectionLine(left + CellSize / 2, top + CellSize / 2, centerX, centerY);
        }

        private Rectangle CreateRandomColorSquare()
        {
            var random = new Random();
            var color = Windows.UI.Color.FromArgb(
                255,
                (byte)random.Next(256),
                (byte)random.Next(256),
                (byte)random.Next(256)
            );

            var randomColorSquare = new Rectangle
            {
                Width = CellSize,
                Height = CellSize,
                Fill = new SolidColorBrush(color)
            };

            return randomColorSquare;
        }

        private Point FindFreeCell(ref int column, ref int row)
        {
            int maxRadius = (int)Math.Max(SceneCanvas.Width, SceneCanvas.Height) / CellSize;
            int x = 0, y = 0;
            int dx = 0, dy = -1;

            for (int i = 0; i < maxRadius * maxRadius; i++)
            {
                if (!IsOccupied(column + x, row + y))
                {
                    column += x;
                    row += y;
                    return new Point(x, y);
                }

                // ������� ��� ��������� ����������� �������� �� �������
                if ((x == y) || (x < 0 && x == -y) || (x > 0 && x == 1 - y))
                {
                    var temp = dx;
                    dx = -dy;
                    dy = temp;
                }

                x += dx;
                y += dy;
            }

            return new Point(column, row); // ���������� ��������� ����������, ���� ��������� ������ �� �������
        }

        private bool IsOccupied(int column, int row)
        {
            double centerX = SceneCanvas.Width / 2;
            double centerY = SceneCanvas.Height / 2;
            double left = centerX + column * CellSize - (CellSize / 2);
            double top = centerY + row * CellSize - (CellSize / 2);
            foreach (var child in SceneCanvas.Children)
            {
                if (child is Rectangle rectangle && Canvas.GetLeft(rectangle) == left && Canvas.GetTop(rectangle) == top)
                {
                    return true;
                }
            }
            return false;
        }

        private UIElement GetElementAtPosition(Point position)
        {
            foreach (var child in SceneCanvas.Children)
            {
                if (child is Rectangle rectangle)
                {
                    double left = Canvas.GetLeft(rectangle);
                    double top = Canvas.GetTop(rectangle);
                    if (position.X >= left && position.X <= left + CellSize && position.Y >= top && position.Y <= top + CellSize)
                    {
                        return rectangle;
                    }
                }
            }
            return null;
        }

        private void DrawConnectionLine(double startX, double startY, double endX, double endY)
        {
            if (verticalLine != null)
            {
                SceneCanvas.Children.Remove(verticalLine);
                verticalLine = null;
            }

            if (horizontalLine != null)
            {
                SceneCanvas.Children.Remove(horizontalLine);
                horizontalLine = null;
            }

            verticalLine = new Line
            {
                Stroke = new SolidColorBrush(Microsoft.UI.Colors.Red),
                StrokeThickness = 2,
                X1 = startX,
                Y1 = startY,
                X2 = startX,
                Y2 = endY
            };

            horizontalLine = new Line
            {
                Stroke = new SolidColorBrush(Microsoft.UI.Colors.Red),
                StrokeThickness = 2,
                X1 = startX,
                Y1 = endY,
                X2 = endX,
                Y2 = endY
            };

            SceneCanvas.Children.Add(verticalLine);
            SceneCanvas.Children.Add(horizontalLine);
        }

        private void SceneCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var pointerPoint = e.GetCurrentPoint(SceneCanvas);

            if (pointerPoint.Properties.IsLeftButtonPressed)
            {
                if (selectedElement == null)
                {
                    selectedElement = GetElementAtPosition(pointerPoint.Position);
                }
                else
                {
                    double centerX = SceneCanvas.Width / 2;
                    double centerY = SceneCanvas.Height / 2;
                    int column = (int)Math.Round((pointerPoint.Position.X - centerX) / CellSize);
                    int row = (int)Math.Round((pointerPoint.Position.Y - centerY) / CellSize);
                    double left = centerX + column * CellSize - (CellSize / 2);
                    double top = centerY + row * CellSize - (CellSize / 2);

                    if (!IsOccupied(column, row))
                    {
                        Canvas.SetLeft(selectedElement, left);
                        Canvas.SetTop(selectedElement, top);

                        if (verticalLine != null)
                        {
                            SceneCanvas.Children.Remove(verticalLine);
                            verticalLine = null;
                        }

                        if (horizontalLine != null)
                        {
                            SceneCanvas.Children.Remove(horizontalLine);
                            horizontalLine = null;
                        }

                        DrawConnectionLine(left + CellSize / 2, top + CellSize / 2, centerX, centerY);

                        selectedElement = null;
                    }
                }
            }
        }
        #endregion
    }

    public static class PointExtensions
    {
        public static Point Subtract(this Point point1, Point point2)
        {
            return new Point(point1.X - point2.X, point1.Y - point2.Y);
        }
    }
}

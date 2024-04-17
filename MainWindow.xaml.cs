using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Linq;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;

namespace CGproj2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        private Point startPoint;
        private Line drawingLine;
        private bool isDrawingLine = false;
        private bool isDrawingCircle = false;
        private bool isDeleting = false;
        private Ellipse drawingCircle;
        private bool isMovingCircle;
        private List<Line> lines = new List<Line>();
        private List<Ellipse> circles = new List<Ellipse>();
        private bool colorEdit = false;


        private List<Shape> shapes = new List<Shape>();

        //polygon addition 
        private List<Point> polygonVertices = new List<Point>();
        private List<Polygon> polygons = new List<Polygon>();
        private Polygon drawingPolygon;
        private bool isDrawingPolygon = false;
        private Polygon selectedPolygon;
        private Point selectedVertex;
        private bool isMovingVertex = false;

        //anti-alia..
        private bool antiAliasingEnabled = false;
        ContextMenu gradientContextMenu = new ContextMenu();
        private bool isMovingLineEndPoint;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(canvas);

            
                if(isDrawingCircle || isDrawingLine)
                {
                    if(isDrawingLine) StartDrawingLine();
                    if(isDrawingCircle) StartDrawingCircle();
                } 
  
                else if(isDrawingCircle == false || isDrawingLine == false)
                {
                    if (isDrawingCircle == false)
                    {
                        circles.Remove(drawingCircle);
                        drawingCircle = null;
                    }
                    if (isDrawingLine == false)
                    {
                        lines.Remove(drawingLine);
                        drawingLine = null;
                    }
                }

            if (isDrawingPolygon)
            {
                polygonVertices.Add(startPoint);

                if (polygonVertices.Count > 1)
                {
                    drawingPolygon.Points = new PointCollection(polygonVertices);
                }
            }
            else if (selectedPolygon != null)
            {
                Point clickPoint = e.GetPosition(canvas);
                MoveVertex(selectedPolygon, clickPoint);
            }




        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDrawingLine = false;
            isDrawingCircle = false;
        }


       

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (drawingLine != null && !colorEdit)
            {
                UpdateLineEndPoint(e.GetPosition(canvas));
            }
            else if (drawingCircle != null && !colorEdit)
            {
                UpdateCircleRadius(e.GetPosition(canvas));
            }
            if (isDrawingPolygon)
            {
                if (polygonVertices.Count > 0)
                {
                    Point currentMousePosition = e.GetPosition(canvas);
                    drawingPolygon.Points = new PointCollection(polygonVertices.Concat(new[] { currentMousePosition }));
                }
            }

        }

        private void Canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(canvas);
            HitTestResult result = VisualTreeHelper.HitTest(canvas, e.GetPosition(canvas));

            if (isDrawingPolygon)
            {
                StopDrawingPolygon();
            }
            else if (result != null && result.VisualHit is Shape)
            {
                Shape clickedShape = result.VisualHit as Shape;
                ContextMenu contextMenu = new ContextMenu();

                MenuItem deleteItem = new MenuItem();
                deleteItem.Header = "Delete";
                deleteItem.Click += DeleteMenuItem_Click;
                contextMenu.Items.Add(deleteItem);

                MenuItem editItem = new MenuItem();
                editItem.Header = "Edit";
                editItem.Click += EditMenuItem_Click;
                contextMenu.Items.Add(editItem);

                MenuItem changeColorItem = new MenuItem();
                changeColorItem.Header = "Change Color";
                changeColorItem.Click += ChangeColorMenuItem_Click;
                contextMenu.Items.Add(changeColorItem);

                MenuItem changeGradientItem = new MenuItem();
                changeGradientItem.Header = "Change Gradient";
                changeGradientItem.Click += ChangeGradientMenuItem_Click;
                contextMenu.Items.Add(changeGradientItem);

                MenuItem changeThicknessItem = new MenuItem();
                changeThicknessItem.Header = "Change Thickness";
                changeThicknessItem.Click += ChangeThicknessMenuItem_Click;
                contextMenu.Items.Add(changeThicknessItem);

                contextMenu.IsOpen = true;
            }
        }




        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"; 
            saveFileDialog.Title = "Save Shapes";

            if (saveFileDialog.ShowDialog() == true)
            {
                string jsonLines = JsonConvert.SerializeObject(lines);
                string jsonCircles = JsonConvert.SerializeObject(circles);
                string jsonPolygons = JsonConvert.SerializeObject(polygons);

                string combinedJson = $"{{\"Lines\":{jsonLines},\"Circles\":{jsonCircles},\"Polygons\":{jsonPolygons}}}";

                File.WriteAllText(saveFileDialog.FileName, combinedJson);
            }
        }

        //private void Load_Click(object sender, RoutedEventArgs e)
        //{
        //    OpenFileDialog openFileDialog = new OpenFileDialog();
        //    openFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
        //    openFileDialog.Title = "Load Shapes";

        //    if (openFileDialog.ShowDialog() == true)
        //    {
        //        try
        //        {
        //            string json = File.ReadAllText(openFileDialog.FileName);

        //            JObject jsonObject = JObject.Parse(json);

        //            // Deserialize lines
        //            lines = jsonObject["Lines"].ToObject<List<Line>>();
        //            foreach (var line in lines)
        //            {
        //                canvas.Children.Add(line);
        //            }

        //            // Deserialize circles
        //            circles = jsonObject["Circles"].ToObject<List<Ellipse>>();
        //            foreach (var circle in circles)
        //            {
        //                canvas.Children.Add(circle);
        //            }

        //            // Deserialize polygons
        //            polygons = jsonObject["Polygons"].ToObject<List<Polygon>>();
        //            foreach (var polygon in polygons)
        //            {
        //                canvas.Children.Add(polygon);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            MessageBox.Show($"Error loading shapes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //        }
        //    }
        //}


        private void Load_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
            openFileDialog.Title = "Load Shapes";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {

                    string json = File.ReadAllText(openFileDialog.FileName);

                    JObject jsonObject = JObject.Parse(json);
                    lines = JsonConvert.DeserializeObject<List<Line>>(jsonObject["Lines"].ToString());
                    circles = JsonConvert.DeserializeObject<List<Ellipse>>(jsonObject["Circles"].ToString());
                    polygons = JsonConvert.DeserializeObject<List<Polygon>>(jsonObject["Polygons"].ToString());

                    foreach (var line in lines)
                    {
                        canvas.Children.Add(line);
                    }
                    foreach (var circle in circles)
                    {
                        canvas.Children.Add(circle);
                    }
                    foreach (var polygon in polygons)
                    {
                        canvas.Children.Add(polygon);
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading shapes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        private void StartDrawingLine()
        {
            drawingLine = new Line
            {
                X1 = startPoint.X,
                Y1 = startPoint.Y,
                X2 = startPoint.X,
                Y2 = startPoint.Y
            };
            if (antiAliasingEnabled == false)
            {
                SymmetricLine((int)drawingLine.X1, (int)drawingLine.Y1, (int)drawingLine.X2, (int)drawingLine.Y2);
                isDrawingLine = false;

            }else if(antiAliasingEnabled == true){

                WuLine((int)drawingLine.X1, (int)drawingLine.Y1, (int)drawingLine.X2, (int)drawingLine.Y2);
                isDrawingLine = false;
            }
        }
      

        private void UpdateLineEndPoint(Point endPoint)
        {
            if (drawingLine != null)
            {
                drawingLine.X2 = endPoint.X;
                drawingLine.Y2 = endPoint.Y;
            }
        }

        private void StartDrawingCircle()
        {
            drawingCircle = new Ellipse
            {
                Width = 0,
                Height = 0,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };

            if (antiAliasingEnabled == false)
            {
                MidpointCircle(drawingCircle);
                isDrawingCircle = false;
            }
            else
            {
                WuCircle((int)drawingCircle.Width/2);
            }
        }

        private void UpdateCircleRadius(Point endPoint)
        {
            if (drawingCircle != null)
            {
                double radius = Math.Sqrt(Math.Pow(endPoint.X - startPoint.X, 2) + Math.Pow(endPoint.Y - startPoint.Y, 2));
                drawingCircle.Width = 2 * radius;
                drawingCircle.Height = 2 * radius;
                Canvas.SetLeft(drawingCircle, startPoint.X - radius);
                Canvas.SetTop(drawingCircle, startPoint.Y - radius);
            }
        }

        private void DrawLine_Click(object sender, RoutedEventArgs e)
        {
            isDrawingLine = true;
            isDrawingCircle = false;
        }

        private void DrawCircle_Click(object sender, RoutedEventArgs e)
        {
            isDrawingLine = false;
            isDrawingCircle = true;
        }

        private void ClearScreen_Click(object sender, RoutedEventArgs e)
        {
            canvas.Children.Clear();
            lines.Clear();
            circles.Clear();
        }

        

        private void ToggleAntiAliasing_Click(object sender, RoutedEventArgs e)
        {
            antiAliasingEnabled = !antiAliasingEnabled; 
        }


        //POLYGON STUFF 
        private void DrawPolygon_Click(object sender, RoutedEventArgs e)
        {
            isDrawingPolygon = true;
            drawingPolygon = new Polygon
            {
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Fill = Brushes.Transparent
            };
            canvas.Children.Add(drawingPolygon);
        }

        private void StopDrawingPolygon()
        {
            isDrawingPolygon = false;
            polygons.Remove(drawingPolygon);
            drawingPolygon = null;
        }

        private void MoveVertex(Polygon polygon, Point newPosition)
        {
            int closestVertexIndex = FindClosestVertexIndex(polygon, newPosition);
            polygon.Points[closestVertexIndex] = newPosition;
        }

        private int FindClosestVertexIndex(Polygon polygon, Point point)
        {
            double minDistance = double.MaxValue;
            int closestVertexIndex = -1;

            for (int i = 0; i < polygon.Points.Count; i++)
            {
                double distance = DistanceBetweenPoints(polygon.Points[i], point);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestVertexIndex = i;
                }
            }

            return closestVertexIndex;
        }

        private double DistanceBetweenPoints(Point p1, Point p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        //MENU ITEM STUFF

        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {

            HitTestResult result = VisualTreeHelper.HitTest(canvas, startPoint);
            if (result != null && result.VisualHit is Line)
            {
                Line lineToDelete = result.VisualHit as Line;
                canvas.Children.Remove(lineToDelete);
                lines.Remove(lineToDelete);
            }
            else if (result != null && result.VisualHit is Ellipse)
            {
                Ellipse circleToDelete = result.VisualHit as Ellipse;
                canvas.Children.Remove(circleToDelete);
                circles.Remove(circleToDelete);
            }
            else if (result != null && result.VisualHit is Polygon)
            {
                Polygon polyToDelete = result.VisualHit as Polygon;
                canvas.Children.Remove(polyToDelete);
                polygons.Remove(polyToDelete);
            }
        }


        private void EditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            selectedPolygon = null;
            selectedVertex = default(Point);

            isMovingVertex = false;


            HitTestResult result = VisualTreeHelper.HitTest(canvas, startPoint);
            if (result != null && result.VisualHit is Line)
            {
               
                drawingLine = result.VisualHit as Line;
                UpdateLineEndPoint(startPoint);
                isMovingLineEndPoint = false;
            }
            else if (result != null && result.VisualHit is Ellipse && selectedPolygon == null)
            {
                Ellipse circle = result.VisualHit as Ellipse;
                Point center = new Point(Canvas.GetLeft(circle) + circle.Width / 2, Canvas.GetTop(circle) + circle.Height / 2);
                double radius = circle.Width / 2;
                double distance = Math.Sqrt(Math.Pow(startPoint.X - center.X, 2) + Math.Pow(startPoint.Y - center.Y, 2));
                if (distance >= radius - 5 && distance <= radius + 5)
                {
                    drawingCircle = circle;
                    isMovingCircle = true;
                }
            }
            else if (result != null && result.VisualHit is Polygon)
            {
                selectedPolygon = result.VisualHit as Polygon;

            }
           
        }
       

        private void ChangeColorMenuItem_Click(object sender, RoutedEventArgs e)
        {
            HitTestResult result = VisualTreeHelper.HitTest(canvas, startPoint);
            colorEdit = true;
            if (result != null && result.VisualHit is Line)
            {
                Line line = result.VisualHit as Line;
                ContextMenu colorContextMenu = new ContextMenu();

                Color[] predefinedColors = { Colors.Red, Colors.Blue, Colors.Green, Colors.Yellow, Colors.Purple };

                foreach (Color color in predefinedColors)
                {
                    MenuItem colorItem = new MenuItem();
                    colorItem.Header = new Border
                    {
                        Background = new SolidColorBrush(color),
                        Width = 20,
                        Height = 20,
                        Margin = new Thickness(5) 
                    };
                    colorItem.Click += (menuItemSender, menuItemEvent) =>
                    {
                        line.Stroke = new SolidColorBrush(color);
                    };
                    colorContextMenu.Items.Add(colorItem);
                }

                colorContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                colorContextMenu.IsOpen = true;
            }
            else if (result != null && result.VisualHit is Ellipse)
            {
                Ellipse circle = result.VisualHit as Ellipse;

                ContextMenu colorContextMenu = new ContextMenu();

                Color[] predefinedColors = { Colors.Red, Colors.Blue, Colors.Green, Colors.Yellow, Colors.Purple };

                foreach (Color color in predefinedColors)
                {
                    MenuItem colorItem = new MenuItem();
                    colorItem.Header = new Border
                    {
                        Background = new SolidColorBrush(color),
                        Width = 20,
                        Height = 20,
                        Margin = new Thickness(5) 
                    };
                    colorItem.Click += (menuItemSender, menuItemEvent) =>
                    {
                        circle.Stroke = new SolidColorBrush(color);
                    };
                    colorContextMenu.Items.Add(colorItem);
                }

                colorContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                colorContextMenu.IsOpen = true;
            }
            else if (result != null && result.VisualHit is Polygon)
            {
                Polygon poly = result.VisualHit as Polygon;

                ContextMenu colorContextMenu = new ContextMenu();

                Color[] predefinedColors = { Colors.Red, Colors.Blue, Colors.Green, Colors.Yellow, Colors.Purple };

                foreach (Color color in predefinedColors)
                {
                    MenuItem colorItem = new MenuItem();
                    colorItem.Header = new Border
                    {
                        Background = new SolidColorBrush(color),
                        Width = 20,
                        Height = 20,
                        Margin = new Thickness(5) 
                    };
                    colorItem.Click += (menuItemSender, menuItemEvent) =>
                    {
                        poly.Stroke = new SolidColorBrush(color);
                    };
                    colorContextMenu.Items.Add(colorItem);
                }

                colorContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                colorContextMenu.IsOpen = true;
            }
            colorEdit = false;
        }

        private void ChangeThicknessMenuItem_Click(object sender, RoutedEventArgs e)
        {
            HitTestResult result = VisualTreeHelper.HitTest(canvas, startPoint);
            if (result != null && result.VisualHit is Line)
            {
                Line line = result.VisualHit as Line;

                ContextMenu thicknessContextMenu = new ContextMenu();

                double[] thicknessOptions = { 3, 4, 5, 6, 7 };

                foreach (double thickness in thicknessOptions)
                {
                    MenuItem thicknessItem = new MenuItem();
                    thicknessItem.Header = thickness;
                    thicknessItem.Click += (menuItemSender, menuItemEvent) =>
                    {
                        line.StrokeThickness = thickness;
                    };
                    thicknessContextMenu.Items.Add(thicknessItem);
                }

                thicknessContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                thicknessContextMenu.IsOpen = true;
            }
            if (result != null && result.VisualHit is Polygon)
            {
                Polygon poly = result.VisualHit as Polygon;

                ContextMenu thicknessContextMenu = new ContextMenu();

                double[] thicknessOptions = { 3, 4, 5, 6, 7 };

                foreach (double thickness in thicknessOptions)
                {
                    MenuItem thicknessItem = new MenuItem();
                    thicknessItem.Header = thickness;
                    thicknessItem.Click += (menuItemSender, menuItemEvent) =>
                    {
                        poly.StrokeThickness = thickness;
                    };
                    thicknessContextMenu.Items.Add(thicknessItem);
                }

                thicknessContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                thicknessContextMenu.IsOpen = true;
            }
        }

        //SYMMETRIC MIDPOINT LINE

        private void SymmetricLine(int x1, int y1, int x2, int y2)
        {
            int dx = x2 - x1;
            int dy = y2 - y1;
            int d = 2 * dy - dx;
            int dE = 2 * dy;
            int dNE = 2 * (dy - dx);
            int xf = x1, yf = y1;
            int xb = x2, yb = y2;
            putPixel(xf, yf); 
            while (xf < xb)
            {
                ++xf; --xb;
                if (d < 0)
                    d += dE;
                else
                {
                    d += dNE;
                    ++yf;
                    --yb;
                }
                putPixel(xf, yf);
            }
        }

        private void putPixel(int x, int y)
        {
            drawingLine = new Line
            {
                X1 = x,
                Y1 = y,

                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            canvas.Children.Add(drawingLine);
        }

        //MIDPOINT CIRCLE ALGORITHM

        private void putPixelCircle(int x, int y)
        {
            if (x >= 0 && x < canvas.ActualWidth && y >= 0 && y < canvas.ActualHeight)
            {
                Canvas.SetLeft(drawingCircle, x - drawingCircle.Width / 2);
                Canvas.SetTop(drawingCircle, y - drawingCircle.Height / 2);
                canvas.Children.Add(drawingCircle);
            }

        }


        private void MidpointCircle(Ellipse circle)
        {
            int R = (int)(circle.ActualWidth / 2);
            int d = 1 - R;
            int x = 0;
            int y = R;
            putPixelCircle(x, y);
            while (y > x)
            {
                if (d < 0) 
                    d += 2 * x + 3;
                else 
                {
                    d += 2 * x - 2 * y + 5;
                    --y;
                }
                ++x;
                putPixelCircle(x, y);
            }
        }


        //Xiaolin Wu


        private void WuLine(int x1, int y1, int x2, int y2)
        {
            Color L = Colors.Black; 
            Color B = Colors.White; 

            int dx = x2 - x1;
            int dy = y2 - y1;

            float m = Math.Abs(dx) > Math.Abs(dy) ? (float)dy / dx : (float)dx / dy;

            for (int x = x1; x <= x2; ++x)
            {
                float y = y1 + m * (x - x1);

                float fraction = 1 - (y - (int)y);

                Color c1 = L * fraction + B * (1 - fraction);
                Color c2 = L * (1 - fraction) + B * fraction;

                XputPixel(x, (int)y, c1);
                XputPixel(x, (int)y + 1, c2);

     
            }
        }




        private void WuCircle(int R)
        {
            Color L = Colors.Black;
            Color B = Colors.White; 

            int x = R;
            int y = 0;
            XputPixel(x, y, L); 

            while (x > y)
            {
                ++y;
                x = (int)Math.Ceiling(Math.Sqrt(R * R - y * y)); 

                float T = D(R, y);
                Color c1 = L * T + B * (1 - T);
                Color c2 = L * (1 - T) + B * T;

                cputPixel(x, y, c2);
                cputPixel(x - 1,y, c1);
            }
        }

     
        private void XputPixel(int x, int y, Color color)
        {
            SolidColorBrush brush = new SolidColorBrush();
            brush.Color = color;
            drawingLine = new Line
            {
                X1 = x,
                Y1 = y,
                Stroke = brush,
                StrokeThickness = 2
            };
            canvas.Children.Add(drawingLine);

        }

        private void cputPixel(int x, int y, Color color)
        {
            SolidColorBrush brush = new SolidColorBrush(color);

            Ellipse pixel = new Ellipse
            {
                Width = 1,
                Height = 1,
                Stroke = brush
            };

            // Calculate the position of the pixel within the canvas
            Canvas.SetLeft(pixel, x);
            Canvas.SetTop(pixel, y);

            // Add the pixel to the canvas
            canvas.Children.Add(pixel);

        }

        private float D(int R, int y)
        {
            double P = Math.Ceiling(Math.Sqrt(R * R - y * y)); 
            double P2 = Math.Sqrt(R * R - (y + 0.5) * (y + 0.5));
            return (float)(P2 - P);
        }


        //lab part -> GRADIENT LINES IMPLEMENTED 


        private void ChangeGradientMenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Media.Color startColor = Colors.Blue;
            System.Windows.Media.Color endColor = Colors.Green;

            HitTestResult result = VisualTreeHelper.HitTest(canvas, startPoint);
            colorEdit = true;
            if (result != null && result.VisualHit is Line)
            {
                Line line = result.VisualHit as Line;


                System.Windows.Media.Color[] predefinedColors = { Colors.Red, Colors.Blue, Colors.Pink, Colors.Green, Colors.Yellow, Colors.Purple };

                foreach (System.Windows.Media.Color color in predefinedColors)
                {
                    CheckBox colorCheckBox = new CheckBox();
                    colorCheckBox.Content = new Border
                    {
                        Background = new SolidColorBrush(color),
                        Width = 20,
                        Height = 20,
                        Margin = new Thickness(5)
                    };
                    colorCheckBox.Checked += (checkBoxSender, checkBoxEvent) =>
                    {
                        ApplyGradientToLine(line, startColor, endColor, GetSelectedColors());
                    };
                    colorCheckBox.Unchecked += (checkBoxSender, checkBoxEvent) =>
                    {
                        ApplyGradientToLine(line, startColor, endColor, GetSelectedColors());
                    };
                    gradientContextMenu.Items.Add(colorCheckBox);
                }

                gradientContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
                gradientContextMenu.IsOpen = true;
            }

            colorEdit = true;
        }

        private List<Color> GetSelectedColors()
        {
            List<Color> selectedColors = new List<Color>();

            foreach (var item in gradientContextMenu.Items)
            {
                if (item is CheckBox checkBox && checkBox.IsChecked == true)
                {
                    if (checkBox.Content is Border border && border.Background is SolidColorBrush brush)
                    {
                        selectedColors.Add(brush.Color);
                    }
                }
            }

            return selectedColors;
        }


        private LinearGradientBrush CreateGradientBrush(List<Color> colors)
        {
            LinearGradientBrush gradientBrush = new LinearGradientBrush();
            gradientBrush.StartPoint = new Point(0, 0);
            gradientBrush.EndPoint = new Point(1, 1);
            Random random = new Random();
          

            for (int i = 0; i < colors.Count; i++)
            {
                Color color = colors[i];
                int randomNumber = random.Next(1, 11);
                double offsetStep = 1.0 / randomNumber;

                gradientBrush.GradientStops.Add(new GradientStop(color, i * offsetStep));  
            }

            return gradientBrush;
        }



        private void ApplyGradientToLine(Line line, Color startColor, Color endColor, List<Color> selectedColors)
        {
            List<Color> colors = new List<Color>();
            colors.Add(startColor);
            colors.AddRange(selectedColors);
            colors.Add(endColor);

            LinearGradientBrush gradientBrush = CreateGradientBrush(colors);
            line.Stroke = gradientBrush;
        }




    }
}
    
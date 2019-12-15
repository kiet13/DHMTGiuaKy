using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharpGL;
using SharpGL.WinForms;
using System.Text.RegularExpressions;

namespace FirstSharpGLProject
{

    public partial class Form1 : Form
    {
        static class Constants
        {
            public const float ControlPointSize = 7;
            public const int YES = 1;
            public const int NO = 0;
            public const int COLLINEAR = 2;

        }

      
        /* Các lớp đối tượng hình học */
        public abstract class Shape
        {
            public Point Start { get; set; }
            public Point End { get; set; }
            public Color BoundaryColor { get; set; }
            public Color FillColor { get; set; }
            public float Size { get; set; } // Size of the edge (pixel)
            public List<Point> ControlPoints { get; set; }
            public bool IsClicked { get; set; }
            public Point SeedPoint { get; set; }
            public bool IsPainted { get; set; }

            public Shape()
            {
                IsClicked = false;
                IsPainted = false;
            }

            public virtual void draw(OpenGL gl)
            {
                gl.Color(BoundaryColor.R / 255.0, BoundaryColor.G / 255.0, BoundaryColor.B / 255.0, 0);
            }

            public virtual void DrawControlPoints(OpenGL gl)
            {
                Color ControlPointsColor = Color.DarkBlue;
                gl.Color(ControlPointsColor.R / 255.0, ControlPointsColor.G / 255.0, ControlPointsColor.B / 255.0, 0);
                gl.PointSize(Constants.ControlPointSize);
                gl.Begin(OpenGL.GL_POINTS);
                for (int i = 0; i < ControlPoints.Count; i++)
                {
                    gl.Vertex(ControlPoints[i].X, ControlPoints[i].Y);
                }
                gl.End();
                gl.Flush();
            }

            public virtual void GenerateControlPoints(OpenGL gl)
            {

            }

            public abstract bool IsContain(int X, int Y);


            private void SetPixel(OpenGL gl, byte[] pixelArray, int X, int Y, Color color)
            {
               

                int widthStep = gl.RenderContextProvider.Width * 4;
                pixelArray[X * 4 + Y * widthStep] = color.R;
                pixelArray[X * 4 + Y * widthStep + 1] = color.G;
                pixelArray[X * 4 + Y * widthStep + 2] = color.B;
                pixelArray[X * 4 + Y * widthStep + 3] = color.A;
            }

            private byte[] GetPixel(OpenGL gl, int X, int Y)
            {
                
                byte[] pixel = new byte[4];
                
                gl.ReadPixels(X, Y, 1, 1, OpenGL.GL_RGBA, OpenGL.GL_UNSIGNED_BYTE, pixel);
                return pixel;
            }

            public void BoundaryFill(OpenGL gl, byte[] pixelArray)
            {
                
                byte[] pixel1 = new byte[4];
                byte[] pixel2 = new byte[4];
                Stack<Point> stack = new Stack<Point>();
                
                stack.Push(new Point(SeedPoint.X, SeedPoint.Y));
                gl.Color(FillColor.R, FillColor.G, FillColor.B);
                int widthStep = gl.RenderContextProvider.Width * 4;
                while (stack.Count > 0)
                {
                    Point currentPoint = stack.Pop();
                    // Xét pixel trên openGL context
                    pixel1 = GetPixel(gl, currentPoint.X, currentPoint.Y);
                    int X = currentPoint.X;
                    int Y = currentPoint.Y;
                    // Xét pixel trên mảng
                    for (int i = 0; i < 4; i++)
                        pixel2[i] = pixelArray[X * 4 + Y * widthStep + i];
                    
                    
                    if ((pixel1[0] != BoundaryColor.R || pixel1[1] != BoundaryColor.G || pixel1[2] != BoundaryColor.B || pixel1[3] != BoundaryColor.A) &&
                        (pixel2[0] != FillColor.R || pixel2[1] != FillColor.G || pixel2[2] != FillColor.B || pixel2[3] != FillColor.A))
                    {
                        // Gán giá trị màu lên vị trí tương ứng trong mảng 
                        SetPixel(gl, pixelArray, currentPoint.X, currentPoint.Y, FillColor);

                        stack.Push(new Point(currentPoint.X, currentPoint.Y + 1));
                        stack.Push(new Point(currentPoint.X, currentPoint.Y - 1));
                        stack.Push(new Point(currentPoint.X + 1, currentPoint.Y));
                        stack.Push(new Point(currentPoint.X - 1, currentPoint.Y));
                        //stack.Push(new Point(currentPoint.X - 1, currentPoint.Y - 1));
                        //stack.Push(new Point(currentPoint.X - 1, currentPoint.Y + 1));
                        //stack.Push(new Point(currentPoint.X + 1, currentPoint.Y - 1));
                        //stack.Push(new Point(currentPoint.X + 1, currentPoint.Y + 1));
                    }      
                }
               
                
            }
        }

        public class Line : Shape
        {

            public override void draw(OpenGL gl)
            {
                base.draw(gl);
                gl.LineWidth(Size);
                gl.Begin(OpenGL.GL_LINES);
                gl.Vertex(Start.X, Start.Y);
                gl.Vertex(End.X, End.Y);
                gl.End();
                gl.Flush();// Thực hiện lệnh vẽ ngay lập tức thay vì đợi sau 1 khoảng thời gian
                this.GenerateControlPoints(gl);
            }

            public override void GenerateControlPoints(OpenGL gl)
            {
                Point point1 = new Point(Start.X, Start.Y);
                Point point2 = new Point(End.X, End.Y);

                ControlPoints.Add(point1);
                ControlPoints.Add(point2);
            }

            public override bool IsContain(int X, int Y)
            {
                
                float epsilon = 8.0f;
              
                float d1, d2;
                d1 = (float)Math.Sqrt((X - Start.X) * (X - Start.X) + (Y - Start.Y) * (Y - Start.Y));
                d2 = (float)Math.Sqrt((X - End.X) * (X - End.X) + (Y - End.Y) * (Y - End.Y));
                if (d1 < epsilon || d2 < epsilon)
                    return true;
                else
                {
                    int a, b, c;
                    a = End.Y - Start.Y;
                    b = Start.X - End.X;
                    c = End.X * Start.Y - Start.X * End.Y;

                    float d;
                    d = (float)Math.Abs(a * X + b * Y + c) / (float)Math.Sqrt(a * a + b * b);
                    if (d < epsilon)
                        return true;
                }
                return false;
               
            }
        }

        /*Phần vẽ Ellipse cho bạn nào làm tham khảo*/

        public class Ellipse : Shape
        {
            public int Xc { get; set; }
            public int Yc { get; set; }
            public override void draw(OpenGL gl)
            {
                base.draw(gl);
                gl.PointSize(Size);
                int rx = Math.Abs(Start.X - End.X) / 2;
                int ry = Math.Abs(Start.Y - End.Y) / 2;

                // Coordinate of the center
                int xc = (Start.X + End.X) / 2;
                int yc = (Start.Y + End.Y) / 2;

                Xc = xc;
                Yc = gl.RenderContextProvider.Height - yc;
                List<Point> points = new List<Point>();

                float dx, dy, d1, d2;
                int x, y;
                x = 0;
                y = ry;

                // Initial decision parameter of region 1 
                d1 = (ry * ry) - (rx * rx * ry) +
                                 (float)(0.25 * rx * rx);
                dx = 2 * ry * ry * x;
                dy = 2 * rx * rx * y;

                // For region 1 
                while (dx < dy)
                {

                    // Add points based on 4-way symmetry
                    List<Point> newPoints = new List<Point>
                    {
                        new Point(x + xc, y + yc),
                        new Point(-x + xc, y + yc),
                        new Point(x + xc, -y + yc),
                        new Point(-x + xc, -y + yc)
                    };
                    points.AddRange(newPoints);


                    // Checking and updating value of 
                    // decision parameter based on algorithm 
                    if (d1 < 0)
                    {
                        x++;
                        dx = dx + (2 * ry * ry);
                        d1 = d1 + dx + (ry * ry);
                    }
                    else
                    {
                        x++;
                        y--;
                        dx = dx + (2 * ry * ry);
                        dy = dy - (2 * rx * rx);
                        d1 = d1 + dx - dy + (ry * ry);
                    }
                }

                // Decision parameter of region 2 
                d2 = ((ry * ry) * ((float)(x + 0.5) * (float)(x + 0.5))) +
                     ((rx * rx) * ((y - 1) * (y - 1))) -
                      (rx * rx * ry * ry);

                // Plotting points of region 2 
                while (y >= 0)
                {


                    // Add points based on 4-way symmetry
                    List<Point> newPoints = new List<Point>
                    {
                        new Point(x + xc, y + yc),
                        new Point(-x + xc, y + yc),
                        new Point(x + xc, -y + yc),
                        new Point(-x + xc, -y + yc)
                    };
                    points.AddRange(newPoints);

                    // Checking and updating parameter 
                    // value based on algorithm 
                    if (d2 > 0)
                    {
                        y--;
                        dy = dy - (2 * rx * rx);
                        d2 = d2 + (rx * rx) - dy;
                    }
                    else
                    {
                        y--;
                        x++;
                        dx = dx + (2 * ry * ry);
                        dy = dy - (2 * rx * rx);
                        d2 = d2 + dx - dy + (rx * rx);
                    }
                }


                gl.Begin(OpenGL.GL_POINTS);
                foreach (Point point in points)
                {
                    gl.Vertex(point.X, gl.RenderContextProvider.Height - point.Y);
                }

                gl.End();
                gl.Flush();
                this.GenerateControlPoints(gl);
            }

            public override void GenerateControlPoints(OpenGL gl)
            {
                int stepX = (End.X - Start.X) / 2;
                int stepY = (End.Y - Start.Y) / 2;
                for (int i = -1; i < 2; i++)
                {
                    for (int j = -1; j < 2; j++)
                    {
                        if (i == 0 && j == 0)
                            continue;
                        Point point = new Point(Xc + j * stepX, Yc + i * stepY);
                        ControlPoints.Add(point);
                    }
                }
            }

            public override bool IsContain(int X, int Y)
            {
                int rx = Math.Abs(Start.X - End.X) / 2;
                int ry = Math.Abs(Start.Y - End.Y) / 2;

                float t1 = (float)(X - Xc) / rx;
                float t2 = (float)(Y - Yc) / ry;
                if (t1 * t1 + t2 * t2 > 1)
                    return false;
                return true;
            }
        }

        public class Circle : Shape
        {
            public int Xc {get; set;}
            public int Yc { get; set; }
            public override void draw(OpenGL gl)
            {
                base.draw(gl);
                gl.PointSize(Size);
                int r = Math.Abs(Start.X - End.X) / 2;
                // Coordinate of the center
                
                
                int xc = (Start.X + End.X) / 2;
                int yc = End.Y;

                Xc = xc;
                Yc = gl.RenderContextProvider.Height - yc;

                int P = 1 - r;
                int x = 0;
                int y = r;

                List<Point> points = new List<Point>();
                while (x < y)
                {
                    // Add points based on 8-way symmetry
                    List<Point> newPoints = new List<Point>
                    {
                        new Point(x + xc, y + yc),
                        new Point(-x + xc, y + yc),
                        new Point(x + xc, -y + yc),
                        new Point(-x + xc, -y + yc),
                        new Point(y + xc, x + yc),
                        new Point(-y + xc, x + yc),
                        new Point(y + xc, -x + yc),
                        new Point(-y + xc, -x + yc)
                    };
                    points.AddRange(newPoints);

                    if (P < 0)
                    {
                        x++;
                        P += (2 * x + 1);
                    }
                    else
                    {
                        x++;
                        y--;
                        P += (2 * x - 2 * y + 1);
                    }
                }

                gl.Begin(OpenGL.GL_POINTS);
                foreach (Point point in points)
                {
                    gl.Vertex(point.X, gl.RenderContextProvider.Height - point.Y);
                }
                gl.End();
                gl.Flush();
                this.GenerateControlPoints(gl);
            }

            public override void GenerateControlPoints(OpenGL gl)
            {
                base.GenerateControlPoints(gl);
                int step = (End.X - Start.X) / 2; // step = r

                // center coordinate
                int xc = (Start.X + End.X) / 2;
                int yc = End.Y;

                for (int i = -1; i < 2; i++)
                {
                    for (int j = -1; j < 2; j++)
                    {
                        if (i == 0 && j == 0)
                            continue;
                        Point point = new Point(xc + j * step, gl.RenderContextProvider.Height - yc - i * step);
                        ControlPoints.Add(point);
                    }
                }
            }

            public override bool IsContain(int X, int Y)
            {
               
                double r = Math.Abs(ControlPoints[1].X - ControlPoints[0].X);

                double distance = Math.Sqrt((X - Xc) * (X - Xc) + (Y - Yc) * (Y - Yc));
                if (distance > r)
                    return false;
                return true;
            }
        }

        public class Triangle : Shape
        {
            public int Xc { get; set; }
            public int Yc { get; set; }
            public override void draw(OpenGL gl)
            {
                base.draw(gl);
                gl.LineWidth(Size);
                gl.PolygonMode(OpenGL.GL_FRONT_AND_BACK, OpenGL.GL_LINE);
                Point pointA = new Point
                {
                    X = Math.Abs(End.X + Start.X) / 2,
                    Y = gl.RenderContextProvider.Height - Start.Y
                };
                Point pointB = new Point(End.X, gl.RenderContextProvider.Height - End.Y);
                Point pointC = new Point(Start.X, gl.RenderContextProvider.Height - End.Y);
                Xc = pointA.X;
                Yc = (pointA.Y + pointB.Y) / 2;

                gl.Begin(OpenGL.GL_POLYGON);
                gl.Vertex(pointA.X, pointA.Y);
                gl.Vertex(pointB.X, pointB.Y);
                gl.Vertex(pointC.X, pointC.Y);
                gl.End();
                gl.Flush();

                //gl.PointSize(Size - 1);
                //gl.Begin(OpenGL.GL_POINTS);
                //gl.Vertex(pointB.X, pointB.Y);
                //gl.Vertex(pointC.X, pointC.Y);
                //gl.End();
                //gl.Flush();
                this.GenerateControlPoints(gl);
            }

            public override void GenerateControlPoints(OpenGL gl)
            {
                base.GenerateControlPoints(gl);
                int stepX = (End.X - Start.X) / 2;
                int stepY = (End.Y - Start.Y) / 2;

                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        if (i == 0 && j == 0)
                            continue;
                        Point point = new Point(Xc + j * stepX, Yc + i * stepY);
                        ControlPoints.Add(point);
                    }
                }
            }

            public override bool IsContain(int X, int Y)
            {
                // Peak points
                Point A = new Point();
                Point B = new Point();
                Point C = new Point();

                A = ControlPoints[6];
                B = ControlPoints[0];
                C = ControlPoints[2];     



                // Determine point inside polygon through cross product
                Point vector = new Point();

                Point vectorAB = new Point(B.X - A.X, B.Y - A.Y);
                Point vectorBC = new Point(C.X - B.X, C.Y - B.Y);
                Point vectorCA = new Point(A.X - C.X, A.Y - C.Y);

                vector.X = X - A.X;
                vector.Y = Y - A.Y;

                int coordinateZ = vector.X * vectorAB.Y - vector.Y * vectorAB.X;
                if (coordinateZ > 0)
                    return false;
                vector.X = X - B.X;
                vector.Y = Y - B.Y;
                coordinateZ = vector.X * vectorBC.Y - vector.Y * vectorBC.X;
                if (coordinateZ > 0)
                    return false;
                vector.X = X - C.X;
                vector.Y = Y - C.Y;
                coordinateZ = vector.X * vectorCA.Y - vector.Y * vectorCA.X;
                if (coordinateZ > 0)
                    return false;
                return true;
            }
        }

        public class Rectangle : Shape
        {
            public int Xc { get; set; }
            public int Yc { get; set; }
            public override void draw(OpenGL gl)
            {
                base.draw(gl);
                gl.LineWidth(Size);
                gl.PolygonMode(OpenGL.GL_FRONT_AND_BACK, OpenGL.GL_LINE);
            
                Point pointA = new Point(Start.X, gl.RenderContextProvider.Height - Start.Y);
                Point pointC = new Point(End.X, gl.RenderContextProvider.Height - End.Y);
                Point pointB = new Point(pointC.X, pointA.Y);
                Point pointD = new Point(pointA.X, pointC.Y);

                Xc = (pointA.X + pointC.X) / 2;
                Yc = (pointA.Y + pointD.Y) / 2;

                gl.Begin(OpenGL.GL_POLYGON);
                gl.Vertex(pointA.X, pointA.Y);
                gl.Vertex(pointB.X, pointB.Y);
                gl.Vertex(pointC.X, pointC.Y);
                gl.Vertex(pointD.X, pointD.Y);
                gl.End();
                gl.Flush();

                gl.PointSize(Size);
                gl.Begin(OpenGL.GL_POINTS);
                gl.Vertex(pointA.X, pointA.Y);
                gl.Vertex(pointB.X, pointB.Y);
                gl.Vertex(pointC.X, pointC.Y);
                gl.Vertex(pointD.X, pointD.Y);
                gl.End();
                gl.Flush();
                this.GenerateControlPoints(gl);
            }

            public override void GenerateControlPoints(OpenGL gl)
            {
                int stepX = Math.Abs(End.X - Start.X) / 2;
                int stepY = Math.Abs(End.Y - Start.Y) / 2;
                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        if (i == 0 && j == 0)
                            continue;
                        Point point = new Point(Xc + j * stepX, Yc + i * stepY);
                        ControlPoints.Add(point);
                    }
                }
            }

            public override bool IsContain(int X, int Y)
            {
                

                if ((X >= ControlPoints[0].X) && (X <= ControlPoints[2].X)
                    && (Y >= ControlPoints[0].Y) && (Y <= ControlPoints[6].Y))
                    return true;
                return false;
            }
        }

        public class Polygon : Shape
        {

            // https://stackoverflow.com/questions/217578/how-can-i-determine-whether-a-2d-point-is-within-a-polygon
            private int IsIntersection(int v1x1, int v1y1, int v1x2, int v1y2,
                int v2x1, int v2y1, int v2x2, int v2y2)
            {
                int d1, d2;
                int a1, b1, c1, a2, b2, c2;

                // Convert vector 1 to a line (line 1) of infinite length.
                // We want the line in linear equation standard form: A*x + B*y + C = 0
                // See: http://en.wikipedia.org/wiki/Linear_equation
                a1 = v1y2 - v1y1;
                b1 = v1x1 - v1x2;
                c1 = (v1x2 * v1y1) - (v1x1 * v1y2);

                // Every point (x,y), that solves the equation above, is on the line,
                // every point that does not solve it, is not. The equation will have a
                // positive result if it is on one side of the line and a negative one 
                // if is on the other side of it. We insert (x1,y1) and (x2,y2) of vector
                // 2 into the equation above.
                d1 = (a1 * v2x1) + (b1 * v2y1) + c1;
                d2 = (a1 * v2x2) + (b1 * v2y2) + c1;

                // If d1 and d2 both have the same sign, they are both on the same side
                // of our line 1 and in that case no intersection is possible. Careful, 
                // 0 is a special case, that's why we don't test ">=" and "<=", 
                // but "<" and ">".
                if (d1 > 0 && d2 > 0) return Constants.NO;
                if (d1 < 0 && d2 < 0) return Constants.NO;

                // The fact that vector 2 intersected the infinite line 1 above doesn't 
                // mean it also intersects the vector 1. Vector 1 is only a subset of that
                // infinite line 1, so it may have intersected that line before the vector
                // started or after it ended. To know for sure, we have to repeat the
                // the same test the other way round. We start by calculating the 
                // infinite line 2 in linear equation standard form.
                a2 = v2y2 - v2y1;
                b2 = v2x1 - v2x2;
                c2 = (v2x2 * v2y1) - (v2x1 * v2y2);

                // Calculate d1 and d2 again, this time using points of vector 1.
                d1 = (a2 * v1x1) + (b2 * v1y1) + c2;
                d2 = (a2 * v1x2) + (b2 * v1y2) + c2;

                // Again, if both have the same sign (and neither one is 0),
                // no intersection is possible.
                if (d1 > 0 && d2 > 0) return Constants.NO;
                if (d1 < 0 && d2 < 0) return Constants.NO;

                // If we get here, only two possibilities are left. Either the two
                // vectors intersect in exactly one point or they are collinear, which
                // means they intersect in any number of points from zero to infinite.
                if ((a1 * b2) - (a2 * b1) == 0.0f) return Constants.COLLINEAR;

                // If they are not collinear, they must intersect in exactly one point.
                return Constants.YES;


            }
            public override void draw(OpenGL gl)
            {
                base.draw(gl);
                gl.LineWidth(Size);
                gl.Begin(OpenGL.GL_LINES);
                for (int i = 1; i < ControlPoints.Count; i++)
                {
                    gl.Vertex(ControlPoints[i - 1].X, ControlPoints[i - 1].Y);
                    gl.Vertex(ControlPoints[i].X, ControlPoints[i].Y);   
                }
                gl.End();
                gl.Flush();// Thực hiện lệnh vẽ ngay lập tức thay vì đợi sau 1 khoảng thời gian

            }

            public override bool IsContain(int X, int Y)
            { 
                // Tìm điểm có tọa đô x lớn nhất trong các đỉnh
                int v1x1, v1y1, v1x2, v1y2;
                int maxX = ControlPoints[1].X;
                for (int i = 2; i < ControlPoints.Count; i++)
                {
                    if (ControlPoints[i].X > maxX)
                        maxX = ControlPoints[i].X;
                }
                v1x1 = X;
                v1y1 = Y;
                v1x2 = maxX + 1;
                v1y2 = Y;

                int v2x1, v2y1, v2x2, v2y2;
                int intersection = 0;
                for (int i = 0; i < ControlPoints.Count - 1; i++)
                {
                    v2x1 = ControlPoints[i].X;
                    v2y1 = ControlPoints[i].Y;
                    v2x2 = ControlPoints[i + 1].X;
                    v2y2 = ControlPoints[i + 1].Y;

                    if (IsIntersection(v1x1, v1y1, v1x2, v1y2, v2x1, v2y1, v2x2, v2y2) == Constants.YES)
                        intersection++;
                }

                if (intersection % 2 != 0)
                    return true;
                return false;
            }
        }

        /* Hàm chức năng bên ngoài */
        private float ConvertValueComboBoxToFloat(string valueCbo)
        {
            string resultString = Regex.Match(valueCbo, @"\d+").Value;
            return float.Parse(resultString);
        }
        void RedrawScreen(OpenGL gl)
        {
            
            foreach (Shape shape in shapes)
            {
                shape.draw(gl);
                if (shape.IsPainted == true)
                {
                    Point point = new Point(shape.SeedPoint.X, shape.SeedPoint.Y);
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    shape.BoundaryFill(gl, pixelArray);
                    watch.Stop();
                    float elapsedMs = watch.ElapsedMilliseconds;
                    elapsedMs /= 1000;
                    txtTime.Text = elapsedMs.ToString();
                    shape.IsPainted = false;
                }
                if (shape.IsClicked == true)
                    shape.DrawControlPoints(gl);
                
            }
        }

        void LoadColor(OpenGL gl, byte[] pixelArray)
        {
            gl.RasterPos(0, 0);
            gl.DrawPixels(gl.RenderContextProvider.Width, gl.RenderContextProvider.Height, OpenGL.GL_RGBA, pixelArray);
        }

        /* Danh sách các biến lưu trữ thông tin giao diện*/
        Color userColor;
        int shShape;
        float shSize;
        Point pStart, pEnd;
        int drawing = 0; // 'drawing' variable helps drawing in mouse move event 
        List<Shape> shapes = new List<Shape>();
        Button selectedButton;
        int isDrawPolygon = 0;
        int selectedIdx = -1; // Selected shape
        bool isPaint = false;
        byte[] pixelArray;
        bool isMove = false;
        Point oldStart = new Point();
        Point oldEnd = new Point();
        Point oldSeedPoint = new Point();
        List<Point> oldControlPoints = new List<Point>();
        
        public Form1()
        {
            InitializeComponent();
            userColor = Color.Black;
            shShape = 0;
            cboSize.SelectedIndex = 0;
            shSize = 1;
            selectedButton = btnSelect;
            selectedButton.BackColor = SystemColors.ActiveCaption;
            btnColorChart.BackColor = userColor;
            pixelArray = new byte[openGLControl.Width * openGLControl.Height * 4];
            for (int i = 0; i < pixelArray.Length; i++)
            {
                pixelArray[i] = 255;
            }
        }


        private void openGLControl_OpenGLInitialized(object sender, EventArgs e)
        {

            // Get the OpenGL object.
            OpenGL gl = openGLControl.OpenGL;
            // Set the clear color.
            gl.ClearColor(255, 255, 255, 255);
            // Set the projection matrix.
            gl.MatrixMode(OpenGL.GL_PROJECTION);
            // Load the identity.
            gl.LoadIdentity();
        }

        private void openGLControl_Resized(object sender, EventArgs e)
        {
            // Get the OpenGL object.
            OpenGL gl = openGLControl.OpenGL;
            // Set the projection matrix.
            gl.MatrixMode(OpenGL.GL_PROJECTION);
            // Load the identity.
            gl.LoadIdentity();
            // Create a perspective transformation.
            gl.Viewport(0, 0, openGLControl.Width, openGLControl.Height);
            gl.Ortho2D(0, openGLControl.Width, 0, openGLControl.Height);
        }

        private void openGLControl_OpenGLDraw(object sender, RenderEventArgs args)
        {
            // Get the OpenGL object.
            OpenGL gl = openGLControl.OpenGL;


            // Clear the color and depth buffer.
            gl.ClearColor(255, 255, 255, 255);
            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);

            LoadColor(gl, pixelArray);
            RedrawScreen(gl);
            
           
            if (shShape == 0)
                openGLControl.Cursor = System.Windows.Forms.Cursors.Default;
            else
                openGLControl.Cursor = System.Windows.Forms.Cursors.Cross;

            if (drawing != 0)
            {
                switch(shShape)
                {
                    case 0: // Chọn hình đã vẽ
                        drawing = 0;
                        break;

                    case 1:
                        Line newLine = new Line
                        {
                            Start = new Point(pStart.X, gl.RenderContextProvider.Height - pStart.Y),
                            End = new Point(pEnd.X, gl.RenderContextProvider.Height - pEnd.Y),
                            BoundaryColor = userColor,
                            Size = shSize,
                            ControlPoints = new List<Point>()
                        };
                        newLine.draw(gl);
                        
                        if (drawing == 2)
                        {
                            newLine.GenerateControlPoints(gl);
                           
                            shapes.Add(newLine);
                            drawing = 0;
                        }
                        break;
                    case 2: // Vẽ hình tròn ở đây
                        Circle circle = new Circle
                        {
                            Start = pStart,
                            End = pEnd,
                            BoundaryColor = userColor,
                            Size = shSize,
                            ControlPoints = new List<Point>()
                        };

                        circle.draw(gl);
                        if (drawing == 2)
                        {
                            // getPixel(gl, 2, 2);
                            shapes.Add(circle);
                            ////getPixel(gl, circle.Xc, circle.Yc);
                            //circle.FillColor = Color.Red;
                            //BoundaryFill(gl, circle, circle.Xc, circle.Yc);
                            drawing = 0;
                        }
                        break;
                    case 3: // Vẽ hình ellipse
                        Ellipse ellipse = new Ellipse
                        {
                            Start = pStart,
                            End = pEnd,
                            BoundaryColor = userColor,
                            Size = shSize,
                            ControlPoints = new List<Point>()
                        };
                        ellipse.draw(gl);
                        if (drawing == 2)
                        {
                            shapes.Add(ellipse);
                            drawing = 0;
                        }
                        break;
                    case 4: // Vẽ hình chữ nhật ở đây
                        Rectangle rectangle = new Rectangle
                        {
                            Start = pStart,
                            End = pEnd,
                            BoundaryColor = userColor,
                            Size = shSize,
                            ControlPoints = new List<Point>()
                        };
                        rectangle.draw(gl);
                        if (drawing == 2)
                        {
                            shapes.Add(rectangle);
                            drawing = 0;
                        }
                        break;
                    case 5: // Vẽ hình tam giác đều ở đây
                        Triangle triangle = new Triangle
                        {
                            Start = pStart,
                            End = pEnd,
                            BoundaryColor = userColor,
                            Size = shSize,
                            ControlPoints = new List<Point>()
                        };
                        triangle.draw(gl);
                        if (drawing == 2)
                        {
                            shapes.Add(triangle);

                            drawing = 0;
                        }
                        break;
                    case 6: // Vẽ hình ngũ giác đều ở đây
                        break;
                    case 7: // Vẽ hình lục giác đều ở đây
                        break;
                    case 8: // Vẽ đa giác ở đây
                        if (isDrawPolygon == 0)
                        {
                            Polygon polygon = new Polygon
                            {
                                Start = pStart,
                                End = pEnd,
                                BoundaryColor = userColor,
                                Size = shSize,
                                ControlPoints = new List<Point>()
                            };
                            shapes.Add(polygon);
                            isDrawPolygon = 1;
                        }

                        Line line = new Line
                        {
                            Start = new Point(pStart.X, gl.RenderContextProvider.Height - pStart.Y),
                            End = new Point(pEnd.X, gl.RenderContextProvider.Height - pEnd.Y),
                            BoundaryColor = userColor,
                            Size = shSize,
                            ControlPoints = new List<Point>()
                        };
                        line.draw(gl);
                        if (drawing == 2)
                        {
                            if (isDrawPolygon == 1)
                            {
                                shapes[shapes.Count - 1].ControlPoints.Add(
                                    new Point(line.Start.X, line.Start.Y));
                                shapes[shapes.Count - 1].ControlPoints.Add(
                                    new Point(line.End.X, line.End.Y));
                                pStart = pEnd;
                                drawing = 1;
                            }
                            if (isDrawPolygon == 2)
                            {
                                Point firstControlPoints = new Point(shapes[shapes.Count - 1].ControlPoints[0].X,
                                    shapes[shapes.Count - 1].ControlPoints[0].Y);
                                shapes[shapes.Count - 1].ControlPoints.Add(
                                   new Point(line.Start.X, line.Start.Y));
                                shapes[shapes.Count - 1].ControlPoints.Add(firstControlPoints);

                                isDrawPolygon = 0;
                                drawing = 0;
                                // shapes[shapes.Count - 1].IsClicked = true;
                            }
                        }

                        break;
                    
                    default:
                        drawing = 0;
                        break;

                }
            }

            if (isPaint == true)
            {
                if (selectedIdx != -1)
                {
                    //byte[] pixel = new byte[4];
                    //int X = shapes[selectedIdx].ControlPoints[1].X;
                    //int Y = shapes[selectedIdx].ControlPoints[1].Y;
                    //gl.ReadPixels(X, Y, 1, 1, OpenGL.GL_RGBA, OpenGL.GL_UNSIGNED_BYTE, pixel);

                    //X = shapes[selectedIdx].SeedPoint.X;
                    //Y = shapes[selectedIdx].SeedPoint.Y;
                    //gl.ReadPixels(X, Y, 1, 1, OpenGL.GL_RGBA, OpenGL.GL_UNSIGNED_BYTE, pixel);
                    //shapes[selectedIdx].FillColor = userColor;
                    //shapes[selectedIdx].BoundaryFill(gl);
                    //isPaint = false;

                    shapes[selectedIdx].IsPainted = true;
                    shapes[selectedIdx].FillColor = userColor;
                    isPaint = false;
                }
            }
        }

        /* Mouse event */
        private void openGLControl_MouseUp(object sender, MouseEventArgs e)
        {
            isMove = false;
            if (e.Button == MouseButtons.Left && shShape != 0)
            {
                pEnd = e.Location;
                drawing = 2;
                
            }
        }

        private void openGLControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMove == false)
                pEnd = e.Location;
            if (isMove == true)
            {
                pEnd = e.Location;
  
                if (shapes[selectedIdx].GetType().Name != "Polygon")
                {
                    int dx = pEnd.X - pStart.X;
                    int dy = pEnd.Y - pStart.Y;
                    if (shapes[selectedIdx].GetType().Name == "Line")
                        dy = -dy;
                    Point newStart = new Point(oldStart.X + dx, oldStart.Y + dy);
                    Point newEnd = new Point(oldEnd.X + dx, oldEnd.Y + dy);
                    Point newSeedPoint = new Point(oldSeedPoint.X + dx, oldSeedPoint.Y + dy);
                    shapes[selectedIdx].Start = newStart;
                    shapes[selectedIdx].End = newEnd;
                    shapes[selectedIdx].SeedPoint = newSeedPoint;
                    shapes[selectedIdx].ControlPoints.Clear();
                }
                else
                {
                    int dx = pEnd.X - pStart.X;
                    int dy = pStart.Y - pEnd.Y;
                    for (int i = 0; i < shapes[selectedIdx].ControlPoints.Count; i++)
                    {
                        int newX = oldControlPoints[i].X + dx;
                        int newY = oldControlPoints[i].Y + dy;

                        Point newPoint = new Point(newX, newY);
                       
                        shapes[selectedIdx].ControlPoints[i] = newPoint;
                        
                    }
                    Point newSeedPoint = new Point(oldSeedPoint.X + dx, oldSeedPoint.Y + dy);
                    shapes[selectedIdx].SeedPoint = newSeedPoint;
                }
                
            }
        }

        private void openGLControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && shShape != 0)
            {
                pStart = e.Location;
                pEnd = pStart;
                drawing = 1;
            }
            else if (e.Button == MouseButtons.Left && shShape == 0)
            {
                pStart = e.Location;
                pEnd = pStart;
                // Get the OpenGL object.
                OpenGL gl = openGLControl.OpenGL;
                selectedIdx = -1;
                for (int i = shapes.Count - 1; i >= 0; i--)
                {
                    int X = e.Location.X;
                    int Y = gl.RenderContextProvider.Height - e.Location.Y;
                    shapes[i].IsClicked = false;
                    if (shapes[i].IsContain(X, Y) == true && selectedIdx == -1)
                    {
                        shapes[i].IsClicked = true;
                        shapes[i].SeedPoint = new Point(X, Y);
                        selectedIdx = i;
                        isMove = true;

                        if (shapes[i].GetType().Name == "Polygon")
                        {
                            oldControlPoints.Clear();
                            foreach (Point point in shapes[i].ControlPoints)
                                oldControlPoints.Add(point);
                        }
                        else
                        {
                            //if (shapes[i].GetType().Name == "Line")
                            //{
                            //    oldStart = new Point(shapes[i].Start.X,
                            //        gl.RenderContextProvider.Height - shapes[i].Start.Y);
                            //    oldEnd = new Point(shapes[i].End.X,
                            //        gl.RenderContextProvider.Height - shapes[i].End.Y);
                            //}

                            oldStart = shapes[i].Start;
                            oldEnd = shapes[i].End;

                        }
                        oldSeedPoint = shapes[i].SeedPoint;
                    }
                }
            }

        }

        private void openGLControl_MouseClick(object sender, MouseEventArgs e)
        {
            MouseEventArgs me = (MouseEventArgs)e;
            if (me.Button == MouseButtons.Right)
            {
                drawing = 2;
                isDrawPolygon = 2;
            }
            


        }

        /* Nhóm chức năng phụ: chọn màu, chọn kích cỡ,...*/
        private void btnColorChart_Click(object sender, EventArgs e)
        {
            // Nếu người dùng chọn xong và bấm ok
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                userColor = colorDialog1.Color;
                btnColorChart.BackColor = userColor;
            }
        }

        private void cboSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            string size = comboBox.SelectedItem.ToString();
            shSize = ConvertValueComboBoxToFloat(size);
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            // Empty the shapes list
            shapes.Clear();
            for (int i = 0; i < pixelArray.Length; i++)
                pixelArray[i] = 255;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            Form1 form1 = (Form1)sender;
            openGLControl.Height = form1.Height - pnToolbar.Height - 50;
        }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            selectedButton.BackColor = SystemColors.Control;
            selectedButton = (Button)sender;
            selectedButton.BackColor = SystemColors.ActiveCaption;
            shShape = 0;
            
        }

        private void btnPaint_Click(object sender, EventArgs e)
        {
            isPaint = true;
        }


        /* Nhóm chức năng vẽ hình */
        private void btnDrawLine_Click(object sender, EventArgs e)
        {
            selectedButton.BackColor = SystemColors.Control;
            selectedButton = (Button)sender;
            selectedButton.BackColor = SystemColors.ActiveCaption;
            shShape = 1;
        }

        private void btnDrawCircle_Click(object sender, EventArgs e)
        {
            selectedButton.BackColor = SystemColors.Control;
            selectedButton = (Button)sender;
            selectedButton.BackColor = SystemColors.ActiveCaption;
            shShape = 2;
        }

        private void btnEllipse_Click(object sender, EventArgs e)
        {
            selectedButton.BackColor = SystemColors.Control;
            selectedButton = (Button)sender;
            selectedButton.BackColor = SystemColors.ActiveCaption;
            shShape = 3;
        }

        private void btnRectangle_Click(object sender, EventArgs e)
        {
            selectedButton.BackColor = SystemColors.Control;
            selectedButton = (Button)sender;
            selectedButton.BackColor = SystemColors.ActiveCaption;
            shShape = 4;
        }

        private void btnTriangle_Click(object sender, EventArgs e)
        {
            selectedButton.BackColor = SystemColors.Control;
            selectedButton = (Button)sender;
            selectedButton.BackColor = SystemColors.ActiveCaption;
            shShape = 5;
        }

        private void btnPentagon_Click(object sender, EventArgs e)
        {
            selectedButton.BackColor = SystemColors.Control;
            selectedButton = (Button)sender;
            selectedButton.BackColor = SystemColors.ActiveCaption;
            shShape = 6;
        }

        private void btnHexagon_Click(object sender, EventArgs e)
        {
            selectedButton.BackColor = SystemColors.Control;
            selectedButton = (Button)sender;
            selectedButton.BackColor = SystemColors.ActiveCaption;
            shShape = 7;
        }

       

        private void btnPolygon_Click(object sender, EventArgs e)
        {
            selectedButton.BackColor = SystemColors.Control;
            selectedButton = (Button)sender;
            selectedButton.BackColor = SystemColors.ActiveCaption;
            shShape = 8;
        }    
    }
}

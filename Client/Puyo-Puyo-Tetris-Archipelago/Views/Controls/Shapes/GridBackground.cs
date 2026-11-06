using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Puyo_Puyo_Tetris_Archipelago.Views.Controls.Shapes
{
    /// <summary>
    /// Gray and white grid background
    /// </summary>
    public class GridBackground : FrameworkElement
    {
        // How far back the main grid sits
        private const double RecessDepth = 0.25;

        // Pixel size per tile
        private const double TilePixelSize = 80.0;
        // Amount of tiles to overshoot past the height and width of the window so the grid effect goes offscreen
        private const double Overscan = 1.0;
        // Depth rings down the walls
        private const int DepthCells = 7;

        // The color of the tiles
        private static readonly SolidColorBrush CellBrush;
        // The color of the lines between the tiles
        private static readonly Pen LinePen;

        // Current render size globals
        private double _width;
        private double _height;
        private double _aspect;

        static GridBackground()
        {
            // Colors set once and frozen
            CellBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D0D0D0"));
            CellBrush.Freeze();
            LinePen = new Pen(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")), 1.0);
            LinePen.Freeze();
        }

        public GridBackground()
        {
            // Keep the box inside its parent, and re-render on window size change
            ClipToBounds = true;
            SizeChanged += (_, _) => InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            // Set resolution globals
            double width = ActualWidth;
            double height = ActualHeight;
            if (width <= 0 || height <= 0) return;

            _width = width;
            _height = height;
            _aspect = width / height;

            // Height and widths of the sides and flat planes
            const double halfWidth = 1.0;
            double halfHeight = halfWidth / _aspect;
            const double sidesZ = -1.0;
            const double flatZ = -(1.0 + RecessDepth);

            // Column amount that keeps tile size closest to TilePixelSize
            int columns = Math.Max(1, (int)Math.Round(width / TilePixelSize));
            double cellSize = 2.0 * halfWidth / columns;

            // Overscan height
            double halfHeightOverscan = halfHeight + Overscan * cellSize;

            // The corners of the walls
            Point3D sidesTopLeft = new Point3D(-halfWidth, halfHeightOverscan, sidesZ);
            Point3D sidesTopRight = new Point3D(halfWidth, halfHeightOverscan, sidesZ);
            Point3D sidesBottomRight = new Point3D(halfWidth, -halfHeightOverscan, sidesZ);
            Point3D sidesBottomLeft = new Point3D(-halfWidth, -halfHeightOverscan, sidesZ);

            // The corners of the flat background
            double flatInset = cellSize;
            Point3D flatTopLeft = new Point3D(-halfWidth + flatInset, halfHeightOverscan - flatInset, flatZ);
            Point3D flatTopRight = new Point3D(halfWidth - flatInset, halfHeightOverscan - flatInset, flatZ);
            Point3D flatBottomRight = new Point3D(halfWidth - flatInset, -halfHeightOverscan + flatInset, flatZ);
            Point3D flatBottomLeft = new Point3D(-halfWidth + flatInset, -halfHeightOverscan + flatInset, flatZ);

            // Rows according to the cell size
            int rows = Math.Max(1, (int)Math.Round(2.0 * halfHeightOverscan / cellSize));

            // Fill the sides
            StreamGeometry panel = new StreamGeometry();
            using (StreamGeometryContext ctx = panel.Open())
            {
                ctx.BeginFigure(Project(sidesTopLeft), true, true);
                ctx.LineTo(Project(sidesTopRight), false, false);
                ctx.LineTo(Project(sidesBottomRight), false, false);
                ctx.LineTo(Project(sidesBottomLeft), false, false);
            }
            panel.Freeze();
            drawingContext.DrawGeometry(CellBrush, null, panel);

            // Fill the flat backpanel
            StreamGeometry flatPanel = new StreamGeometry();
            using (StreamGeometryContext ctx = flatPanel.Open())
            {
                ctx.BeginFigure(Project(flatTopLeft), true, true);
                ctx.LineTo(Project(flatTopRight), false, false);
                ctx.LineTo(Project(flatBottomRight), false, false);
                ctx.LineTo(Project(flatBottomLeft), false, false);
            }
            flatPanel.Freeze();
            drawingContext.DrawGeometry(CellBrush, null, flatPanel);

            // Add the grid lines across all panels
            StreamGeometry lines = new StreamGeometry();
            using (StreamGeometryContext ctx = lines.Open())
            {
                AddFaceGrid(ctx, sidesTopLeft, sidesTopRight, flatTopRight, flatTopLeft, columns, DepthCells); // top
                AddFaceGrid(ctx, sidesTopRight, sidesBottomRight, flatBottomRight, flatTopRight, rows, DepthCells); // right
                AddFaceGrid(ctx, sidesBottomRight, sidesBottomLeft, flatBottomLeft, flatBottomRight, columns, DepthCells); // bottom
                AddFaceGrid(ctx, sidesBottomLeft, sidesTopLeft, flatTopLeft, flatBottomLeft, rows, DepthCells); // left
                AddFaceGrid(ctx, flatTopLeft, flatTopRight, flatBottomRight, flatBottomLeft, columns, rows); // background
            }
            lines.Freeze();
            drawingContext.DrawGeometry(null, LinePen, lines);
        }

        /// <summary>
        /// Projects a 3D point to the screen, camera at the origin looking down -Z
        /// </summary>
        private Point Project(Point3D point)
        {
            double depth = -point.Z;
            double projectedX = point.X / depth;
            double projectedY = point.Y / depth * _aspect;
            return new Point((projectedX + 1.0) * 0.5 * _width, (1.0 - projectedY) * 0.5 * _height);
        }

        /// <summary>
        /// Draws the grid on a quad
        /// </summary>
        private void AddFaceGrid(StreamGeometryContext ctx, Point3D a, Point3D b, Point3D c, Point3D d, int columns, int rows)
        {
            // Lines across the quad, rows
            for (int row = 0; row <= rows; row++)
            {
                double v = (double)row / rows;
                ctx.BeginFigure(ProjectUV(a, b, c, d, 0.0, v), false, false);
                for (int column = 1; column <= columns; column++)
                    ctx.LineTo(ProjectUV(a, b, c, d, (double)column / columns, v), true, false);
            }

            // Lines across the quad, columns
            for (int column = 0; column <= columns; column++)
            {
                double u = (double)column / columns;
                ctx.BeginFigure(ProjectUV(a, b, c, d, u, 0.0), false, false);
                for (int row = 1; row <= rows; row++)
                    ctx.LineTo(ProjectUV(a, b, c, d, u, (double)row / rows), true, false);
            }
        }

        /// <summary>
        /// Picks a point on the quad (corners as in AddFaceGrid) and projects it
        /// </summary>
        private Point ProjectUV(Point3D a, Point3D b, Point3D c, Point3D d, double u, double v)
        {
            Point3D top = Lerp(a, b, u);
            Point3D bottom = Lerp(d, c, u);
            return Project(Lerp(top, bottom, v));
        }

        private static Point3D Lerp(Point3D a, Point3D b, double t) => a + (b - a) * t;
    }
}

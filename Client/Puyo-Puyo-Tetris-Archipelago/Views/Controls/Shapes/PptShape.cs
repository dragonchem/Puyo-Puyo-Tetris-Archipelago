using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DependencyPropertyGenerator;

namespace Puyo_Puyo_Tetris_Archipelago.Views.Controls.Shapes
{
    /// <summary>
    /// PPT style shape defined as four points
    /// </summary>

    // Scale the shape to fill the parent
    [DependencyProperty<bool>("Scalable", DefaultValue = false)]
    // Show the halftone dots background
    [DependencyProperty<bool>("ShowDots", DefaultValue = false)]
    // Points, 1 = topleft, 2 = topright, 3 = bottomright, 4 = bottomleft
    [DependencyProperty<Point>("Point1")]
    [DependencyProperty<Point>("Point2")]
    [DependencyProperty<Point>("Point3")]
    [DependencyProperty<Point>("Point4")]

    // Brush to use to fill the background
    [DependencyProperty<Brush>("Fill")]
    // Brush to use for the halftone dots if ShowDots is true
    [DependencyProperty<Brush>("DotBrush")]
    // Amount of pixels between the dots
    [DependencyProperty<double>("DotSpacing", DefaultValue = 16.0)]
    // Size range of the dots
    [DependencyProperty<double>("DotMinDiameter", DefaultValue = 3.0)]
    [DependencyProperty<double>("DotMaxDiameter", DefaultValue = 10.0)]
    // Angle that the dot grid is rendered at
    [DependencyProperty<double>("DotAngle", DefaultValue = 0.0)]
    // Angle at which the dots grow along
    [DependencyProperty<double>("GrowAngle", DefaultValue = 0.0)]
    // Inner outline color and width
    [DependencyProperty<Brush>("InnerOutlineBrush")]
    [DependencyProperty<double>("InnerOutlineWidth", DefaultValue = 4.0)]
    // Outer outline color and width
    [DependencyProperty<Brush>("OuterOutlineBrush")]
    [DependencyProperty<double>("OuterOutlineWidth", DefaultValue = 6.0)]
    // Offset to draw the drop shadow at in pixels
    [DependencyProperty<Vector>("ShadowOffset")]
    public partial class PptShape : Decorator
    {
        // Always gray, 40% transparent
        private static readonly Brush ShadowBrush = MakeBrush("#303030");
        private const double ShadowOpacity = 0.4;

        public PptShape()
        {
            // if size of element changes, re-render
            SizeChanged += (_, __) => InvalidateVisual();

            // Default shape
            Point1 = new Point(0, 0);
            Point2 = new Point(0, 100);
            Point3 = new Point(100, 100);
            Point4 = new Point(100, 0);

            // Default shadow offset
            ShadowOffset = new Vector(7, 9);
        }

        // Rerender if property changes
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property.OwnerType == typeof(PptShape))
            {
                InvalidateMeasure();
                InvalidateVisual();
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            // Scale corners if Scalable is true
            Size size = new Size(ActualWidth, ActualHeight);
            Point corner1 = ScaleCorner(Point1, size);
            Point corner2 = ScaleCorner(Point2, size);
            Point corner3 = ScaleCorner(Point3, size);
            Point corner4 = ScaleCorner(Point4, size);

            // Build the quad from the four corners
            StreamGeometry quad = new StreamGeometry();
            using (StreamGeometryContext ctx = quad.Open())
            {
                ctx.BeginFigure(corner1, true, true);
                ctx.LineTo(corner2, true, false);
                ctx.LineTo(corner3, true, false);
                ctx.LineTo(corner4, true, false);
            }
            quad.Freeze();

            // Render drop shadow first
            if (ShadowOffset.X != 0 || ShadowOffset.Y != 0)
            {
                // Set shadow pen to the size of both outline widths so the shadow grows
                double reach = InnerOutlineWidth + OuterOutlineWidth;
                Pen? shadowPen = null;
                if (reach > 0) shadowPen = new Pen(ShadowBrush, 2 * reach) { LineJoin = PenLineJoin.Miter, MiterLimit = 10 };
                // Draw shadow as solid shape with opacity to prevent drawing over itself multiple times
                drawingContext.PushOpacity(ShadowOpacity);
                drawingContext.PushTransform(new TranslateTransform(ShadowOffset.X, ShadowOffset.Y));
                drawingContext.DrawGeometry(ShadowBrush, shadowPen, quad);
                drawingContext.Pop();
                drawingContext.Pop();
            }

            // Draw outlines, outer first, inner after
            DrawOutline(drawingContext, quad, OuterOutlineBrush, 2 * (InnerOutlineWidth + OuterOutlineWidth));
            DrawOutline(drawingContext, quad, InnerOutlineBrush, 2 * InnerOutlineWidth);

            // Draw the gradient background
            if (Fill != null) drawingContext.DrawGeometry(Fill, null, quad);

            // Draw the dots, clipped to the quad so that they don't bleed
            if (ShowDots && DotBrush != null)
            {
                // Scale the dots with the shape so the halftone looks the same at any resolution
                double dotScale = ContentScale(size);
                drawingContext.PushClip(quad);
                DrawDots(drawingContext, corner1, corner2, corner3, corner4, dotScale);
                drawingContext.Pop();
            }
        }

        /// <summary>
        /// Draws one outline around the quad
        /// </summary>
        private static void DrawOutline(DrawingContext drawingContext, Geometry geometry, Brush? brush, double thickness)
        {
            if (brush == null || thickness <= 0) return;
            drawingContext.DrawGeometry(null, new Pen(brush, thickness), geometry);
        }

        /// <summary>
        /// Draws the halftone dots over the background
        /// </summary>
        private void DrawDots(DrawingContext drawingContext, Point corner1, Point corner2, Point corner3, Point corner4, double scale)
        {
            // Box around the quad, so the dots fill and grow across the shape, not the parent
            double minX = Math.Min(Math.Min(corner1.X, corner2.X), Math.Min(corner3.X, corner4.X));
            double maxX = Math.Max(Math.Max(corner1.X, corner2.X), Math.Max(corner3.X, corner4.X));
            double minY = Math.Min(Math.Min(corner1.Y, corner2.Y), Math.Min(corner3.Y, corner4.Y));
            double maxY = Math.Max(Math.Max(corner1.Y, corner2.Y), Math.Max(corner3.Y, corner4.Y));
            double boxWidth = maxX - minX;
            double boxHeight = maxY - minY;
            if (boxWidth <= 0 || boxHeight <= 0) return;

            // Negative spacing would break, then scale it with the shape
            double spacing = DotSpacing;
            if (DotSpacing <= 0) spacing = 16.0;
            spacing *= scale;

            // Dot angle in cosine and sine
            double dotsAngle = DotAngle * Math.PI / 180.0;
            double dotsCos = Math.Cos(dotsAngle);
            double dotsSin = Math.Sin(dotsAngle);

            // Amount of pixels moved X and Y for column and row in pixels
            double columnStepX = dotsCos * spacing;
            double columnStepY = dotsSin * spacing;
            double rowStepX = -dotsSin * spacing;
            double rowStepY = dotsCos * spacing;

            // Grow angle
            double growAngle = GrowAngle * Math.PI / 180.0;
            double growCos = Math.Cos(growAngle);
            double growSin = Math.Sin(growAngle);

            // The four corners on the grow axis
            double cornerA = minX * growCos + minY * growSin;
            double cornerB = maxX * growCos + minY * growSin;
            double cornerC = minX * growCos + maxY * growSin;
            double cornerD = maxX * growCos + maxY * growSin;

            // Size range
            double minSize = Math.Min(Math.Min(cornerA, cornerB), Math.Min(cornerC, cornerD));
            double maxSize = Math.Max(Math.Max(cornerA, cornerB), Math.Max(cornerC, cornerD));
            double sizeRange = Math.Max(1e-6, maxSize - minSize);

            double minRadius = DotMinDiameter / 2.0 * scale;
            double maxRadius = DotMaxDiameter / 2.0 * scale;
            double margin = Math.Max(minRadius, maxRadius) + 1.0;

            // Walk out from the center far enough to cover the whole box, even at odd angles
            double centerX = (minX + maxX) / 2.0;
            double centerY = (minY + maxY) / 2.0;
            double coverRadius = 0.5 * Math.Sqrt(boxWidth * boxWidth + boxHeight * boxHeight) + spacing;
            int steps = (int)Math.Ceiling(coverRadius / spacing) + 1;

            for (int row = -steps; row <= steps; row++)
            {
                // Offset every other row for the staggered look
                double staggeredColumn = 0.0;
                if ((row & 1) != 0) staggeredColumn = 0.5;
                for (int column = -steps; column <= steps; column++)
                {
                    double columnIndex = column + staggeredColumn;
                    double x = centerX + columnIndex * columnStepX + row * rowStepX;
                    double y = centerY + columnIndex * columnStepY + row * rowStepY;

                    // Skip dots outside the box, with a dot of margin for partial ones
                    if (x < minX - margin || x > maxX + margin || y < minY - margin || y > maxY + margin) continue;

                    // Position -> size
                    double growT = Math.Clamp(((x * growCos + y * growSin) - minSize) / sizeRange, 0, 1);
                    double radius = minRadius + (maxRadius - minRadius) * growT;
                    drawingContext.DrawEllipse(DotBrush, null, new Point(x, y), radius, radius);
                }
            }
        }

        /// <summary>
        /// The quad's raw size from its points
        /// </summary>
        private Size QuadExtent()
        {
            double maxX = Math.Max(Math.Max(Point1.X, Point2.X), Math.Max(Point3.X, Point4.X));
            double maxY = Math.Max(Math.Max(Point1.Y, Point2.Y), Math.Max(Point3.Y, Point4.Y));
            return new Size(maxX, maxY);
        }

        /// <summary>
        /// The size we want to be including outlines and shadow, only used when scalable is false
        /// </summary>
        private Size DesignSize()
        {
            Size quad = QuadExtent();
            double band = InnerOutlineWidth + OuterOutlineWidth;
            return new Size(
                quad.Width + band + Math.Max(0, ShadowOffset.X),
                quad.Height + band + Math.Max(0, ShadowOffset.Y)
            );
        }

        /// <summary>
        /// Maps a corner into the render size, stretched to fill when Scalable
        /// </summary>
        private Point ScaleCorner(Point corner, Size size)
        {
            double scaleX = 1.0;
            double scaleY = 1.0;
            if (Scalable && size.Width > 0 && size.Height > 0)
            {
                Size extent = QuadExtent();
                if (extent.Width > 0 && extent.Height > 0)
                {
                    scaleX = size.Width / extent.Width;
                    scaleY = size.Height / extent.Height;
                }
            }
            return new Point(corner.X * scaleX, corner.Y * scaleY);
        }

        /// <summary>
        /// How much the quad got scaled up, 1 when not Scalable
        /// </summary>
        private double ContentScale(Size size)
        {
            if (!Scalable || size.Width <= 0 || size.Height <= 0) return 1.0;
            Size extent = QuadExtent();
            if (extent.Width <= 0 || extent.Height <= 0) return 1.0;

            // Average the two axes so round dots stay round
            double scaleX = size.Width / extent.Width;
            double scaleY = size.Height / extent.Height;
            return Math.Sqrt(scaleX * scaleY);
        }

        /// <summary>
        /// The upright box sitting at the midpoint of the (scaled) quad's slant, where the child is put
        /// </summary>
        private Rect QuadBounds(Size size)
        {
            Point topLeft = ScaleCorner(Point1, size);
            Point topRight = ScaleCorner(Point2, size);
            Point bottomRight = ScaleCorner(Point3, size);
            Point bottomLeft = ScaleCorner(Point4, size);

            // Put each edge halfway along the slant between its two corners
            double left = (topLeft.X + bottomLeft.X) / 2;
            double right = (topRight.X + bottomRight.X) / 2;
            double top = (topLeft.Y + topRight.Y) / 2;
            double bottom = (bottomLeft.Y + bottomRight.Y) / 2;
            return new Rect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
        }


        protected override Size MeasureOverride(Size availableSize)
        {
            // Measure the child but don't let it affect our size due to shape
            Child?.Measure(availableSize);
            if (Scalable) return new Size(0, 0);
            return DesignSize();
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            // Drop the child in the quad box so that it uses the quad for size and position
            Child?.Arrange(QuadBounds(arrangeSize));
            return arrangeSize;
        }

        /// <summary>
        /// Solid brush from a hex color
        /// </summary>
        private static SolidColorBrush MakeBrush(string hex)
        {
            SolidColorBrush brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            return brush;
        }

        /// <summary>
        /// Top to bottom gradient from two hex colors
        /// </summary>
        private static LinearGradientBrush MakeGradient(string topHex, string bottomHex)
        {
            LinearGradientBrush brush = new LinearGradientBrush(
                (Color)ColorConverter.ConvertFromString(topHex),
                (Color)ColorConverter.ConvertFromString(bottomHex),
                new Point(0, 0),
                new Point(0, 1)
            );
            brush.Freeze();
            return brush;
        }
    }
}

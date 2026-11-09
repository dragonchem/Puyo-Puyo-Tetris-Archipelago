using System;
using System.Windows;
using System.Windows.Media;
using DependencyPropertyGenerator;

namespace Puyo_Puyo_Tetris_Archipelago.Views.Controls.Shapes
{
    /// <summary>
    /// Dot halftone background in the PPT style
    /// </summary>

    // Draw the gradient background behind the dots
    [DependencyProperty<bool>("DrawBackground", DefaultValue = true)]
    // Background gradient colors, top to bottom
    [DependencyProperty<Color>("BackgroundStartColor")]
    [DependencyProperty<Color>("BackgroundEndColor")]
    // Dot fill color
    [DependencyProperty<Color>("DotColor")]
    // Dot outline color and thickness
    [DependencyProperty<Color>("DotOutlineColor")]
    [DependencyProperty<double>("DotOutlineThickness", DefaultValue = 0.8)]
    // Size range of the dots
    [DependencyProperty<double>("MinDotRadius", DefaultValue = 2.0)]
    [DependencyProperty<double>("MaxDotRadius", DefaultValue = 5.0)]
    // Amount of pixels between the dots
    [DependencyProperty<double>("DotSpacing", DefaultValue = 16.0)]
    // Angle that the dot grid is rendered at
    [DependencyProperty<double>("DotAngle", DefaultValue = 0.0)]
    // Angle at which the dots grow along
    [DependencyProperty<double>("GrowAngle", DefaultValue = 90.0)]
    public partial class DotBackground : FrameworkElement
    {
        public DotBackground()
        {
            // Keep the dots inside the parent
            ClipToBounds = true;

            // Trigger re-render on window size change
            SizeChanged += (_, __) => InvalidateVisual();

            // Default color values
            DotColor = (Color)ColorConverter.ConvertFromString("#C9C9D0");
            DotOutlineColor = (Color)ColorConverter.ConvertFromString("#CEB4F1");
            BackgroundStartColor = (Color)ColorConverter.ConvertFromString("#FFFFFF");
            BackgroundEndColor = (Color)ColorConverter.ConvertFromString("#EFEFF3");
        }

        // Rerender if property changes
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property.OwnerType == typeof(DotBackground))
            {
                InvalidateMeasure();
                InvalidateVisual();
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            // Grab the render size
            double width = ActualWidth;
            double height = ActualHeight;
            if (width <= 0 || height <= 0) return;

            // Gradient fill behind the dots
            if (DrawBackground)
            {
                LinearGradientBrush background = new LinearGradientBrush(BackgroundStartColor, BackgroundEndColor, 90);
                drawingContext.DrawRectangle(background, null, new Rect(0, 0, width, height));
            }

            // Guard negative dotspacing
            double spacing = DotSpacing;
            if (spacing <= 0) spacing = 1;

            // Angle calculation
            double angleRadian = DotAngle * Math.PI / 180.0;
            double cosAngle = Math.Cos(angleRadian);
            double sinAngle = Math.Sin(angleRadian);

            // Position difference between dots per step
            double columnStepX = cosAngle * spacing;
            double columnStepY = sinAngle * spacing;
            double rowStepX = -sinAngle * spacing;
            double rowStepY = cosAngle * spacing;

            // Size grow direction, separate from dot angle
            double growAngleRadian = GrowAngle * Math.PI / 180.0;
            double growDirectionX = Math.Cos(growAngleRadian);
            double growDirectionY = Math.Sin(growAngleRadian);

            // Where the four corners sit on the growth direction
            double growDistanceTopLeft = 0;
            double growDistanceTopRight = width * growDirectionX;
            double growDistanceBottomLeft = height * growDirectionY;
            double growDistanceBottomRight = width * growDirectionX + height * growDirectionY;

            // Find the smallest and biggest distance
            double growDistanceMin = Math.Min(
                Math.Min(growDistanceTopLeft, growDistanceTopRight),
                Math.Min(growDistanceBottomLeft, growDistanceBottomRight)
            );
            double growDistanceMax = Math.Max(
                Math.Max(growDistanceTopLeft, growDistanceTopRight),
                Math.Max(growDistanceBottomLeft, growDistanceBottomRight)
            );

            // Range between the two, guard against zero
            double growDistanceRange = Math.Max(0.000001, growDistanceMin - growDistanceMax);

            // Dot fill, frozen
            SolidColorBrush fill = new SolidColorBrush(DotColor);
            fill.Freeze();

            // If outlined, create a pen brush to draw it
            Pen? pen = null;
            if (DotOutlineThickness > 0)
            {
                pen = new Pen(new SolidColorBrush(DotOutlineColor), DotOutlineThickness);
                pen.Freeze();
            }

            double margin = Math.Max(MinDotRadius, MaxDotRadius) + 1.0;

            // Walk out from the center far enough to cover the whole element, even at odd angles
            double centerX = width / 2.0;
            double centerY = height / 2.0;
            double reach = 0.5 * Math.Sqrt(width * width + height * height) + spacing;
            int numberOfDotsPerHalf = (int)Math.Ceiling(reach / spacing) + 1;

            // Render the rows of the dots, starting from the centre
            for (int rowIndex = -numberOfDotsPerHalf; rowIndex <= numberOfDotsPerHalf; rowIndex++)
            {
                // Offset for alternating rows
                double stagger;
                if (rowIndex % 2 == 0) stagger = 0.0;
                else stagger = 0.5;

                // Render the columns per row
                for (int columnIndex = -numberOfDotsPerHalf; columnIndex <= numberOfDotsPerHalf; columnIndex++)
                {
                    // Alternate if needed
                    double staggeredColumn = columnIndex + stagger;

                    // Raw position the dot should be drawn at
                    double dotX = centerX + staggeredColumn * columnStepX + rowIndex * rowStepX;
                    double dotY = centerY + staggeredColumn * columnStepY + rowIndex * rowStepY;

                    // Skip dots that fall outside the element, with a single dot worth of margin for partial rendering
                    bool outsideElement = dotX < -margin || dotX > width + margin ||
                                          dotY < -margin || dotY > height + margin;
                    if (outsideElement)
                    {
                        continue;
                    }

                    // Position to size
                    double growDistance = dotX * growDirectionX + dotY * growDirectionY;
                    double sizeFactor = Math.Clamp((growDistance - growDistanceMin) / growDistanceRange, 0.0, 1.0);

                    // Draw dot at position at size
                    double radius = MinDotRadius + (MaxDotRadius - MinDotRadius) * sizeFactor;
                    drawingContext.DrawEllipse(fill, pen, new Point(dotX, dotY), radius, radius);
                }
            }
        }
    }
}
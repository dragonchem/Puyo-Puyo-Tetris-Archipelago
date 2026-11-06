using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using DependencyPropertyGenerator;

namespace Puyo_Puyo_Tetris_Archipelago.Views.Controls.Shapes
{
    /// <summary>
    /// Outlined menu text in the PPT style
    /// </summary>

    // Scale the word to fill the element
    [DependencyProperty<bool>("Scalable", DefaultValue = false)]
    // The word to render
    [DependencyProperty<string>("Text", DefaultValue = "Text")]
    // Angle to tilt the whole word at
    [DependencyProperty<double>("TextRotation", DefaultValue = 0.0)]
    // Random up/down offset and tilt per letter
    [DependencyProperty<double>("LetterOffset", DefaultValue = 0.0)]
    [DependencyProperty<double>("LetterRotation", DefaultValue = 0.0)]
    // Brush to use for the text color
    [DependencyProperty<Brush>("TextBrush")]
    // Outline color and thickness
    [DependencyProperty<Brush>("OutlineBrush")]
    [DependencyProperty<double>("OutlineThickness", DefaultValue = 5.0)]
    // Drop shadow color and offset in pixels
    [DependencyProperty<Brush>("ShadowBrush")]
    [DependencyProperty<Vector>("ShadowOffset")]
    // Font decoration forwarded to the font
    [DependencyProperty<double>("FontSize", DefaultValue = 48.0)]
    [DependencyProperty<FontWeight>("FontWeight")]
    [DependencyProperty<FontStyle>("FontStyle")]
    [DependencyProperty<FontStretch>("FontStretch")]
    public partial class OutlinedText : FrameworkElement
    {
        // Rounded arial is close enough to PPT font without having to install a proprietary font
        private static readonly FontFamily Font = new FontFamily("Arial Rounded MT Bold, Segoe UI, Arial");

        public OutlinedText()
        {
            // Trigger re-render on window size change
            SizeChanged += (_, __) => InvalidateVisual();

            // Default color values
            TextBrush = Brushes.White;
            OutlineBrush = MakeBrush("#3A7A1E");
            ShadowBrush = MakeBrush("#66000000");

            // Default shadow offset and font style
            ShadowOffset = new Vector(3, 4);
            FontWeight = FontWeights.Bold;
            FontStyle = FontStyles.Normal;
            FontStretch = FontStretches.Normal;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            // Build the text
            Geometry? text = BuildText(out Size naturalSize);
            if (text == null) return;

            // If scalable is on, scale the text to fit the element and center it
            int transformsPushed = 0;
            if (Scalable && ActualWidth > 0 && ActualHeight > 0 && naturalSize.Width > 0 && naturalSize.Height > 0)
            {
                double scale = Math.Min(ActualWidth / naturalSize.Width, ActualHeight / naturalSize.Height);
                double offsetX = (ActualWidth - naturalSize.Width * scale) / 2.0;
                double offsetY = (ActualHeight - naturalSize.Height * scale) / 2.0;
                drawingContext.PushTransform(new TranslateTransform(offsetX, offsetY));
                drawingContext.PushTransform(new ScaleTransform(scale, scale));
                transformsPushed = 2;
            }

            // Create pen for outline
            double outline = Math.Max(0, OutlineThickness);
            Pen? outlinePen = null;
            if (outline > 0 && OutlineBrush != null)
            {
                outlinePen = new Pen(OutlineBrush, 2 * outline) { LineJoin = PenLineJoin.Round };
            }

            // Draw drop shadow first
            if (ShadowBrush != null && (ShadowOffset.X != 0 || ShadowOffset.Y != 0))
            {
                Pen? shadowPen = null;
                if (outline > 0) shadowPen = new Pen(ShadowBrush, 2 * outline) { LineJoin = PenLineJoin.Round };

                drawingContext.PushTransform(new TranslateTransform(ShadowOffset.X, ShadowOffset.Y));
                drawingContext.DrawGeometry(ShadowBrush, shadowPen, text);
                drawingContext.Pop();
            }

            // Outline behind, text on top
            if (outlinePen != null) drawingContext.DrawGeometry(null, outlinePen, text);
            drawingContext.DrawGeometry(TextBrush, null, text);

            for (int i = 0; i < transformsPushed; i++) drawingContext.Pop();
        }

        /// <summary>
        /// Renders the letters into a geometry group, gives back the size
        /// </summary>
        private Geometry? BuildText(out Size naturalSize)
        {
            // Default values
            naturalSize = new Size(0, 0);
            if (string.IsNullOrEmpty(Text) || FontSize <= 0) return null;

            // Load font and offset ranges
            Typeface typeface = new Typeface(Font, FontStyle, FontWeight, FontStretch);
            double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            double maxOffset = Math.Max(0, LetterOffset);
            double maxTilt = LetterRotation;

            // Randomize using seed from the text
            Random? random = new Random(StableHash(Text));

            // Build the letters one by one
            GeometryGroup letters = new GeometryGroup();
            double cursorX = 0;

            foreach (char character in Text)
            {
                // Build single letter
                FormattedText letter = new FormattedText(
                    character.ToString(),
                    CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight,
                    typeface, FontSize,
                    Brushes.Black,
                    pixelsPerDip
                );

                // Get offsets
                double verticalOffset = 0;
                if (maxOffset > 0) verticalOffset = (random.NextDouble() * 2 - 1) * maxOffset;
                double tilt = 0;
                if (maxTilt != 0) tilt = (random.NextDouble() * 2 - 1) * maxTilt;

                // Create actual geometry to render
                if (!char.IsWhiteSpace(character))
                {
                    Geometry glyph = letter.BuildGeometry(new Point(cursorX, verticalOffset));
                    if (tilt != 0)
                    {
                        // Rotate around the center of the letter
                        Rect box = glyph.Bounds;
                        glyph.Transform = new RotateTransform(tilt, box.X + box.Width / 2, box.Y + box.Height / 2);
                    }

                    // Add to the whole word
                    letters.Children.Add(glyph);
                }

                // Advance the cursor position
                cursorX += letter.WidthIncludingTrailingWhitespace;
            }
            if (letters.Children.Count == 0) return null;

            // Rotate the word with static rotation
            RotateTransform rotation = new RotateTransform(TextRotation);
            letters.Transform = rotation;
            Rect bounds = letters.Bounds;
            if (bounds.IsEmpty) return null;

            // Draw outline
            double outline = Math.Max(0, OutlineThickness);
            double shiftX = outline - bounds.Left - Math.Min(0, ShadowOffset.X);
            double shiftY = outline - bounds.Top - Math.Min(0, ShadowOffset.Y);

            // Render word and shift so that word is 0,0
            TransformGroup placement = new TransformGroup();
            placement.Children.Add(rotation);
            placement.Children.Add(new TranslateTransform(shiftX, shiftY));
            letters.Transform = placement;
            letters.Freeze();

            // Leave room for outline
            naturalSize = new Size(
                bounds.Width + 2 * outline + Math.Abs(ShadowOffset.X),
                bounds.Height + 2 * outline + Math.Abs(ShadowOffset.Y)
            );
            return letters;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            // Scalable asks for nothing so the parent stretches us, then we fit in OnRender
            if (Scalable) return new Size(0, 0);
            BuildText(out Size naturalSize);
            return naturalSize;
        }

        /// <summary>
        /// Hash that stays the same across runs, since string.GetHashCode is randomized per run
        /// </summary>
        private static int StableHash(string text)
        {
            unchecked
            {
                int hash = (int)2166136261;
                foreach (char c in text) hash = (hash ^ c) * 16777619;
                return hash;
            }
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
    }
}

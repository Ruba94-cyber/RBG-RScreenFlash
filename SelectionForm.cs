using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace RScreenFlash
{
    public class SelectionForm : Form
    {
        private Point start;
        private Rectangle selection;
        private Rectangle selectionScreen;
        private Rectangle virtualScreen;
        private bool isSelecting = false;
        private Bitmap magnifierBackdrop;

        public Rectangle SelectedRegion => GetScreenSelection();

        public SelectionForm()
        {
            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Opacity = 0.25;
            this.BackColor = Color.Black;
            this.TopMost = true;
            this.KeyPreview = true;
            this.Cursor = Cursors.Cross;
            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Normal;

            virtualScreen = GetTrueVirtualScreen();
            if (VisualPreferences.SelectionMagnifierEnabled)
            {
                try
                {
                    magnifierBackdrop = new Bitmap(virtualScreen.Width, virtualScreen.Height,
                        PixelFormat.Format32bppArgb);
                    using (Graphics graphics = Graphics.FromImage(magnifierBackdrop))
                        graphics.CopyFromScreen(virtualScreen.Location, Point.Empty, virtualScreen.Size);
                }
                catch (Exception)
                {
                    if (magnifierBackdrop != null)
                    {
                        magnifierBackdrop.Dispose();
                        magnifierBackdrop = null;
                    }
                    // The magnifier is optional and must never prevent selection.
                }
            }

            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(virtualScreen.X, virtualScreen.Y);
            this.Size = new Size(virtualScreen.Width, virtualScreen.Height);

            this.Load += (s, e) =>
            {
                this.Focus();
                this.BringToFront();
                this.Activate();
            };
        }

        private Rectangle GetTrueVirtualScreen()
        {
            return SystemInformation.VirtualScreen;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                start = e.Location;
                selection = new Rectangle(e.Location, Size.Empty);
                selectionScreen = Rectangle.Empty;
                isSelecting = true;
                this.Capture = true;
                Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (isSelecting && (Control.MouseButtons & MouseButtons.Left) == MouseButtons.Left)
            {
                Point currentClient = PointToClient(Cursor.Position);
                UpdateSelectionRects(currentClient);
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            e.Graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

            // Semi-transparent dark background overlay
            using (Brush dark = new SolidBrush(Color.FromArgb(120, 0, 0, 0)))
            {
                e.Graphics.FillRectangle(dark, this.ClientRectangle);
            }

            if (selection.Width > 0 && selection.Height > 0)
            {
                // Transparent selected area (removes the dark overlay)
                using (Brush clear = new SolidBrush(Color.FromArgb(50, 255, 255, 255)))
                {
                    e.Graphics.FillRectangle(clear, selection);
                }

                // Cyan border for better visibility
                using (Pen pen = new Pen(Color.Cyan, 3))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                    e.Graphics.DrawRectangle(pen, selection);
                }

                // Display the selection dimensions
                string dimensions = $"{selection.Width} × {selection.Height}";
                using (Font font = new Font("Segoe UI", 12, FontStyle.Bold))
                using (Brush textBrush = new SolidBrush(Color.White))
                using (Brush backgroundBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                {
                    SizeF textSize = e.Graphics.MeasureString(dimensions, font);
                    Point textPos = new Point(
                        selection.X + selection.Width - (int)textSize.Width - 10,
                        selection.Y - (int)textSize.Height - 5
                    );

                    if (textPos.Y < 0) textPos.Y = selection.Y + 5;
                    if (textPos.X < 0) textPos.X = selection.X + 5;

                    Rectangle textRect = new Rectangle(textPos, Size.Ceiling(textSize));
                    textRect.Inflate(5, 2);

                    e.Graphics.FillRectangle(backgroundBrush, textRect);
                    e.Graphics.DrawString(dimensions, font, textBrush, textPos);
                }
            }
            DrawMagnifier(e.Graphics);
        }

        // Pixel loupe uses a backdrop captured before the selection overlay becomes
        // visible, avoiding recursive screen grabs of the overlay itself.
        private void DrawMagnifier(Graphics graphics)
        {
            if (magnifierBackdrop == null || !ClientRectangle.Contains(PointToClient(Cursor.Position)))
                return;

            Point cursor = PointToClient(Cursor.Position);
            const int sample = 19;
            const int size = 114;
            int sx = Math.Max(0, Math.Min(cursor.X - sample / 2, magnifierBackdrop.Width - sample));
            int sy = Math.Max(0, Math.Min(cursor.Y - sample / 2, magnifierBackdrop.Height - sample));
            int x = Math.Max(4, Math.Min(cursor.X + 20, ClientSize.Width - size - 4));
            int y = Math.Max(4, Math.Min(cursor.Y + 20, ClientSize.Height - size - 4));

            InterpolationMode oldInterpolation = graphics.InterpolationMode;
            PixelOffsetMode oldPixelOffset = graphics.PixelOffsetMode;
            try
            {
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                graphics.PixelOffsetMode = PixelOffsetMode.Half;
                graphics.DrawImage(magnifierBackdrop,
                    new Rectangle(x, y, size, size),
                    new Rectangle(sx, sy, sample, sample),
                    GraphicsUnit.Pixel);
                using (var border = new Pen(Color.Cyan, 2))
                    graphics.DrawRectangle(border, x, y, size, size);
                using (var crosshair = new Pen(Color.FromArgb(190, 255, 255, 255), 1))
                {
                    int centerX = x + size / 2;
                    int centerY = y + size / 2;
                    graphics.DrawLine(crosshair, centerX, y, centerX, y + size);
                    graphics.DrawLine(crosshair, x, centerY, x + size, centerY);
                }
            }
            finally
            {
                graphics.InterpolationMode = oldInterpolation;
                graphics.PixelOffsetMode = oldPixelOffset;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && magnifierBackdrop != null)
            {
                magnifierBackdrop.Dispose();
                magnifierBackdrop = null;
            }
            base.Dispose(disposing);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && isSelecting)
            {
                this.Capture = false;
                isSelecting = false;

                Point releaseClient = PointToClient(Cursor.Position);
                UpdateSelectionRects(releaseClient);

                if (selection.Width > 5 && selection.Height > 5) // Minimum size of 5x5 pixels
                {
                    EnsureSelectionScreen();
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    // Selection too small, reset
                    selection = Rectangle.Empty;
                    selectionScreen = Rectangle.Empty;
                    Invalidate();
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                return true;
            }
            else if (keyData == Keys.Enter && selection.Width > 5 && selection.Height > 5)
            {
                EnsureSelectionScreen();
                this.DialogResult = DialogResult.OK;
                this.Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private Rectangle GetScreenSelection()
        {
            if (selection.Width <= 0 || selection.Height <= 0)
                return Rectangle.Empty;

            if (selectionScreen.IsEmpty)
                EnsureSelectionScreen();

            return selectionScreen;
        }

        private void UpdateSelectionRects(Point currentClient)
        {
            selection = Rectangle.FromLTRB(
                Math.Min(start.X, currentClient.X),
                Math.Min(start.Y, currentClient.Y),
                Math.Max(start.X, currentClient.X),
                Math.Max(start.Y, currentClient.Y));

            if (selection.Width <= 0 || selection.Height <= 0)
            {
                selectionScreen = Rectangle.Empty;
                return;
            }

            Rectangle screenRect = RectangleToScreen(selection);
            screenRect.Intersect(virtualScreen);

            if (screenRect.Width <= 0 || screenRect.Height <= 0)
            {
                selectionScreen = Rectangle.Empty;
                return;
            }

            selectionScreen = screenRect;
        }

        private void EnsureSelectionScreen()
        {
            if (!selectionScreen.IsEmpty)
                return;

            Rectangle screenRect = RectangleToScreen(selection);
            screenRect.Intersect(virtualScreen);

            if (screenRect.Width <= 0 || screenRect.Height <= 0)
            {
                selectionScreen = Rectangle.Empty;
                return;
            }

            selectionScreen = screenRect;
        }
    }
}

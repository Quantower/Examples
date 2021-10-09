using System.Drawing;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Native;
using TradingPlatform.PresentationLayer.Plugins;
using TradingPlatform.PresentationLayer.Renderers;
using System.Linq;

namespace GDIBasedPlugin
{
    public class GDIRenderer : Renderer
    {
        #region Proeprties

        private BufferedGraphic bufferedGraphic;

        public Color Color { get; set; }

        #endregion

        public GDIRenderer(IRenderingNativeControl native)
           : base(native)
        {
            // Subscribe to mouse events
            native.MouseDownNative += this.OnMouseDown;
            native.MouseUpNative += this.OnMouseUp;
            native.MouseMoveNative += this.OnMouseMove;

            //
            this.Color = Color.Black;
            this.bufferedGraphic = new BufferedGraphic(this.Draw, this.Refresh, native.DisposeImage, native.IsDisplayed);
        }

        public void RedrawBufferedGraphic()
        {
            this.bufferedGraphic.IsDirty = true;
        }

        /// <summary>
        /// Implement your painting in this method
        /// </summary>
        protected virtual void Draw(Graphics gr)
        {
            gr.Clear(Color.White);

            // Display a list of first 100 symbols
            Font font = new Font("Arial", 10, FontStyle.Regular);
            Brush brush = new SolidBrush(this.Color);
            var symbols = Core.Instance.Symbols.Take(100);
            int currentY = 10;
            foreach (var symbol in symbols)
            {
                gr.DrawString(symbol.Name, font, brush, 20, currentY);
                gr.DrawString(symbol.SymbolType.ToString(), font, brush, 200, currentY);
                gr.DrawString(symbol.Connection.VendorName.ToString(), font, brush, 400, currentY);

                currentY += 20;
            }
        }

        public override object Render() => bufferedGraphic.CurrentImage;

        public override void Dispose()
        {
            if (this.bufferedGraphic != null)
                this.bufferedGraphic.Dispose();

            base.Dispose();
        }

        public override void OnResize()
        {
            base.OnResize();

            Rectangle bounds = this.Bounds;
            if (bounds.Width == 0 || bounds.Height == 0)
                return;

            //
            try
            {
                //
                // Recreate buffer
                //
                bufferedGraphic.Resize(bounds.Width, bounds.Height);
                bufferedGraphic.IsDirty = true;
            }
            catch { }
        }

        #region Mouse Processing

        private void OnMouseDown(NativeMouseEventArgs e)
        {
            // Add your MouseDown processing logic here
        }

        private void OnMouseMove(NativeMouseEventArgs obj)
        {
            // Add your MouseMove processing logic here
        }

        private void OnMouseUp(NativeMouseEventArgs obj)
        {
            // Add your MouseUp processing logic here
        }

        #endregion
    }
}

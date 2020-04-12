using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace OpenTibiaUnity
{
    public class AsyncGraphics
    {
        Bitmap m_Bitmap;
        Graphics m_Graphics;
        List<Task> m_DrawTasks = new List<Task>();

        // mutex
        object m_DrawLock = new object();

        public AsyncGraphics(Bitmap bitmap) {
            m_Bitmap = bitmap ?? throw new System.ArgumentNullException("Invalid bitmap");
            m_Graphics = Graphics.FromImage(m_Bitmap);
        }
        
        public void DrawImage(Image image, int x, int y) => m_DrawTasks.Add(Task.Run(() => InternalDraw(image, x, y)));
        public void DrawImage(Image image, Point point) => m_DrawTasks.Add(Task.Run(() => InternalDraw(image, point.X, point.Y)));
        public void DrawImage(Image image, int x, int y, int w, int h) => m_DrawTasks.Add(Task.Run(() => InternalDraw(image, x, y, w, h)));
        public void DrawImage(Image image, Rectangle rect) => m_DrawTasks.Add(Task.Run(() => InternalDraw(image, rect.X, rect.Y, rect.Width, rect.Height)));

        public void DrawImages(List<Image> images, List<Point> points) {
            if (images.Count != points.Count)
                throw new System.ArgumentException("Invalid draw call: points.Count != image.Count");

            for (int i = 0; i < images.Count; i++) {
                var image = images[i];
                var point = points[i];

                DrawImage(image, point);
            }
        }

        void InternalDraw(Image image, int x, int y) {
            lock (m_DrawLock) {
                m_Graphics.DrawImage(image, x, y);
                image.Dispose();
            }
        }

        void InternalDraw(Image image, int x, int y, int w, int h) {
            lock (m_DrawLock) {
                m_Graphics.DrawImage(image, x, y, w, h);
                image.Dispose();
            }
        }

        public async Task DisposeOnDone(IEnumerable<Bitmap> bitmaps) {
            await Task.WhenAll(m_DrawTasks);
            foreach (var bitmap in bitmaps) { 
                if (bitmap != null) {
                    bitmap.Dispose();
                }
            }
                
        }

        public async Task SaveAndDispose(string filename) {
            await Task.WhenAll(m_DrawTasks);
            m_Bitmap.Save(filename);
            m_Bitmap.Dispose();
            m_Graphics.Dispose();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Badoucai.Business.Zhaopin
{
    /// <summary>
    /// 图片助手
    /// </summary>
    public class ImageHelper
    {
        private static Bitmap Cut(Image source,int x,int y,int width,int height)
        {
            var pb = new Bitmap(width, height);
            var graphic = Graphics.FromImage(pb);
            graphic.DrawImage(source, 0, 0, new Rectangle(x, y, width, height), GraphicsUnit.Pixel);
            graphic.Dispose();
            return pb;
        }

        private static Bitmap Combine(IReadOnlyList<Bitmap> bitmaps,int width,int height)
        {
            if (bitmaps != null && bitmaps.Count > 0)
            {
                var image = new Bitmap(width, height);

                var graphic = Graphics.FromImage(image);

                int x = 0, y = 0;

                for (var i = 0; i < bitmaps.Count; i++)
                {
                    graphic.DrawImage(bitmaps[i], new Point(x, y));

                    if (i >= 19 && y < 1)
                    {
                        y = 85;
                        x = 0;
                    }
                    else
                        x = x + 14;
                    bitmaps[i].Dispose();
                }

                graphic.Dispose();

                return image;
            }

            return null;
        }

        public static Bitmap GetValidCode_Zhaopin(Image source)
        {
            var bmpList = new List<Bitmap>();
            var pb1 = Cut(source, 140, 0, 14, 85);
            bmpList.Add(pb1);
            var pb2 = Cut(source, 238, 0, 14, 85);
            bmpList.Add(pb2);
            var pb3 = Cut(source, 196, 0, 14, 85);
            bmpList.Add(pb3);
            var pb4 = Cut(source, 112, 0, 14, 85);
            bmpList.Add(pb4);
            var pb5 = Cut(source, 14, 0, 14, 85);
            bmpList.Add(pb5);
            var pb6 = Cut(source, 126, 0, 14, 85);
            bmpList.Add(pb6);
            var pb7 = Cut(source, 56, 0, 14, 85);
            bmpList.Add(pb7);
            var pb8 = Cut(source, 28, 0, 14, 85);
            bmpList.Add(pb8);
            var pb9 = Cut(source, 42, 0, 14, 85);
            bmpList.Add(pb9);
            var pb10 = Cut(source, 168, 0, 14, 85);
            bmpList.Add(pb10);
            var pb11 = Cut(source, 266, 0, 14, 85);
            bmpList.Add(pb11);
            var pb12 = Cut(source, 210, 0, 14, 85);
            bmpList.Add(pb12);
            var pb13 = Cut(source, 154, 0, 14, 85);
            bmpList.Add(pb13);
            var pb14 = Cut(source, 182, 0, 14, 85);
            bmpList.Add(pb14);
            var pb15 = Cut(source, 84, 0, 14, 85);
            bmpList.Add(pb15);
            var pb16 = Cut(source, 0, 0, 14, 85);
            bmpList.Add(pb16);
            var pb17 = Cut(source, 70, 0, 14, 85);
            bmpList.Add(pb17);
            var pb18 = Cut(source, 98, 0, 14, 85);
            bmpList.Add(pb18);
            var pb19 = Cut(source, 224, 0, 14, 85);
            bmpList.Add(pb19);
            var pb20 = Cut(source, 252, 0, 14, 85);
            bmpList.Add(pb20);
            var pb21 = Cut(source, 210, 85, 14, 85);
            bmpList.Add(pb21);
            var pb22 = Cut(source, 84, 85, 14, 85);
            bmpList.Add(pb22);
            var pb23 = Cut(source, 238, 85, 14, 85);
            bmpList.Add(pb23);
            var pb24 = Cut(source, 196, 85, 14, 85);
            bmpList.Add(pb24);
            var pb25 = Cut(source, 28, 85, 14, 85);
            bmpList.Add(pb25);
            var pb26 = Cut(source, 140, 85, 14, 85);
            bmpList.Add(pb26);
            var pb27 = Cut(source, 126, 85, 14, 85);
            bmpList.Add(pb27);
            var pb28 = Cut(source, 182, 85, 14, 85);
            bmpList.Add(pb28);
            var pb29 = Cut(source, 42, 85, 14, 85);
            bmpList.Add(pb29);
            var pb30 = Cut(source, 98, 85, 14, 85);
            bmpList.Add(pb30);
            var pb31 = Cut(source, 56, 85, 14, 85);
            bmpList.Add(pb31);
            var pb32 = Cut(source, 154, 85, 14, 85);
            bmpList.Add(pb32);
            var pb33 = Cut(source, 266, 85, 14, 85);
            bmpList.Add(pb33);
            var pb34 = Cut(source, 168, 85, 14, 85);
            bmpList.Add(pb34);
            var pb35 = Cut(source, 252, 85, 14, 85);
            bmpList.Add(pb35);
            var pb36 = Cut(source, 14, 85, 14, 85);
            bmpList.Add(pb36);
            var pb37 = Cut(source, 0, 85, 14, 85);
            bmpList.Add(pb37);
            var pb38 = Cut(source, 224, 85, 14, 85);
            bmpList.Add(pb38);
            var pb39 = Cut(source, 112, 85, 14, 85);
            bmpList.Add(pb39);
            var pb40 = Cut(source, 70, 85, 14, 85);
            bmpList.Add(pb40);

            return Combine(bmpList,280,130);
        }

        public static Bitmap GetValidCodeSource_Zhaopin(Image source)
        {
            var bmpList = new List<Bitmap>();
            var pbSource1 = Cut(source, 210, 130, 14, 85);
            bmpList.Add(pbSource1);
            var pbSource2 = Cut(source, 84, 130, 14, 85);
            bmpList.Add(pbSource2);
            var pbSource3 = Cut(source, 238, 130, 14, 85);
            bmpList.Add(pbSource3);
            var pbSource4 = Cut(source, 196, 130, 14, 85);
            bmpList.Add(pbSource4);
            var pbSource5 = Cut(source, 28, 130, 14, 85);
            bmpList.Add(pbSource5);
            var pbSource6 = Cut(source, 140, 130, 14, 85);
            bmpList.Add(pbSource6);
            var pbSource7 = Cut(source, 126, 130, 14, 85);
            bmpList.Add(pbSource7);

            return Combine(bmpList, 100, 40);
        }
    }
}

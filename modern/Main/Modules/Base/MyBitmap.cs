using System.Windows.Media;
using MeloongCore;
using PCLCS;

namespace PCL
{

    // 原 LF 类型、转换运算符与实际应用资源／日志边界；图片业务由 C# 实现。
    public class MyBitmap : LegacyBitmap
    {

        private static readonly LegacyBitmapHost BitmapHost = new LegacyBitmapHost(typeof(MyBitmap).Assembly.GetName().Name, Key => (System.Drawing.Bitmap)My.MyWpfExtension.Application.TryFindResource(Key), (ErrorValue, Message) => Logger.Warn(ErrorValue, Message), Application.PrepareWebP);

        public MyBitmap() : base()
        {
        }
        public MyBitmap(string FilePathOrResourceName) : base(FilePathOrResourceName, BitmapHost)
        {
        }
        public MyBitmap(ImageSource Image) : base(Image)
        {
        }
        public MyBitmap(System.Drawing.Image Image) : base(Image)
        {
        }
        public MyBitmap(System.Drawing.Bitmap Image) : base(Image)
        {
        }
        public MyBitmap(ImageBrush Image) : base(Image)
        {
        }

        public static implicit operator MyBitmap(System.Drawing.Image Image)
        {
            if (Image is null)
                return null;
            return new MyBitmap(Image);
        }
        public static implicit operator System.Drawing.Image(MyBitmap Image)
        {
            if (Image is null)
                return null;
            return Image.Pic;
        }
        public static implicit operator MyBitmap(ImageSource Image)
        {
            if (Image is null)
                return null;
            return new MyBitmap(Image);
        }
        public static implicit operator ImageSource(MyBitmap Image)
        {
            if (Image is null)
                return null;
            return Image.ToImageSource();
        }
        public static implicit operator MyBitmap(System.Drawing.Bitmap Image)
        {
            if (Image is null)
                return null;
            return new MyBitmap(Image);
        }
        public static implicit operator System.Drawing.Bitmap(MyBitmap Image)
        {
            if (Image is null)
                return null;
            return Image.Pic;
        }
        public static implicit operator MyBitmap(ImageBrush Image)
        {
            if (Image is null)
                return null;
            return new MyBitmap(Image);
        }
        public static implicit operator ImageBrush(MyBitmap Image)
        {
            if (Image is null)
                return null;
            return Image.ToImageBrush();
        }

        public static new string NormalizeResourceUri(string Value)
        {
            return NormalizeResourceUri(Value, typeof(MyBitmap).Assembly.GetName().Name);
        }
        protected override LegacyBitmap Wrap(System.Drawing.Bitmap Image)
        {
            return new MyBitmap(Image);
        }
        public new MyBitmap Clip(int X, int Y, int Width, int Height)
        {
            return (MyBitmap)base.Clip(X, Y, Width, Height);
        }
        public new MyBitmap RotateFlip(System.Drawing.RotateFlipType Type)
        {
            return (MyBitmap)base.RotateFlip(Type);
        }
        public new void Save(string FilePath)
        {
            base.Save(FilePath);
        }
    }
}

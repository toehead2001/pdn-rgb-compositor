using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace RGBCompositorPlugin
{
    public class PluginSupportInfo : IPluginSupportInfo
    {
        public string Author => base.GetType().Assembly.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;
        public string Copyright => base.GetType().Assembly.GetCustomAttribute<AssemblyDescriptionAttribute>().Description;
        public string DisplayName => base.GetType().Assembly.GetCustomAttribute<AssemblyProductAttribute>().Product;
        public Version Version => base.GetType().Assembly.GetName().Version;
        public Uri WebsiteUri => new Uri("https://forums.getpaint.net/index.php?showtopic=113646");
    }

    [PluginSupportInfo(typeof(PluginSupportInfo), DisplayName = "RGB Compositor")]
    public class RGBCompositor : PropertyBasedEffect
    {
        private Surface rSurface;
        private Surface gSurface;
        private Surface bSurface;

        private static readonly Bitmap StaticIcon = new Bitmap(typeof(RGBCompositor), "RGBCompositor.png");

        public RGBCompositor()
          : base("RGB Compositor", StaticIcon, "Color", new EffectOptions() { Flags = EffectFlags.Configurable })
        {
        }

        private enum PropertyNames
        {
            RedFile,
            GreenFile,
            BlueFile
        }

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> props = new List<Property>
            {
                new StringProperty(PropertyNames.RedFile, string.Empty),
                new StringProperty(PropertyNames.GreenFile, string.Empty),
                new StringProperty(PropertyNames.BlueFile, string.Empty),
            };

            return new PropertyCollection(props);
        }

        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            configUI.SetPropertyControlValue(PropertyNames.RedFile, ControlInfoPropertyNames.DisplayName, "Red File");
            configUI.SetPropertyControlValue(PropertyNames.RedFile, ControlInfoPropertyNames.FileTypes, new string[] { "bmp", "gif", "jpg", "jpeg", "png" });
            configUI.SetPropertyControlType(PropertyNames.RedFile, PropertyControlType.FileChooser);

            configUI.SetPropertyControlValue(PropertyNames.GreenFile, ControlInfoPropertyNames.DisplayName, "Green File");
            configUI.SetPropertyControlValue(PropertyNames.GreenFile, ControlInfoPropertyNames.FileTypes, new string[] { "bmp", "gif", "jpg", "jpeg", "png" });
            configUI.SetPropertyControlType(PropertyNames.GreenFile, PropertyControlType.FileChooser);

            configUI.SetPropertyControlValue(PropertyNames.BlueFile, ControlInfoPropertyNames.DisplayName, "Blue File");
            configUI.SetPropertyControlValue(PropertyNames.BlueFile, ControlInfoPropertyNames.FileTypes, new string[] { "bmp", "gif", "jpg", "jpeg", "png" });
            configUI.SetPropertyControlType(PropertyNames.BlueFile, PropertyControlType.FileChooser);

            return configUI;
        }

        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken newToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            string rPath = newToken.GetProperty<StringProperty>(PropertyNames.RedFile).Value;
            string gPath = newToken.GetProperty<StringProperty>(PropertyNames.GreenFile).Value;
            string bPath = newToken.GetProperty<StringProperty>(PropertyNames.BlueFile).Value;

            Bitmap rImage = null;
            if (File.Exists(rPath))
            {
                try
                {
                    rImage = new Bitmap(rPath);
                }
                catch
                {
                }
            }

            if (this.rSurface != null)
            {
                this.rSurface.Dispose();
                this.rSurface = null;
            }

            if (rImage != null && rImage?.Size == srcArgs.Size)
            {
                this.rSurface = Surface.CopyFromBitmap(rImage);
            }

            rImage?.Dispose();

            Bitmap gImage = null;
            if (File.Exists(gPath))
            {
                try
                {
                    gImage = new Bitmap(gPath);
                }
                catch
                {
                }
            }

            if (this.gSurface != null)
            {
                this.gSurface.Dispose();
                this.gSurface = null;
            }

            if (gImage != null && gImage.Size == srcArgs.Size)
            {
                this.gSurface = Surface.CopyFromBitmap(gImage);
            }

            gImage?.Dispose();

            Bitmap bImage = null;
            if (File.Exists(bPath))
            {
                try
                {
                    bImage = new Bitmap(bPath);
                }
                catch
                {
                }
            }

            if (this.bSurface != null)
            {
                this.bSurface.Dispose();
                this.bSurface = null;
            }

            if (bImage != null && bImage.Size == srcArgs.Size)
            {
                this.bSurface = Surface.CopyFromBitmap(bImage);
            }

            bImage?.Dispose();

            base.OnSetRenderInfo(newToken, dstArgs, srcArgs);
        }

        protected override void OnRender(Rectangle[] renderRects, int startIndex, int length)
        {
            if (length == 0) return;
            for (int i = startIndex; i < startIndex + length; ++i)
            {
                Render(DstArgs.Surface, renderRects[i]);
            }
        }

        private void Render(Surface dst, Rectangle rect)
        {
            bool rLoaded = this.rSurface != null;
            bool gLoaded = this.gSurface != null;
            bool bLoaded = this.bSurface != null;

            if (!rLoaded && !gLoaded && !bLoaded)
            {
                dst.Clear(rect, ColorBgra.Transparent);
                return;
            }

            for (int y = rect.Top; y < rect.Bottom; y++)
            {
                if (IsCancelRequested) return;
                for (int x = rect.Left; x < rect.Right; x++)
                {
                    dst[x, y] = ColorBgra.FromBgra(
                        bLoaded ? this.bSurface[x, y].B : byte.MinValue,
                        gLoaded ? this.gSurface[x, y].G : byte.MinValue,
                        rLoaded ? this.rSurface[x, y].R : byte.MinValue,
                        byte.MaxValue);
                }
            }
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                this.rSurface?.Dispose();
                this.gSurface?.Dispose();
                this.bSurface?.Dispose();
            }

            base.OnDispose(disposing);
        }
    }
}

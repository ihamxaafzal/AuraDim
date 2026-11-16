$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$assetsDir = Join-Path $root "src\assets"
New-Item -ItemType Directory -Force -Path $assetsDir | Out-Null
$icoPath = Join-Path $assetsDir "auradim.ico"

$code = @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

public static class IconBuilder
{
    public static void Build(string outputPath)
    {
        int[] sizes = new int[] { 16, 32, 48, 64, 128, 256 };
        var pngBlobs = new List<byte[]>();

        foreach (int s in sizes)
        {
            using (var bmp = RenderLogo(s))
            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Png);
                pngBlobs.Add(ms.ToArray());
            }
        }

        using (var fs = File.Create(outputPath))
        using (var bw = new BinaryWriter(fs))
        {
            // ICONDIR header
            bw.Write((ushort)0); // reserved
            bw.Write((ushort)1); // type = 1 (icon)
            bw.Write((ushort)sizes.Length);

            int offset = 6 + (16 * sizes.Length);
            for (int i = 0; i < sizes.Length; i++)
            {
                int sz = sizes[i];
                byte w = (sz >= 256) ? (byte)0 : (byte)sz;
                byte h = (sz >= 256) ? (byte)0 : (byte)sz;
                byte[] data = pngBlobs[i];

                bw.Write(w);
                bw.Write(h);
                bw.Write((byte)0);     // color count
                bw.Write((byte)0);     // reserved
                bw.Write((ushort)1);   // planes
                bw.Write((ushort)32);  // bpp
                bw.Write((uint)data.Length);
                bw.Write((uint)offset);
                offset += data.Length;
            }

            foreach (var data in pngBlobs)
            {
                bw.Write(data);
            }
        }
    }

    private static Bitmap RenderLogo(int size)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);

            float pad = size * 0.06f;
            float box = size - pad * 2f;
            float radius = size * 0.24f;

            // Squircle backdrop
            var rect = new RectangleF(pad, pad, box, box);
            using (var path = RoundedRect(rect, radius))
            using (var bg = new LinearGradientBrush(
                new PointF(pad, pad),
                new PointF(size - pad, size - pad),
                Color.FromArgb(14, 165, 233),
                Color.FromArgb(79, 70, 229)))
            {
                g.FillPath(bg, path);

                // Subtle top-left highlight rim
                using (var rim = new Pen(Color.FromArgb(110, 255, 255, 255), Math.Max(1f, size * 0.03f)))
                {
                    g.DrawPath(rim, path);
                }
            }

            // Inner dark depth circle for high contrast
            float innerPad = size * 0.16f;
            float innerBox = size - innerPad * 2f;
            using (var innerBrush = new SolidBrush(Color.FromArgb(48, 7, 11, 25)))
            {
                g.FillEllipse(innerBrush, innerPad, innerPad, innerBox, innerBox);
            }

            // Monitor silhouette + radiant sun core
            float cx = size * 0.5f;
            float cy = size * 0.46f;
            float sunR = size * 0.145f;

            // Sun rays
            float rayInner = size * 0.21f;
            float rayOuter = size * 0.29f;
            using (var rayPen = new Pen(Color.White, Math.Max(1.3f, size * 0.058f))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            })
            {
                for (int i = 0; i < 8; i++)
                {
                    double ang = i * Math.PI / 4.0;
                    float x1 = cx + (float)(Math.Cos(ang) * rayInner);
                    float y1 = cy + (float)(Math.Sin(ang) * rayInner);
                    float x2 = cx + (float)(Math.Cos(ang) * rayOuter);
                    float y2 = cy + (float)(Math.Sin(ang) * rayOuter);
                    g.DrawLine(rayPen, x1, y1, x2, y2);
                }
            }

            // Sun center disc
            using (var sunBrush = new SolidBrush(Color.White))
            {
                g.FillEllipse(sunBrush, cx - sunR, cy - sunR, sunR * 2f, sunR * 2f);
            }

            // Sleek monitor stand bar underneath
            float barW = size * 0.42f;
            float barH = Math.Max(1.5f, size * 0.075f);
            float barY = size * 0.785f;
            using (var barPath = RoundedRect(new RectangleF(cx - barW / 2f, barY, barW, barH), barH / 2f))
            using (var barBrush = new SolidBrush(Color.FromArgb(235, 255, 255, 255)))
            {
                g.FillPath(barBrush, barPath);
            }
        }
        return bmp;
    }

    private static GraphicsPath RoundedRect(RectangleF bounds, float radius)
    {
        float d = radius * 2f;
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
'@

Add-Type -TypeDefinition $code -ReferencedAssemblies System.Drawing
[IconBuilder]::Build($icoPath)
Write-Host "Generated icon at: $icoPath"
Get-Item $icoPath | Select-Object FullName, Length

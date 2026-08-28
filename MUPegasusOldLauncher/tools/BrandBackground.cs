using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

class BrandBackground
{
    static void Main(string[] args)
    {
        string root = args.Length > 0 ? args[0] : @"c:\Users\JUNIOR-DEV\Downloads\MuServer-20260803T155926Z-1-001";
        string bgPath = Path.Combine(root, @"MUPegasusOldLauncher\Launcher\Resources\background.png");
        string logoPath = Path.Combine(root, "logo-mu.png");
        string logoHeader = Path.Combine(root, @"MorpheusWeb_SuporteS21(2)\templates\unique\assets\images\logo-header.png");
        if (!File.Exists(logoPath) && File.Exists(logoHeader)) logoPath = logoHeader;

        string backup = Path.Combine(root, @"MUPegasusOldLauncher\Launcher\Resources\background_elev8_backup.png");
        if (!File.Exists(backup))
            File.Copy(bgPath, backup, false);

        using (var bg = new Bitmap(Image.FromFile(backup)))
        using (var logo = Image.FromFile(logoPath))
        using (var g = Graphics.FromImage(bg))
        {
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // Cobre a marca Elev8 no canto superior esquerdo
            int coverW = 210;
            int coverH = 95;
            using (var brush = new SolidBrush(Color.FromArgb(255, 12, 14, 28)))
            {
                g.FillRectangle(brush, 8, 8, coverW, coverH);
            }

            // Logo Mu Eldryn no lugar
            int logoW = 190;
            int logoH = (int)(logo.Height * (logoW / (double)logo.Width));
            if (logoH > 88) { logoH = 88; logoW = (int)(logo.Width * (logoH / (double)logo.Height)); }
            int lx = 18;
            int ly = 12;
            g.DrawImage(logo, new Rectangle(lx, ly, logoW, logoH));

            bg.Save(bgPath, ImageFormat.Png);
            Console.WriteLine("OK branded background -> " + bgPath);
            Console.WriteLine("logo " + logoW + "x" + logoH + " at " + lx + "," + ly);
        }

        // também copia ico se existir
        string icoSrc = Path.Combine(root, @"MorpheusWeb_SuporteS21(2)\templates\unique\assets\images\logo.png");
        Console.WriteLine("done");
    }
}

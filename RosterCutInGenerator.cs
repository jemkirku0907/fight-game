using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace FightingGame
{
    public static class RosterCutInGenerator
    {
        public static void EnsureAllRosterCutIns(string? explicitSpritesDir = null)
        {
            var characters = new (string Name, string PreferredSrc, int FrameCount, int UseFrame, Color ThemeColor, Color SpeedLineColor, float EyeXRatio, float EyeYRatio)[]
            {
                ("Disciple", "special.png", 4, 0, Color.DarkViolet, Color.FromArgb(200, 140, 255), 0.50f, 0.28f),
                ("Mage",     "idle.png",    8, 0, Color.Crimson,    Color.FromArgb(255, 120, 30),  0.50f, 0.35f),
                ("Gunner",   "special.png", 4, 0, Color.Gold,       Color.FromArgb(255, 215, 60),  0.55f, 0.34f),
                ("Ninja",    "idle.png",    3, 0, Color.Cyan,       Color.FromArgb(50, 230, 255),  0.50f, 0.35f),
                ("Warrior",  "idle.png",    8, 7, Color.OrangeRed,  Color.FromArgb(255, 80, 50),   0.55f, 0.32f)
            };

            var targetDirs = new List<string>();
            string projectSprites = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Sprites"));
            if (Directory.Exists(projectSprites)) targetDirs.Add(projectSprites);

            string appSprites = Path.Combine(AppContext.BaseDirectory, "Sprites");
            if (Directory.Exists(appSprites) && !targetDirs.Contains(appSprites)) targetDirs.Add(appSprites);

            if (explicitSpritesDir != null && Directory.Exists(explicitSpritesDir) && !targetDirs.Contains(explicitSpritesDir))
                targetDirs.Add(explicitSpritesDir);

            foreach (var spritesDir in targetDirs)
            {
                foreach (var ch in characters)
                {
                    string charDir = Path.Combine(spritesDir, ch.Name);
                    if (!Directory.Exists(charDir)) continue;

                    string cutinPath = Path.Combine(charDir, "cutin.png");
                    string portraitPath = Path.Combine(charDir, "portrait.png");

                    if (!File.Exists(cutinPath) || !File.Exists(portraitPath))
                    {
                        GenerateCharCutIn(charDir, ch.Name, ch.PreferredSrc, ch.FrameCount, ch.UseFrame, ch.ThemeColor, ch.SpeedLineColor, ch.EyeXRatio, ch.EyeYRatio);
                    }
                }
            }
        }

        private static void GenerateCharCutIn(string dir, string name, string preferredSrc, int frameCount, int useFrame, Color theme, Color speedLineCol, float eyeXRatio, float eyeYRatio)
        {
            string cutinPath = Path.Combine(dir, "cutin.png");
            string portraitPath = Path.Combine(dir, "portrait.png");

            string srcPath = Path.Combine(dir, preferredSrc);
            if (!File.Exists(srcPath)) srcPath = Path.Combine(dir, "idle.png");
            if (!File.Exists(srcPath)) return;

            using var srcStrip = new Bitmap(srcPath);

            int fw = Math.Max(1, srcStrip.Width / frameCount);
            int fh = srcStrip.Height;
            int frameOffset = Math.Clamp(useFrame, 0, frameCount - 1) * fw;

            int minX = fw, maxX = 0, minY = fh, maxY = 0;
            for (int y = 0; y < fh; y++)
            {
                for (int x = 0; x < fw; x++)
                {
                    if (srcStrip.GetPixel(frameOffset + x, y).A > 20)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            if (minX > maxX || minY > maxY)
            {
                minX = 0; maxX = fw - 1; minY = 0; maxY = fh - 1;
            }

            int charW = Math.Max(1, maxX - minX + 1);
            int charH = Math.Max(1, maxY - minY + 1);

            // 1. Generate 130x165 portrait focusing on the head & upper torso
            const int portW = 130, portH = 165;
            using (var portraitBmp = new Bitmap(portW, portH, PixelFormat.Format32bppArgb))
            {
                using (var gp = Graphics.FromImage(portraitBmp))
                {
                    gp.Clear(Color.Transparent);
                    gp.InterpolationMode = InterpolationMode.NearestNeighbor;
                    gp.PixelOffsetMode = PixelOffsetMode.Half;

                    // Crop upper 65% of character (head & chest)
                    int cropX = Math.Max(0, minX - 3);
                    int cropY = Math.Max(0, minY - 2);
                    int cropW = Math.Min(fw - cropX, charW + 6);
                    int cropH = Math.Min(fh - cropY, Math.Max(16, (int)(charH * 0.68f)));

                    float scale = Math.Min((portW * 0.88f) / cropW, (portH * 0.88f) / cropH);
                    int dw = (int)(cropW * scale);
                    int dh = (int)(cropH * scale);
                    int dx = (portW - dw) / 2;
                    int dy = (portH - dh) / 2;

                    gp.DrawImage(srcStrip, new Rectangle(dx, dy, dw, dh), new Rectangle(frameOffset + cropX, cropY, cropW, cropH), GraphicsUnit.Pixel);
                }
                portraitBmp.Save(portraitPath, ImageFormat.Png);
            }

            // 2. Generate 6-frame Tekken-style Rage Art cut-in strip (1320x220)
            const int cutW = 220, cutH = 220, count = 6;
            using (var cutinBmp = new Bitmap(cutW * count, cutH, PixelFormat.Format32bppArgb))
            {
                using (var gCut = Graphics.FromImage(cutinBmp))
                {
                    gCut.Clear(Color.Transparent);
                    gCut.PixelOffsetMode = PixelOffsetMode.Half;

                    using var portImg = Image.FromFile(portraitPath);

                    for (int f = 0; f < count; f++)
                    {
                        int ox = f * cutW;

                        // Progressive dramatic zoom
                        float zoom = 1.05f + f * 0.11f;
                        int dw = (int)(cutW * zoom);
                        int dh = (int)(cutH * zoom);
                        int dx = ox + (cutW - dw) / 2;
                        int dy = (cutH - dh) / 2;

                        gCut.InterpolationMode = InterpolationMode.NearestNeighbor;
                        gCut.DrawImage(portImg, new Rectangle(dx, dy, dw, dh));

                        // Dynamic angled speedlines
                        gCut.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        using var linePen = new Pen(Color.FromArgb(130 + f * 20, speedLineCol), 2.5f);
                        for (int s = 0; s < 5; s++)
                        {
                            float sx = ox + 18f + s * 42f;
                            gCut.DrawLine(linePen, sx, 0, sx + 28f, cutH);
                        }

                        // Angled targeting bounding box (yellow bracket)
                        using var boxPen = new Pen(Color.Gold, 2.5f);
                        float bx = ox + 15f;
                        float by = 15f;
                        float bw = cutW - 30f;
                        float bh = cutH - 30f;
                        gCut.DrawRectangle(boxPen, bx, by, bw, bh);

                        // Tekken Rage Art targeting crosshair / eye flare (frames 2 to 5)
                        if (f >= 2)
                        {
                            float eyeX = ox + cutW * 0.50f;
                            float eyeY = cutH * 0.38f - (f * 1.5f);
                            float flareSize = 6f + (f - 2) * 5.5f;

                            using var flareGleam = new SolidBrush(Color.FromArgb(240, Color.Gold));
                            using var flareCore = new SolidBrush(Color.White);
                            using var targetPen = new Pen(Color.OrangeRed, 2f);

                            // Crosshair circle & spikes
                            gCut.DrawEllipse(targetPen, eyeX - flareSize * 0.7f, eyeY - flareSize * 0.7f, flareSize * 1.4f, flareSize * 1.4f);
                            gCut.FillRectangle(flareGleam, eyeX - flareSize, eyeY - 2f, flareSize * 2f, 4f);
                            gCut.FillRectangle(flareGleam, eyeX - 2f, eyeY - flareSize, 4f, flareSize * 2f);
                            gCut.FillRectangle(flareCore, eyeX - 2.5f, eyeY - 2.5f, 5f, 5f);
                        }
                    }
                }

                cutinBmp.Save(cutinPath, ImageFormat.Png);
            }
        }
    }
}

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace FightingGame
{
    public static class BallerSpriteGenerator
    {
        private static readonly Color SkinColor = Color.FromArgb(215, 150, 105);
        private static readonly Color HairColor = Color.FromArgb(22, 18, 18);
        private static readonly Color JerseyRed = Color.FromArgb(195, 25, 25);
        private static readonly Color JerseyGold = Color.FromArgb(245, 210, 40);
        private static readonly Color ShortsRed = Color.FromArgb(170, 20, 20);
        private static readonly Color ShoeWhite = Color.FromArgb(240, 240, 245);
        private static readonly Color BallOrange = Color.FromArgb(245, 115, 20);
        private static readonly Color SeamBlack = Color.FromArgb(30, 25, 25);

        public static void EnsureSprites()
        {
            try
            {
                string baseDir = AppContext.BaseDirectory;
                string[] targetDirs = new[]
                {
                    Path.Combine(baseDir, "Sprites", "Baller"),
                    Path.Combine(Directory.GetCurrentDirectory(), "Sprites", "Baller")
                };

                foreach (var dir in targetDirs)
                {
                    Directory.CreateDirectory(dir);

                    GenerateStrip(Path.Combine(dir, "idle.png"), 4, DrawIdle);
                    GenerateStrip(Path.Combine(dir, "walk.png"), 4, DrawWalk);
                    GenerateStrip(Path.Combine(dir, "jump.png"), 1, DrawJump);
                    GenerateStrip(Path.Combine(dir, "attack.png"), 4, DrawAttack);
                    GenerateStrip(Path.Combine(dir, "special.png"), 6, DrawSpecial);
                    GenerateStrip(Path.Combine(dir, "block.png"), 2, DrawBlock);
                    GenerateStrip(Path.Combine(dir, "hit.png"), 1, DrawHit);
                    GenerateStrip(Path.Combine(dir, "ko.png"), 2, DrawKo);

                    GeneratePixelPortraitAndCutIn(dir);
                }

                // Ensure sound file
                string[] soundDirs = new[]
                {
                    Path.Combine(baseDir, "Sounds", "Special"),
                    Path.Combine(Directory.GetCurrentDirectory(), "Sounds", "Special")
                };

                foreach (var sDir in soundDirs)
                {
                    Directory.CreateDirectory(sDir);
                    string targetWav = Path.Combine(sDir, "Baller.wav");
                    if (!File.Exists(targetWav))
                    {
                        string gunnerWav = Path.Combine(sDir, "Gunner.wav");
                        string mageWav = Path.Combine(sDir, "Mage.wav");
                        if (File.Exists(gunnerWav)) File.Copy(gunnerWav, targetWav, true);
                        else if (File.Exists(mageWav)) File.Copy(mageWav, targetWav, true);
                    }
                }
            }
            catch { }
        }

        private static void GenerateStrip(string path, int frameCount, Action<Graphics, int, int, int> drawFrame)
        {
            if (File.Exists(path)) return; // already generated

            const int fw = 50, fh = 55;
            using var bmp = new Bitmap(fw * frameCount, fh);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            for (int f = 0; f < frameCount; f++)
            {
                var state = g.Save();
                g.TranslateTransform(f * fw, 0);
                drawFrame(g, f, fw, fh);
                g.Restore(state);
            }

            bmp.Save(path, ImageFormat.Png);
        }

        private static void DrawBall(Graphics g, float cx, float cy, float r)
        {
            using var ballBrush = new SolidBrush(BallOrange);
            using var seamPen = new Pen(SeamBlack, 1.2f);
            using var hlBrush = new SolidBrush(Color.FromArgb(160, Color.White));

            g.FillEllipse(ballBrush, cx - r, cy - r, r * 2f, r * 2f);
            g.DrawLine(seamPen, cx - r + 1f, cy, cx + r - 1f, cy);
            g.DrawLine(seamPen, cx, cy - r + 1f, cx, cy + r - 1f);
            g.DrawArc(seamPen, cx - r * 0.7f, cy - r, r * 1.4f, r * 2f, -70, 140);
            g.FillEllipse(hlBrush, cx - r * 0.45f, cy - r * 0.45f, r * 0.4f, r * 0.4f);
        }

        private static void DrawBasePlayer(Graphics g, float x, float y, float bob)
        {
            using var skin = new SolidBrush(SkinColor);
            using var hair = new SolidBrush(HairColor);
            using var jersey = new SolidBrush(JerseyRed);
            using var gold = new SolidBrush(JerseyGold);
            using var shorts = new SolidBrush(ShortsRed);
            using var eye = new SolidBrush(Color.Black);
            using var goldPen = new Pen(JerseyGold, 1.5f);
            using var trimPen = new Pen(JerseyGold, 1f);
            using var numFont = new Font("Segoe UI Black", 5.5f, FontStyle.Bold);

            float hy = y + bob;
            // Head & hair
            g.FillEllipse(skin, x - 5.5f, hy - 19f, 11f, 13f);
            g.FillEllipse(hair, x - 6f, hy - 21f, 12f, 7f);
            // Eye
            g.FillRectangle(eye, x + 1.5f, hy - 14f, 1.5f, 1.5f);

            // Jersey Torso (#2)
            g.FillRectangle(jersey, x - 6.5f, hy - 6f, 13f, 16f);
            g.DrawLine(goldPen, x - 4.5f, hy - 6f, x, hy - 2f);
            g.DrawLine(goldPen, x + 4.5f, hy - 6f, x, hy - 2f);
            g.DrawString("2", numFont, gold, x - 3.5f, hy - 4f);

            // Shorts
            g.FillRectangle(shorts, x - 7f, hy + 10f, 14f, 11f);
            g.DrawLine(trimPen, x - 7f, hy + 20.5f, x + 7f, hy + 20.5f);
        }

        private static void DrawIdle(Graphics g, int f, int w, int h)
        {
            float cx = w * 0.48f;
            float cy = h * 0.48f;
            float bob = (f % 2 == 1) ? -1.5f : 0f;
            DrawBasePlayer(g, cx, cy, bob);

            using var skin = new SolidBrush(SkinColor);
            using var shoes = new SolidBrush(ShoeWhite);
            using var armPen = new Pen(SkinColor, 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round };

            g.FillRectangle(skin, cx - 5.5f, cy + bob + 21f, 3.5f, 13f);
            g.FillRectangle(skin, cx + 2f, cy + bob + 21f, 3.5f, 13f);
            g.FillRectangle(shoes, cx - 6f, cy + bob + 34f, 5.5f, 4f);
            g.FillRectangle(shoes, cx + 1.5f, cy + bob + 34f, 5.5f, 4f);

            float ballY = (f % 2 == 0) ? cy + bob + 16f : cy + bob + 24f;
            g.DrawLine(armPen, cx - 4f, cy + bob - 3f, cx + 8f, ballY - 4f);
            DrawBall(g, cx + 9f, ballY, 6f);
        }

        private static void DrawWalk(Graphics g, int f, int w, int h)
        {
            float cx = w * 0.48f;
            float cy = h * 0.48f;
            float bob = MathF.Sin(f * MathF.PI / 2f) * 2f;
            DrawBasePlayer(g, cx, cy, bob);

            using var skin = new SolidBrush(SkinColor);
            using var shoes = new SolidBrush(ShoeWhite);
            using var armPen = new Pen(SkinColor, 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round };

            float legSwing = (f % 2 == 0) ? 6f : -6f;
            g.FillRectangle(skin, cx - 5f + legSwing, cy + bob + 21f, 3.5f, 13f);
            g.FillRectangle(skin, cx + 2f - legSwing, cy + bob + 21f, 3.5f, 13f);
            g.FillRectangle(shoes, cx - 5.5f + legSwing, cy + bob + 34f, 5.5f, 4f);
            g.FillRectangle(shoes, cx + 1.5f - legSwing, cy + bob + 34f, 5.5f, 4f);

            float ballY = cy + bob + (f % 2 == 0 ? 14f : 24f);
            g.DrawLine(armPen, cx + 2f, cy + bob - 2f, cx + 10f, ballY - 4f);
            DrawBall(g, cx + 11f, ballY, 5.5f);
        }

        private static void DrawJump(Graphics g, int f, int w, int h)
        {
            float cx = w * 0.48f;
            float cy = h * 0.40f;
            DrawBasePlayer(g, cx, cy, 0f);

            using var skin = new SolidBrush(SkinColor);
            using var shoes = new SolidBrush(ShoeWhite);
            using var armPen = new Pen(SkinColor, 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round };

            g.FillRectangle(skin, cx - 6f, cy + 20f, 3.5f, 10f);
            g.FillRectangle(skin, cx + 2.5f, cy + 18f, 3.5f, 11f);
            g.FillRectangle(shoes, cx - 6.5f, cy + 30f, 5f, 4f);
            g.FillRectangle(shoes, cx + 2f, cy + 29f, 5f, 4f);

            g.DrawLine(armPen, cx + 3f, cy - 4f, cx + 7f, cy - 18f);
            DrawBall(g, cx + 9f, cy - 23f, 6f);
        }

        private static void DrawAttack(Graphics g, int f, int w, int h)
        {
            float cx = w * 0.44f;
            float cy = h * 0.48f;
            DrawBasePlayer(g, cx, cy, 0f);

            using var skin = new SolidBrush(SkinColor);
            using var shoes = new SolidBrush(ShoeWhite);
            using var armPen = new Pen(SkinColor, 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round };

            g.FillRectangle(skin, cx - 7f, cy + 21f, 3.5f, 13f);
            g.FillRectangle(skin, cx + 4f, cy + 21f, 3.5f, 13f);
            g.FillRectangle(shoes, cx - 7.5f, cy + 34f, 5.5f, 4f);
            g.FillRectangle(shoes, cx + 3.5f, cy + 34f, 5.5f, 4f);

            if (f == 0)
            {
                g.DrawLine(armPen, cx - 3f, cy - 4f, cx - 11f, cy - 12f);
                DrawBall(g, cx - 14f, cy - 14f, 6f);
            }
            else if (f == 1)
            {
                g.DrawLine(armPen, cx + 1f, cy - 4f, cx + 3f, cy - 18f);
                DrawBall(g, cx + 4f, cy - 22f, 6.2f);
            }
            else if (f == 2)
            {
                g.DrawLine(armPen, cx + 4f, cy - 4f, cx + 16f, cy - 2f);
                DrawBall(g, cx + 21f, cy - 2f, 6f);
            }
            else
            {
                g.DrawLine(armPen, cx + 4f, cy - 4f, cx + 17f, cy + 2f);
            }
        }

        private static void DrawSpecial(Graphics g, int f, int w, int h)
        {
            float cx = w * 0.48f;
            float cy = h * 0.45f;
            float airY = (f == 2 || f == 3) ? -12f : (f == 1 || f == 4) ? -6f : 0f;

            using var aura = new SolidBrush(Color.FromArgb(90, Color.Gold));
            g.FillEllipse(aura, cx - 16f, cy + airY - 26f, 32f, 52f);

            DrawBasePlayer(g, cx, cy, airY);

            using var skin = new SolidBrush(SkinColor);
            using var shoes = new SolidBrush(ShoeWhite);
            using var armPen = new Pen(SkinColor, 3.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            using var hoopPen = new Pen(Color.FromArgb(220, Color.OrangeRed), 2.5f);

            if (f <= 2)
            {
                g.FillRectangle(skin, cx - 6f, cy + airY + 20f, 3.5f, 10f);
                g.FillRectangle(skin, cx + 3f, cy + airY + 18f, 3.5f, 11f);
                g.FillRectangle(shoes, cx - 6.5f, cy + airY + 30f, 5f, 4f);
                g.FillRectangle(shoes, cx + 2.5f, cy + airY + 29f, 5f, 4f);

                g.DrawLine(armPen, cx - 2f, cy + airY - 5f, cx + 5f, cy + airY - 22f);
                DrawBall(g, cx + 6f, cy + airY - 26f, 7f);
            }
            else if (f == 3)
            {
                g.FillRectangle(skin, cx - 7f, cy + airY + 19f, 3.5f, 11f);
                g.FillRectangle(skin, cx + 2f, cy + airY + 19f, 3.5f, 11f);
                g.FillRectangle(shoes, cx - 7.5f, cy + airY + 30f, 5f, 4f);
                g.FillRectangle(shoes, cx + 1.5f, cy + airY + 30f, 5f, 4f);

                g.DrawLine(armPen, cx - 1f, cy + airY - 5f, cx - 6f, cy + airY - 24f);
                DrawBall(g, cx - 8f, cy + airY - 28f, 7.5f);
            }
            else if (f == 4)
            {
                g.FillRectangle(skin, cx - 5f, cy + airY + 21f, 3.5f, 12f);
                g.FillRectangle(skin, cx + 3f, cy + airY + 21f, 3.5f, 12f);
                g.FillRectangle(shoes, cx - 5.5f, cy + airY + 33f, 5f, 4f);
                g.FillRectangle(shoes, cx + 2.5f, cy + airY + 33f, 5f, 4f);

                g.DrawLine(armPen, cx + 2f, cy + airY - 4f, cx + 12f, cy + airY + 6f);
                DrawBall(g, cx + 15f, cy + airY + 8f, 7f);
                g.DrawLine(hoopPen, cx + 6f, cy + airY + 7f, cx + 24f, cy + airY + 7f);
            }
            else
            {
                g.FillRectangle(skin, cx - 7f, cy + 22f, 3.5f, 12f);
                g.FillRectangle(skin, cx + 5f, cy + 22f, 3.5f, 12f);
                g.FillRectangle(shoes, cx - 7.5f, cy + 34f, 5.5f, 4f);
                g.FillRectangle(shoes, cx + 4.5f, cy + 34f, 5.5f, 4f);

                g.DrawLine(armPen, cx - 4f, cy - 5f, cx - 11f, cy - 14f);
                g.DrawLine(armPen, cx + 4f, cy - 5f, cx + 11f, cy - 14f);
            }
        }

        private static void DrawBlock(Graphics g, int f, int w, int h)
        {
            float cx = w * 0.48f;
            float cy = h * 0.50f;
            DrawBasePlayer(g, cx, cy, 0f);

            using var skin = new SolidBrush(SkinColor);
            using var shoes = new SolidBrush(ShoeWhite);
            using var armPen = new Pen(SkinColor, 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round };

            g.FillRectangle(skin, cx - 9f, cy + 21f, 3.5f, 13f);
            g.FillRectangle(skin, cx + 6f, cy + 21f, 3.5f, 13f);
            g.FillRectangle(shoes, cx - 10f, cy + 34f, 5.5f, 4f);
            g.FillRectangle(shoes, cx + 5.5f, cy + 34f, 5.5f, 4f);

            float armSpread = (f == 0) ? 12f : 14f;
            g.DrawLine(armPen, cx - 4f, cy - 4f, cx - armSpread, cy - 10f);
            g.DrawLine(armPen, cx + 4f, cy - 4f, cx + armSpread, cy - 10f);
        }

        private static void DrawHit(Graphics g, int f, int w, int h)
        {
            float cx = w * 0.42f;
            float cy = h * 0.50f;
            DrawBasePlayer(g, cx, cy, 0f);

            using var skin = new SolidBrush(SkinColor);
            using var shoes = new SolidBrush(ShoeWhite);
            using var armPen = new Pen(SkinColor, 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round };

            g.FillRectangle(skin, cx - 8f, cy + 21f, 3.5f, 13f);
            g.FillRectangle(skin, cx + 2f, cy + 21f, 3.5f, 13f);
            g.FillRectangle(shoes, cx - 9f, cy + 34f, 5.5f, 4f);
            g.FillRectangle(shoes, cx + 1.5f, cy + 34f, 5.5f, 4f);

            g.DrawLine(armPen, cx - 4f, cy - 4f, cx - 14f, cy - 14f);
            g.DrawLine(armPen, cx + 4f, cy - 4f, cx - 8f, cy - 12f);
        }

        private static void DrawKo(Graphics g, int f, int w, int h)
        {
            float cx = w * 0.50f;
            float cy = h * 0.72f;

            using var skin = new SolidBrush(SkinColor);
            using var hair = new SolidBrush(HairColor);
            using var jersey = new SolidBrush(JerseyRed);
            using var shorts = new SolidBrush(ShortsRed);
            using var shoes = new SolidBrush(ShoeWhite);

            g.FillEllipse(skin, cx - 16f, cy - 6f, 11f, 11f);
            g.FillEllipse(hair, cx - 18f, cy - 7f, 7f, 12f);
            g.FillRectangle(jersey, cx - 7f, cy - 5f, 16f, 10f);
            g.FillRectangle(shorts, cx + 8f, cy - 4f, 11f, 9f);
            g.FillRectangle(skin, cx + 19f, cy - 3f, 11f, 3.5f);
            g.FillRectangle(shoes, cx + 28f, cy - 5f, 5f, 5f);
        }

        private static void GeneratePixelPortraitAndCutIn(string dir)
        {
            string portraitPath = Path.Combine(dir, "portrait.png");
            string cutinPath = Path.Combine(dir, "cutin.png");

            string sourceImgPath = @"C:\Users\jemki\.gemini\antigravity\brain\a2193f6a-f001-4c9d-853d-40f287a06cd6\.user_uploaded\media_1790575599861.png";
            Image? raw = null;
            if (File.Exists(sourceImgPath))
            {
                try { raw = Image.FromFile(sourceImgPath); } catch { }
            }
            if (raw == null && File.Exists(portraitPath))
            {
                try { raw = Image.FromFile(portraitPath); } catch { }
            }
            if (raw == null) return;

            // Low-res pixel grid: 44 x 56 pixels (Authentic Street Fighter / Neo-Geo portrait resolution)
            const int pw = 44, ph = 56;
            using (var pixelGrid = new Bitmap(pw, ph, PixelFormat.Format32bppArgb))
            {
                using (var gSmall = Graphics.FromImage(pixelGrid))
                {
                    gSmall.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    if (raw.Width > 300)
                    {
                        int srcX = (int)(raw.Width * 0.15f);
                        int srcY = (int)(raw.Height * 0.08f);
                        int srcW = (int)(raw.Width * 0.70f);
                        int srcH = (int)(raw.Height * 0.65f);
                        gSmall.DrawImage(raw, new Rectangle(0, 0, pw, ph), new Rectangle(srcX, srcY, srcW, srcH), GraphicsUnit.Pixel);
                    }
                    else
                    {
                        gSmall.DrawImage(raw, new Rectangle(0, 0, pw, ph), new Rectangle(0, 0, raw.Width, raw.Height), GraphicsUnit.Pixel);
                    }
                }
                raw.Dispose();

                // Quantize into 16-bit arcade palette
                for (int y = 0; y < ph; y++)
                {
                    for (int x = 0; x < pw; x++)
                    {
                        var col = pixelGrid.GetPixel(x, y);
                        if (col.A < 20) continue;

                        int r = Math.Clamp((col.R / 32) * 32 + 16, 0, 255);
                        int g = Math.Clamp((col.G / 32) * 32 + 16, 0, 255);
                        int b = Math.Clamp((col.B / 32) * 32 + 16, 0, 255);

                        if (r > 130 && g < 75 && b < 75)
                        {
                            r = 220; g = 30; b = 30; // Vibrant arcade red jersey
                        }
                        else if (r > 170 && g > 130 && b < 60)
                        {
                            r = 245; g = 190; b = 25; // Gold jersey trim / numbers
                        }
                        else if (r > 180 && g > 90 && b < 50)
                        {
                            r = 245; g = 115; b = 20; // Basketball orange
                        }

                        pixelGrid.SetPixel(x, y, Color.FromArgb(col.A, r, g, b));
                    }
                }

                // Upscale with NearestNeighbor to crisp retro arcade portrait (132 x 168)
                const int targetW = 132, targetH = 168;
                using (var upscaled = new Bitmap(targetW, targetH, PixelFormat.Format32bppArgb))
                {
                    using (var gBig = Graphics.FromImage(upscaled))
                    {
                        gBig.InterpolationMode = InterpolationMode.NearestNeighbor;
                        gBig.PixelOffsetMode = PixelOffsetMode.Half;
                        gBig.SmoothingMode = SmoothingMode.None;

                        // Deep arcade dark background behind player
                        using var bgBrush = new LinearGradientBrush(new Rectangle(0, 0, targetW, targetH), Color.FromArgb(40, 12, 22), Color.FromArgb(14, 8, 18), LinearGradientMode.Vertical);
                        gBig.FillRectangle(bgBrush, 0, 0, targetW, targetH);

                        gBig.DrawImage(pixelGrid, new Rectangle(0, 0, targetW, targetH), new Rectangle(0, 0, pw, ph), GraphicsUnit.Pixel);

                        // Arcade gold border
                        using var borderPen = new Pen(Color.FromArgb(245, 210, 40), 3f);
                        gBig.DrawRectangle(borderPen, 1, 1, targetW - 2, targetH - 2);
                    }

                    upscaled.Save(portraitPath, ImageFormat.Png);
                }
            }

            // Create pixel cutin strip for 3-second super intro
            const int fw = 220, fh = 220, count = 6;
            using (var cutinBmp = new Bitmap(fw * count, fh, PixelFormat.Format32bppArgb))
            {
                using (var gCut = Graphics.FromImage(cutinBmp))
                {
                    gCut.InterpolationMode = InterpolationMode.NearestNeighbor;
                    gCut.PixelOffsetMode = PixelOffsetMode.Half;
                    gCut.SmoothingMode = SmoothingMode.None;

                    using var portImg = Image.FromFile(portraitPath);

                    for (int f = 0; f < count; f++)
                    {
                        int ox = f * fw;
                        using var bgBrush = new LinearGradientBrush(new Rectangle(ox, 0, fw, fh), Color.FromArgb(32, 10, 16), Color.FromArgb(10, 6, 14), 45f);
                        gCut.FillRectangle(bgBrush, ox, 0, fw, fh);

                        float zoom = 1.0f + f * 0.08f;
                        int dw = (int)(fw * zoom);
                        int dh = (int)(fh * zoom);
                        int dx = ox + (fw - dw) / 2;
                        int dy = (fh - dh) / 2;

                        gCut.DrawImage(portImg, new Rectangle(dx, dy, dw, dh));

                        // Pixel speedlines
                        using var linePen = new Pen(Color.FromArgb(140 + f * 20, Color.Gold), 3f);
                        for (int s = 0; s < 5; s++)
                        {
                            float sx = ox + 15f + s * 42f;
                            gCut.DrawLine(linePen, sx, 0, sx + 30f, fh);
                        }

                        // Tekken Rage Art red eye flare in later frames
                        if (f >= 2)
                        {
                            float eyeX = ox + fw * 0.44f;
                            float eyeY = fh * 0.30f - (f * 1.5f);
                            float flareSize = 6f + (f - 2) * 5f;
                            using var eyeGleam = new SolidBrush(Color.FromArgb(230, Color.OrangeRed));
                            using var eyeCore = new SolidBrush(Color.White);
                            gCut.FillRectangle(eyeGleam, eyeX - flareSize, eyeY - 2f, flareSize * 2f, 4f);
                            gCut.FillRectangle(eyeGleam, eyeX - 2f, eyeY - flareSize, 4f, flareSize * 2f);
                            gCut.FillRectangle(eyeCore, eyeX - 2f, eyeY - 2f, 4f, 4f);
                        }

                        using var borderPen = new Pen(Color.FromArgb(200, Color.OrangeRed), 3f);
                        gCut.DrawRectangle(borderPen, ox + 1, 1, fw - 2, fh - 2);
                    }
                }

                cutinBmp.Save(cutinPath, ImageFormat.Png);
            }
        }
    }
}

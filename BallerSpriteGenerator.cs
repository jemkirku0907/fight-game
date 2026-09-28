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
                string ultVoiceSource = @"C:\Users\jemki\.gemini\antigravity\brain\a2193f6a-f001-4c9d-853d-40f287a06cd6\.user_uploaded\uploaded_media_1790596330393.mp3";
                string[] soundDirs = new[]
                {
                    Path.Combine(baseDir, "Sounds", "Special"),
                    Path.Combine(Directory.GetCurrentDirectory(), "Sounds", "Special")
                };

                foreach (var sDir in soundDirs)
                {
                    Directory.CreateDirectory(sDir);
                    string targetMp3 = Path.Combine(sDir, "Baller.mp3");
                    if (File.Exists(ultVoiceSource))
                    {
                        try { File.Copy(ultVoiceSource, targetMp3, true); } catch { }
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

        private static Bitmap RemoveBackground(Bitmap src)
        {
            int w = src.Width;
            int h = src.Height;
            var result = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            bool[,] visited = new bool[w, h];
            var q = new Queue<Point>();

            bool IsBgColor(Color c)
            {
                if (c.A < 30) return true;
                int maxDiff = Math.Max(Math.Abs(c.R - c.G), Math.Max(Math.Abs(c.G - c.B), Math.Abs(c.R - c.B)));
                if (maxDiff <= 18 && c.R >= 125) return true;
                return false;
            }

            // Seed outer borders (top, bottom, left, right)
            for (int x = 0; x < w; x++)
            {
                if (IsBgColor(src.GetPixel(x, 0))) { visited[x, 0] = true; q.Enqueue(new Point(x, 0)); }
                if (IsBgColor(src.GetPixel(x, h - 1))) { visited[x, h - 1] = true; q.Enqueue(new Point(x, h - 1)); }
            }
            for (int y = 0; y < h; y++)
            {
                if (!visited[0, y] && IsBgColor(src.GetPixel(0, y))) { visited[0, y] = true; q.Enqueue(new Point(0, y)); }
                if (!visited[w - 1, y] && IsBgColor(src.GetPixel(w - 1, y))) { visited[w - 1, y] = true; q.Enqueue(new Point(w - 1, y)); }
            }

            // 8-directional BFS flood fill so diagonal checkerboard squares are seamlessly traversed
            int[] dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
            int[] dy = { 0, 0, 1, -1, 1, -1, 1, -1 };

            while (q.Count > 0)
            {
                var pt = q.Dequeue();
                for (int i = 0; i < 8; i++)
                {
                    int nx = pt.X + dx[i];
                    int ny = pt.Y + dy[i];
                    if (nx >= 0 && nx < w && ny >= 0 && ny < h && !visited[nx, ny])
                    {
                        if (IsBgColor(src.GetPixel(nx, ny)))
                        {
                            visited[nx, ny] = true;
                            q.Enqueue(new Point(nx, ny));
                        }
                    }
                }
            }

            // Copy to result, setting visited background pixels to 100% transparent
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (visited[x, y])
                    {
                        result.SetPixel(x, y, Color.FromArgb(0, 0, 0, 0));
                    }
                    else
                    {
                        result.SetPixel(x, y, src.GetPixel(x, y));
                    }
                }
            }

            return result;
        }

        private static void GeneratePixelPortraitAndCutIn(string dir)
        {
            string portraitPath = Path.Combine(dir, "portrait.png");
            string cutinPath = Path.Combine(dir, "cutin.png");
            string avatarPath = Path.Combine(dir, "avatar.png");

            string userUploaded = @"C:\Users\jemki\.gemini\antigravity\brain\a2193f6a-f001-4c9d-853d-40f287a06cd6\.user_uploaded\media_1790597173045.png";
            if (!File.Exists(userUploaded))
                userUploaded = @"C:\Users\jemki\.gemini\antigravity\brain\a2193f6a-f001-4c9d-853d-40f287a06cd6\.user_uploaded\media_1790595746467.png";

            Bitmap? cleanAvatar = null;
            if (File.Exists(userUploaded))
            {
                using var rawImg = new Bitmap(userUploaded);
                cleanAvatar = RemoveBackground(rawImg);
                cleanAvatar.Save(avatarPath, ImageFormat.Png);
            }
            else if (File.Exists(avatarPath))
            {
                using var rawImg = new Bitmap(avatarPath);
                cleanAvatar = RemoveBackground(rawImg);
            }

            if (cleanAvatar == null) return;

            // Crop Rene holding the Molten basketball with his #2 DAVRAA jersey
            int srcX = (int)(cleanAvatar.Width * 0.16f);
            int srcY = (int)(cleanAvatar.Height * 0.02f);
            int srcW = (int)(cleanAvatar.Width * 0.65f);
            int srcH = (int)(cleanAvatar.Height * 0.96f);

            const int targetW = 130, targetH = 165;
            using (var upscaled = new Bitmap(targetW, targetH, PixelFormat.Format32bppArgb))
            {
                using (var gBig = Graphics.FromImage(upscaled))
                {
                    gBig.Clear(Color.Transparent); // NO BG!
                    gBig.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    gBig.PixelOffsetMode = PixelOffsetMode.Half;
                    gBig.SmoothingMode = SmoothingMode.AntiAlias;

                    // Draw Rene directly with transparent background
                    gBig.DrawImage(cleanAvatar, new Rectangle(0, 0, targetW, targetH), new Rectangle(srcX, srcY, srcW, srcH), GraphicsUnit.Pixel);
                }

                upscaled.Save(portraitPath, ImageFormat.Png);
            }

            // Create pixel cutin strip for 3-second super intro
            const int fw = 220, fh = 220, count = 6;
            using (var cutinBmp = new Bitmap(fw * count, fh, PixelFormat.Format32bppArgb))
            {
                using (var gCut = Graphics.FromImage(cutinBmp))
                {
                    gCut.Clear(Color.Transparent); // NO BG!
                    gCut.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    gCut.PixelOffsetMode = PixelOffsetMode.Half;

                    using var portImg = Image.FromFile(portraitPath);

                    for (int f = 0; f < count; f++)
                    {
                        int ox = f * fw;

                        // Dynamic zoom
                        float zoom = 1.0f + f * 0.10f;
                        int dw = (int)(fw * zoom);
                        int dh = (int)(fh * zoom);
                        int dx = ox + (fw - dw) / 2;
                        int dy = (fh - dh) / 2;

                        // Gold speedlines (drawn behind portrait so face remains clean)
                        using (var linePen = new Pen(Color.FromArgb(90 + f * 15, Color.Gold), 2f))
                        {
                            for (int s = 0; s < 4; s++)
                            {
                                float sx = ox + 15f + s * 55f;
                                gCut.DrawLine(linePen, sx, 0, sx + 25f, fh);
                            }
                        }

                        gCut.DrawImage(portImg, new Rectangle(dx, dy, dw, dh));

                        // Tekken Rage Art red eye flare in later frames
                        if (f >= 2)
                        {
                            float eyeX = ox + fw * 0.55f;
                            float eyeY = fh * 0.28f - (f * 1.8f);
                            float flareSize = 5f + (f - 2) * 5.5f;
                            using var eyeGleam = new SolidBrush(Color.FromArgb(240, Color.OrangeRed));
                            using var eyeCore = new SolidBrush(Color.White);
                            gCut.FillRectangle(eyeGleam, eyeX - flareSize, eyeY - 2.5f, flareSize * 2f, 5f);
                            gCut.FillRectangle(eyeGleam, eyeX - 2.5f, eyeY - flareSize, 5f, flareSize * 2f);
                            gCut.FillRectangle(eyeCore, eyeX - 2.5f, eyeY - 2.5f, 5f, 5f);
                        }
                    }
                }

                cutinBmp.Save(cutinPath, ImageFormat.Png);
            }
            cleanAvatar.Dispose();
        }
    }
}

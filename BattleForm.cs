using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FightingGame
{
    public class BattleForm : Form
    {
        // ---- Panels (screens) ----
        private readonly Panel pnlStartMenu = new();
        private readonly Panel pnlCharSelect = new();
        private readonly Panel pnlVersus = new();
        private readonly Panel pnlBattle = new();
        private readonly Panel pnlResult = new();

        // ---- Start Menu Animated Elements ----
        private readonly Panel startMenuCanvas = new BufferedPanel();
        private Image? menuBgImage;
        private Image? walkerSpriteStrip;
        private int walkerFrameCount = 8;
        private int walkerFrameIndex = 0;
        private float walkerFrameTimer = 0f;
        private float menuWalkerX = 140f;
        private float menuWalkerDir = 1f;
        private float menuAnimTime = 0f;
        private bool isHoveringStart = false;
        private bool isHoveringMenu = false;
        private bool isHoveringExit = false;

        // ---- Character select controls ----
        private static readonly string[] RosterNames = { "Warrior", "Ninja", "Mage", "Disciple", "Gunner", "Baller" };
        private readonly Dictionary<string, Button> p1Tiles = new();
        private readonly Dictionary<string, Button> p2Tiles = new();
        private readonly Button btnFight = new();
        private readonly Button btnBackMenu = new();
        private readonly Label lblSelectTitle = new();
        private readonly Label lblP1 = new();
        private readonly Label lblP2 = new();
        private string? p1SelectedType;
        private string? p2SelectedType;

        // ---- Versus screen controls ----
        private readonly Label lblVersusText = new();
        private readonly Label lblFightFlash = new();

        // ---- Battle screen controls ----
        private readonly Panel canvas = new BufferedPanel();
        private readonly ListBox lstLog = new();
        private readonly Button btnStatus = new();
        private readonly Label lblControls = new();
        private readonly Label lblLog = new();

        // ---- Result screen controls ----
        private readonly Label lblKO = new();
        private readonly Label lblWinner = new();
        private readonly Button btnRematch = new();
        private readonly Button btnExit = new();

        // ---- Fullscreen / Big Screen Support ----
        private bool isFullscreen = false;
        private FormWindowState prevWindowState = FormWindowState.Normal;
        private FormBorderStyle prevBorderStyle = FormBorderStyle.Sizable;

        // ---- Game state ----
        private Character? p1Char;
        private Character? p2Char;
        private FighterView? p1View;
        private FighterView? p2View;
        private bool gameOver = false;
        private bool practiceLoopActive = false;
        private const int Round = 1;

        // ---- Juice: floating damage numbers, super-flash, special banner ----
        private readonly List<FloatingText> floatingTexts = new();
        private float canvasFlash = 0f;
        private string bannerText = "";
        private float bannerAlpha = 0f;
        private float bannerScale = 0.5f;

        // ---- Arcade-style special intro: portrait flash card + skill name ----
        private readonly List<BoomEffect> boomEffects = new();
        private FighterView? introAttacker;
        private string introSkillName = "";
        private float introDim = 0f;
        private float introCardAlpha = 0f;
        private float introCardScale = 0f;
        private float introFlashPulse = 0f;
        private float introSlideIn = 0f;
        private readonly List<Bitmap> introFrames = new();

        // ---- Hit-stop + camera punch-in (impact juice for sound sync) ----
        private float zoomPunch = 0f;   // 0 = no zoom, 1 = max punch-in
        private PointF zoomCenter;

        private const float GroundYRatio = 0.72f;

        // ---- Real-time movement / jump ----
        private readonly HashSet<Keys> pressedKeys = new();
        private readonly System.Windows.Forms.Timer gameTimer = new() { Interval = 16 };
        private const float MoveSpeed = 4.5f;
        private const float JumpPower = 11f;
        private const float Gravity = 0.55f;
        private const float StageMargin = 50f;
        private const float MinFighterGap = 70f;
        private const float BlockRampPerTick = 0.16f;

        // ---- Real-time combat: P1 = A/D/W move, F/G attack, S block. P2 = arrows move, K/L attack, Down block. ----
        private const Keys P1Left = Keys.A, P1Right = Keys.D, P1Jump = Keys.W, P1Basic = Keys.F, P1Special = Keys.G, P1Block = Keys.S, P1Reload = Keys.R;
        private const Keys P2Left = Keys.Left, P2Right = Keys.Right, P2Jump = Keys.Up, P2Basic = Keys.K, P2Special = Keys.L, P2Block = Keys.Down, P2Reload = Keys.P;
        private const float BasicReach = 130f;
        private const float SpecialReach = 160f;
        private const float GunnerMidRange = 520f;
        private const float BasicCooldownMs = 480f;
        private const float SpecialCooldownMs = 900f;
        private const float RageMax = 100f;
        private const float RageGainPerHit = 22f; // ~5 landed basics to fill the meter

        public BattleForm()
        {
            BallerSpriteGenerator.EnsureSprites();
            RosterCutInGenerator.EnsureAllRosterCutIns(Path.Combine(AppContext.BaseDirectory, "Sprites"));
            SpecialAudioGenerator.EnsureSounds(Path.Combine(AppContext.BaseDirectory, "Sounds"));

            Text = "Simple Fighting Game";
            ClientSize = new Size(1200, 800);
            MinimumSize = new Size(950, 680);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(18, 18, 26);
            Font = new Font("Segoe UI", 9f);
            KeyPreview = true;

            BuildStartMenuPanel();
            BuildCharSelectPanel();
            BuildVersusPanel();
            BuildBattlePanel();
            BuildResultPanel();

            Controls.Add(pnlResult);
            Controls.Add(pnlVersus);
            Controls.Add(pnlBattle);
            Controls.Add(pnlCharSelect);
            Controls.Add(pnlStartMenu);

            KeyDown += Form_KeyDown;
            KeyUp += (s, e) => pressedKeys.Remove(e.KeyCode);
            Resize += (s, e) => LayoutAllScreens();

            gameTimer.Tick += GameLoop_Tick;
            gameTimer.Start();

            ShowOnly(pnlStartMenu);
        }

        private void ToggleFullscreen()
        {
            isFullscreen = !isFullscreen;
            if (isFullscreen)
            {
                prevWindowState = WindowState;
                prevBorderStyle = FormBorderStyle;
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Maximized;
            }
            else
            {
                FormBorderStyle = prevBorderStyle;
                WindowState = prevWindowState;
            }
            LayoutAllScreens();
        }

        // WinForms quirk: arrow keys (and sometimes others) get eaten by whichever
        // control currently has focus (e.g. clicking "SHOW STATS" steals it) for
        // its own focus-navigation, so they never reach Form_KeyDown at all —
        // that's what made facing/movement look "stuck". Catching them here,
        // before any control gets a say, guarantees they always register.
        // (Key-up doesn't have this problem — KeyPreview already routes it to
        // the Form fine — so only key-down needs this extra layer.)
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            const int WM_KEYDOWN = 0x0100;

            if (keyData == Keys.F11 && msg.Msg == WM_KEYDOWN)
            {
                ToggleFullscreen();
                return true;
            }

            if (pnlStartMenu.Visible && msg.Msg == WM_KEYDOWN)
            {
                if (keyData == Keys.Escape)
                {
                    Close();
                    return true;
                }
                ShowOnly(pnlCharSelect);
                return true;
            }

            bool isOurKey = keyData is P1Left or P1Right or P1Jump or P1Basic or P1Special or P1Block or P1Reload
                                    or P2Left or P2Right or P2Jump or P2Basic or P2Special or P2Block or P2Reload;

            if (isOurKey && msg.Msg == WM_KEYDOWN)
            {
                Form_KeyDown(this, new KeyEventArgs(keyData));
                return true; // handled — don't let any control treat it as navigation
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void Form_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F11)
            {
                ToggleFullscreen();
                return;
            }

            if (pnlStartMenu.Visible)
            {
                if (e.KeyCode == Keys.Escape) Close();
                else ShowOnly(pnlCharSelect);
                return;
            }

            bool wasAlreadyDown = pressedKeys.Contains(e.KeyCode);
            pressedKeys.Add(e.KeyCode);
            if (wasAlreadyDown) return; // ignore OS key-repeat — only react to the initial press

            if (e.KeyCode == Keys.T && pnlBattle.Visible)
            {
                TogglePracticeLoop();
                return;
            }

            if (!pnlBattle.Visible || gameOver || p1Char == null || p2Char == null || p1View == null || p2View == null) return;

            switch (e.KeyCode)
            {
                case P1Basic: _ = PerformAttack(p1View, p2View, special: false); break;
                case P1Special: _ = PerformAttack(p1View, p2View, special: true); break;
                case P1Reload:
                    if (p1View.Character is Gunner g1) _ = TriggerManualReload(p1View, g1);
                    break;
                case P2Basic: _ = PerformAttack(p2View, p1View, special: false); break;
                case P2Special: _ = PerformAttack(p2View, p1View, special: true); break;
                case P2Reload:
                    if (p2View.Character is Gunner g2) _ = TriggerManualReload(p2View, g2);
                    break;
            }
        }

        // Runs continuously — movement, jumping, cooldowns and block posture
        // all update live instead of waiting for a turn.
        private void GameLoop_Tick(object? sender, EventArgs e)
        {
            if (pnlStartMenu.Visible)
            {
                UpdateStartMenuAnimation(gameTimer.Interval);
                startMenuCanvas.Invalidate();
                return;
            }

            if (!pnlBattle.Visible || p1View == null || p2View == null) return;

            UpdateMovement(p1View, P1Left, P1Right, P1Jump);
            UpdateMovement(p2View, P2Left, P2Right, P2Jump);
            UpdateBlockPosture(p1View, P1Block);
            UpdateBlockPosture(p2View, P2Block);
            ClampPositions();
            UpdateFacing();

            p1View.UpdateAnimation(gameTimer.Interval, DetermineAnimState(p1View, P1Left, P1Right));
            p2View.UpdateAnimation(gameTimer.Interval, DetermineAnimState(p2View, P2Left, P2Right));

            if (p1View.AttackCooldown > 0f) p1View.AttackCooldown -= gameTimer.Interval;
            if (p2View.AttackCooldown > 0f) p2View.AttackCooldown -= gameTimer.Interval;

            // Update active bullets
            for (int i = activeBullets.Count - 1; i >= 0; i--)
            {
                var b = activeBullets[i];
                b.X += b.VelocityX;
                b.DistanceTraveled += MathF.Abs(b.VelocityX);
                if (b.DistanceTraveled >= b.MaxDistance || b.X < 0 || b.X > canvas.Width)
                    activeBullets.RemoveAt(i);
            }
            // Update muzzle flashes
            for (int i = muzzleFlashes.Count - 1; i >= 0; i--)
            {
                var mf = muzzleFlashes[i];
                mf.Alpha -= 0.16f;
                if (mf.Alpha <= 0f) muzzleFlashes.RemoveAt(i);
            }

            canvas.Invalidate();
        }

        // Faces whichever direction the movement key says — left key = face
        // left, right key = face right. Keeps last facing when no movement
        // key is held. Locked while mid-attack so a swing never flips
        // direction partway through.
        private void UpdateFacing()
        {
            if (p1View == null || p2View == null) return;
            UpdateFacingFromKeys(p1View, P1Left, P1Right);
            UpdateFacingFromKeys(p2View, P2Left, P2Right);
        }

        private void UpdateFacingFromKeys(FighterView v, Keys left, Keys right)
        {
            if (v.IsAttacking) return;
            bool leftHeld = pressedKeys.Contains(left);
            bool rightHeld = pressedKeys.Contains(right);
            if (leftHeld && !rightHeld) v.FacingSign = -1;
            else if (rightHeld && !leftHeld) v.FacingSign = 1;
            // both or neither held: keep whatever facing it already had
        }

        // Picks which animation a fighter should be showing right now —
        // used by the sprite renderer (the stick figure ignores this).
        private AnimState DetermineAnimState(FighterView v, Keys left, Keys right)
        {
            if (!v.Character.IsAlive()) return AnimState.KO;
            if (v.FlashIntensity > 0.35f) return AnimState.Hit;
            if (v.IsAttacking) return v.LastAttackWasSpecial ? AnimState.Special : AnimState.Attack;
            if (v.IsHoldingBlock) return AnimState.Block;
            if (v.JumpHeight > 0.01f) return AnimState.Jump;
            if (pressedKeys.Contains(left) || pressedKeys.Contains(right)) return AnimState.Walk;
            return AnimState.Idle;
        }

        private void UpdateMovement(FighterView v, Keys left, Keys right, Keys jump)
        {
            if (v.IsAttacking || gameOver) return; // committed to the attack animation, can't move mid-swing

            if (pressedKeys.Contains(left)) v.PosX -= MoveSpeed;
            if (pressedKeys.Contains(right)) v.PosX += MoveSpeed;

            bool grounded = v.JumpHeight <= 0.01f && v.VerticalVelocity == 0f;
            if (pressedKeys.Contains(jump) && grounded)
                v.VerticalVelocity = JumpPower;

            if (v.VerticalVelocity != 0f || v.JumpHeight > 0f)
            {
                v.JumpHeight += v.VerticalVelocity;
                v.VerticalVelocity -= Gravity;
                if (v.JumpHeight <= 0f)
                {
                    v.JumpHeight = 0f;
                    v.VerticalVelocity = 0f;
                }
            }
        }

        private void UpdateBlockPosture(FighterView v, Keys blockKey)
        {
            v.IsHoldingBlock = pressedKeys.Contains(blockKey) && !v.IsAttacking && v.Character.IsAlive();
            v.GuardProgress = Math.Clamp(v.GuardProgress + (v.IsHoldingBlock ? BlockRampPerTick : -BlockRampPerTick), 0f, 1f);
        }

        // Keeps both fighters on-stage and stops them from walking through each other.
        private void ClampPositions()
        {
            if (p1View == null || p2View == null) return;

            p1View.PosX = Math.Clamp(p1View.PosX, StageMargin, canvas.Width - StageMargin);
            p2View.PosX = Math.Clamp(p2View.PosX, StageMargin, canvas.Width - StageMargin);

            float gap = p2View.PosX - p1View.PosX;
            if (MathF.Abs(gap) < MinFighterGap)
            {
                float mid = (p1View.PosX + p2View.PosX) / 2f;
                float sign = gap >= 0 ? 1f : -1f; // p1 normally left of p2
                p1View.PosX = Math.Clamp(mid - sign * MinFighterGap / 2f, StageMargin, canvas.Width - StageMargin);
                p2View.PosX = Math.Clamp(mid + sign * MinFighterGap / 2f, StageMargin, canvas.Width - StageMargin);
            }
        }

        // =========================================================
        // Screen 0 — Start / Main Menu (Animated Retro Pixel Art)
        // =========================================================
        private void BuildStartMenuPanel()
        {
            pnlStartMenu.Dock = DockStyle.Fill;
            pnlStartMenu.BackColor = Color.FromArgb(16, 12, 30);

            // Load background image
            string bgPath = Path.Combine(AppContext.BaseDirectory, "Sprites", "pixel_menu.jpg");
            if (!File.Exists(bgPath)) bgPath = Path.Combine(AppContext.BaseDirectory, "pixel_menu.jpg");
            if (File.Exists(bgPath))
            {
                try { menuBgImage = Image.FromFile(bgPath); } catch { }
            }

            // Load walking sprite strip
            string walkPath = Path.Combine(AppContext.BaseDirectory, "Sprites", "Warrior", "walk.png");
            if (File.Exists(walkPath))
            {
                try
                {
                    walkerSpriteStrip = Image.FromFile(walkPath);
                    walkerFrameCount = 8;
                }
                catch { }
            }
            if (walkerSpriteStrip == null)
            {
                walkPath = Path.Combine(AppContext.BaseDirectory, "Sprites", "Ninja", "walk.png");
                if (File.Exists(walkPath))
                {
                    try
                    {
                        walkerSpriteStrip = Image.FromFile(walkPath);
                        walkerFrameCount = 5;
                    }
                    catch { }
                }
            }

            startMenuCanvas.Dock = DockStyle.Fill;
            startMenuCanvas.BackColor = Color.FromArgb(16, 12, 30);
            startMenuCanvas.Paint += StartMenuCanvas_Paint;
            startMenuCanvas.MouseMove += StartMenuCanvas_MouseMove;
            startMenuCanvas.MouseClick += StartMenuCanvas_MouseClick;

            pnlStartMenu.Controls.Clear();
            pnlStartMenu.Controls.Add(startMenuCanvas);
        }

        private void UpdateStartMenuAnimation(float dtMs)
        {
            menuAnimTime += dtMs * 0.001f;

            // Animate walking hero along bottom grass
            menuWalkerX += menuWalkerDir * 1.8f;
            float minX = 60f;
            float maxX = Math.Max(minX + 100f, startMenuCanvas.Width * 0.72f);
            if (menuWalkerX > maxX)
            {
                menuWalkerX = maxX;
                menuWalkerDir = -1f;
            }
            else if (menuWalkerX < minX)
            {
                menuWalkerX = minX;
                menuWalkerDir = 1f;
            }

            walkerFrameTimer += dtMs;
            if (walkerFrameTimer >= 100f)
            {
                walkerFrameTimer = 0f;
                walkerFrameIndex = (walkerFrameIndex + 1) % Math.Max(1, walkerFrameCount);
            }
        }

        private (RectangleF menu, RectangleF start, RectangleF exit) GetMenuButtonRects()
        {
            float w = startMenuCanvas.Width;
            float h = startMenuCanvas.Height;
            float btnY = h * 0.63f;
            float btnH = h * 0.082f;
            float btnW = w * 0.142f;

            var rMenu = new RectangleF(w * 0.218f, btnY, btnW, btnH);
            var rStart = new RectangleF(w * 0.428f, btnY, btnW, btnH);
            var rExit = new RectangleF(w * 0.640f, btnY, btnW, btnH);
            return (rMenu, rStart, rExit);
        }

        private void StartMenuCanvas_MouseMove(object? sender, MouseEventArgs e)
        {
            var (rMenu, rStart, rExit) = GetMenuButtonRects();
            isHoveringMenu = rMenu.Contains(e.Location);
            isHoveringStart = rStart.Contains(e.Location);
            isHoveringExit = rExit.Contains(e.Location);

            if (isHoveringMenu || isHoveringStart || isHoveringExit)
                startMenuCanvas.Cursor = Cursors.Hand;
            else
                startMenuCanvas.Cursor = Cursors.Default;
        }

        private void StartMenuCanvas_MouseClick(object? sender, MouseEventArgs e)
        {
            var (rMenu, rStart, rExit) = GetMenuButtonRects();
            if (rStart.Contains(e.Location) || rMenu.Contains(e.Location))
            {
                ShowOnly(pnlCharSelect);
            }
            else if (rExit.Contains(e.Location))
            {
                Close();
            }
            else
            {
                // Clicking anywhere starts
                ShowOnly(pnlCharSelect);
            }
        }

        private void StartMenuCanvas_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            int w = startMenuCanvas.Width;
            int h = startMenuCanvas.Height;
            if (w <= 0 || h <= 0) return;

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;

            // 1. Draw pixel art background
            if (menuBgImage != null)
            {
                g.DrawImage(menuBgImage, 0, 0, w, h);
            }
            else
            {
                using var bgBrush = new SolidBrush(Color.FromArgb(16, 12, 30));
                g.FillRectangle(bgBrush, 0, 0, w, h);
            }

            // 2. Animated Twinkling Sparkles in the night sky
            var sparkles = new (float x, float y, float speed, float phase, Color col)[]
            {
                (0.06f, 0.52f, 3.2f, 0.0f, Color.FromArgb(255, 180, 255)),
                (0.27f, 0.28f, 2.7f, 1.4f, Color.FromArgb(200, 240, 255)),
                (0.35f, 0.39f, 4.0f, 2.5f, Color.FromArgb(255, 255, 200)),
                (0.61f, 0.27f, 3.5f, 0.8f, Color.FromArgb(255, 200, 255)),
                (0.72f, 0.32f, 4.2f, 3.1f, Color.FromArgb(220, 255, 255)),
                (0.85f, 0.22f, 2.9f, 1.9f, Color.FromArgb(255, 255, 180)),
                (0.18f, 0.15f, 3.8f, 4.2f, Color.FromArgb(240, 220, 255)),
                (0.48f, 0.20f, 2.5f, 0.5f, Color.FromArgb(255, 240, 210))
            };

            foreach (var sp in sparkles)
            {
                float sx = sp.x * w;
                float sy = sp.y * h;
                float pulse = 0.5f + 0.5f * MathF.Sin(menuAnimTime * sp.speed + sp.phase);
                int alpha = (int)(70 + 185 * pulse);
                float size = 4f + 5f * pulse;

                using var spPen = new Pen(Color.FromArgb(alpha, sp.col), 1.6f);
                g.DrawLine(spPen, sx - size, sy, sx + size, sy);
                g.DrawLine(spPen, sx, sy - size, sx, sy + size);

                using var spBrush = new SolidBrush(Color.FromArgb(alpha, Color.White));
                g.FillEllipse(spBrush, sx - 1.5f, sy - 1.5f, 3f, 3f);
            }

            // 3. Animated Torch Flame on castle tower
            float torchX = w * 0.932f;
            float torchY = h * 0.525f;
            for (int i = 0; i < 5; i++)
            {
                float fo = (menuAnimTime * 7f + i * 1.4f) % 3.0f;
                float py = torchY - fo * 6f;
                float px = torchX + MathF.Sin(menuAnimTime * 9f + i * 2f) * 3f;
                int fa = Math.Clamp((int)(240 * (1f - fo / 3.0f)), 0, 255);
                Color fCol = i % 2 == 0 ? Color.FromArgb(fa, 255, 210, 50) : Color.FromArgb(fa, 255, 80, 20);
                using var fb = new SolidBrush(fCol);
                g.FillEllipse(fb, px - 3.5f, py - 3.5f, 7f, 7f);
            }

            // 4. Animated Walking Character across bottom grass
            if (walkerSpriteStrip != null)
            {
                int frameW = walkerSpriteStrip.Width / Math.Max(1, walkerFrameCount);
                int frameH = walkerSpriteStrip.Height;
                int srcX = walkerFrameIndex * frameW;

                float drawH = h * 0.12f;
                float drawW = drawH * ((float)frameW / frameH);
                float drawY = h * 0.94f - drawH;
                float drawX = menuWalkerX;

                var state = g.Save();
                if (menuWalkerDir < 0)
                {
                    g.TranslateTransform(drawX + drawW, drawY);
                    g.ScaleTransform(-1, 1);
                    g.DrawImage(walkerSpriteStrip, new RectangleF(0, 0, drawW, drawH), new RectangleF(srcX, 0, frameW, frameH), GraphicsUnit.Pixel);
                }
                else
                {
                    g.DrawImage(walkerSpriteStrip, new RectangleF(drawX, drawY, drawW, drawH), new RectangleF(srcX, 0, frameW, frameH), GraphicsUnit.Pixel);
                }
                g.Restore(state);
            }

            // 5. Pulsing Button Highlights & Hover Rings
            var (rMenu, rStart, rExit) = GetMenuButtonRects();

            // Menu button hover/active
            if (isHoveringMenu)
            {
                using var pen = new Pen(Color.FromArgb(240, Color.Gold), 3.5f);
                DrawRoundedRect(g, pen, rMenu, 16f);
            }

            // Exit button hover/active
            if (isHoveringExit)
            {
                using var pen = new Pen(Color.FromArgb(240, Color.OrangeRed), 3.5f);
                DrawRoundedRect(g, pen, rExit, 16f);
            }

            // Start button breathing pulse (brighter on hover)
            float startPulse = 0.5f + 0.5f * MathF.Sin(menuAnimTime * 5f);
            int startAlpha = isHoveringStart ? 255 : (int)(110 + 125 * startPulse);
            Color startGlowCol = isHoveringStart ? Color.Cyan : Color.FromArgb(startAlpha, 80, 200, 255);
            using var startPen = new Pen(startGlowCol, isHoveringStart ? 4f : 2.5f);
            DrawRoundedRect(g, startPen, rStart, 16f);

            // 6. Retro animated mouse pointer bobbing over START
            if (!isHoveringStart && !isHoveringMenu && !isHoveringExit)
            {
                float cursorBob = MathF.Sin(menuAnimTime * 4f) * 3f;
                float curX = rStart.Right - 12f;
                float curY = rStart.Bottom - 6f + cursorBob;

                var pts = new PointF[]
                {
                    new(curX, curY),
                    new(curX + 11f, curY + 11f),
                    new(curX + 5f, curY + 12f),
                    new(curX + 9f, curY + 18f),
                    new(curX + 6f, curY + 19f),
                    new(curX + 2f, curY + 13f),
                    new(curX - 2f, curY + 16f)
                };
                using var curBrush = new SolidBrush(Color.White);
                using var curOutline = new Pen(Color.Black, 1.5f);
                g.FillPolygon(curBrush, pts);
                g.DrawPolygon(curOutline, pts);
            }

            // 7. Heartbeat pulse on hearts (bottom left) and diamonds (bottom right)
            float heartPulse = 0.5f + 0.5f * MathF.Sin(menuAnimTime * 3.5f);
            if (heartPulse > 0.82f)
            {
                int hAlpha = (int)((heartPulse - 0.82f) / 0.18f * 100);
                using var hBrush = new SolidBrush(Color.FromArgb(hAlpha, 255, 100, 160));
                g.FillEllipse(hBrush, w * 0.20f, h * 0.81f, w * 0.13f, h * 0.05f);
                g.FillEllipse(hBrush, w * 0.69f, h * 0.81f, w * 0.14f, h * 0.05f);
            }
        }

        private static void DrawRoundedRect(Graphics g, Pen pen, RectangleF bounds, float radius)
        {
            using var path = new GraphicsPath();
            float d = radius * 2f;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            g.DrawPath(pen, path);
        }

        // =========================================================
        // Screen 1 — character select
        // =========================================================
        private void BuildCharSelectPanel()
        {
            pnlCharSelect.Dock = DockStyle.Fill;
            pnlCharSelect.BackColor = BackColor;
            pnlCharSelect.BackgroundImage = null;

            btnBackMenu.Text = "◀ MENU";
            btnBackMenu.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnBackMenu.ForeColor = Color.Gainsboro;
            btnBackMenu.BackColor = Color.FromArgb(30, 30, 45);
            btnBackMenu.Size = new Size(80, 32);
            btnBackMenu.Location = new Point(20, 25);
            btnBackMenu.FlatStyle = FlatStyle.Flat;
            btnBackMenu.Cursor = Cursors.Hand;
            btnBackMenu.UseVisualStyleBackColor = false;
            btnBackMenu.FlatAppearance.BorderSize = 1;
            btnBackMenu.FlatAppearance.BorderColor = Color.DimGray;
            btnBackMenu.MouseEnter += (s, e) => btnBackMenu.BackColor = Color.FromArgb(50, 50, 75);
            btnBackMenu.MouseLeave += (s, e) => btnBackMenu.BackColor = Color.FromArgb(30, 30, 45);
            btnBackMenu.Click += (s, e) => ShowOnly(pnlStartMenu);
            pnlCharSelect.Controls.Add(btnBackMenu);

            lblSelectTitle.Text = "SELECT YOUR FIGHTER";
            lblSelectTitle.Font = new Font("Segoe UI Black", 22f, FontStyle.Bold);
            lblSelectTitle.ForeColor = Color.Gold;
            lblSelectTitle.AutoSize = true;
            lblSelectTitle.Location = new Point(230, 30);

            lblP1.Text = "PLAYER 1";
            lblP1.ForeColor = Color.Cyan;
            lblP1.BackColor = Color.Transparent;
            lblP1.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
            lblP1.AutoSize = true;
            lblP1.Location = new Point(60, 100);

            lblP2.Text = "PLAYER 2";
            lblP2.ForeColor = Color.HotPink;
            lblP2.BackColor = Color.Transparent;
            lblP2.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
            lblP2.AutoSize = true;
            lblP2.Location = new Point(60, 330);

            pnlCharSelect.Controls.Add(lblSelectTitle);
            pnlCharSelect.Controls.Add(lblP1);
            pnlCharSelect.Controls.Add(lblP2);

            const int tileW = 150, tileH = 180, gap = 20, startX = 60;

            for (int i = 0; i < RosterNames.Length; i++)
            {
                string type = RosterNames[i];
                int x = startX + i * (tileW + gap);

                var p1Tile = MakeCharTile(type, x, 130, tileW, tileH);
                p1Tile.Click += (s, e) => { p1SelectedType = type; RefreshTileHighlights(); UpdateFightButtonEnabled(); };
                p1Tiles[type] = p1Tile;
                pnlCharSelect.Controls.Add(p1Tile);

                var p2Tile = MakeCharTile(type, x, 360, tileW, tileH);
                p2Tile.Click += (s, e) => { p2SelectedType = type; RefreshTileHighlights(); UpdateFightButtonEnabled(); };
                p2Tiles[type] = p2Tile;
                pnlCharSelect.Controls.Add(p2Tile);
            }

            btnFight.Text = "FIGHT!";
            btnFight.Location = new Point(startX, 570);
            btnFight.Size = new Size(tileW * RosterNames.Length + gap * (RosterNames.Length - 1), 46);
            btnFight.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
            btnFight.BackColor = Color.FromArgb(230, 255, 0, 110);
            btnFight.ForeColor = Color.White;
            btnFight.FlatStyle = FlatStyle.Flat;
            btnFight.FlatAppearance.BorderSize = 0;
            btnFight.Cursor = Cursors.Hand;
            btnFight.Enabled = false;
            btnFight.Click += BtnFight_Click;
            pnlCharSelect.Controls.Add(btnFight);
        }

        private Button MakeCharTile(string type, int x, int y, int w, int h)
        {
            string label = type == "Baller" ? "Rene" : type;
            var btn = new Button
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = Color.FromArgb(200, 18, 14, 32),
                ForeColor = Color.White,
                Text = label,
                TextImageRelation = TextImageRelation.ImageAboveText,
                ImageAlign = ContentAlignment.MiddleCenter,
                TextAlign = ContentAlignment.BottomCenter,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            btn.FlatAppearance.BorderSize = 2;
            btn.FlatAppearance.BorderColor = Color.FromArgb(140, 80, 50, 120);

            var portrait = LoadPortrait(type);
            if (portrait != null) btn.Image = portrait;

            return btn;
        }

        // Loads dedicated portrait.png for Baller (Rene), otherwise loads full normal form from idle.png
        private static Bitmap? LoadPortrait(string type, int targetHeight = 110)
        {
            if (type == "Baller")
            {
                string portraitPath = Path.Combine(AppContext.BaseDirectory, "Sprites", type, "portrait.png");
                if (File.Exists(portraitPath))
                {
                    try
                    {
                        using var full = Image.FromFile(portraitPath);
                        float scale = targetHeight / (float)full.Height;
                        int destW = Math.Max(1, (int)(full.Width * scale));
                        var bmp = new Bitmap(destW, targetHeight);
                        using var g = Graphics.FromImage(bmp);
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode = PixelOffsetMode.Half;
                        g.DrawImage(full, 0, 0, destW, targetHeight);
                        return bmp;
                    }
                    catch { }
                }
            }

            string path = Path.Combine(AppContext.BaseDirectory, "Sprites", type, "idle.png");
            if (!File.Exists(path)) return null;

            using var idleImg = Image.FromFile(path);
            int frameCount = Math.Max(1, FrameCountsFor(type)[AnimState.Idle]);
            int frameW = Math.Max(1, idleImg.Width / frameCount);
            int frameH = idleImg.Height;

            float s = targetHeight / (float)frameH;
            int dW = Math.Max(1, (int)(frameW * s));

            var result = new Bitmap(dW, targetHeight);
            using var gr = Graphics.FromImage(result);
            gr.InterpolationMode = InterpolationMode.NearestNeighbor;
            gr.PixelOffsetMode = PixelOffsetMode.Half;
            gr.DrawImage(idleImg, new Rectangle(0, 0, dW, targetHeight), new Rectangle(0, 0, frameW, frameH), GraphicsUnit.Pixel);
            return result;
        }

        private void RefreshTileHighlights()
        {
            foreach (var kv in p1Tiles)
            {
                bool selected = kv.Key == p1SelectedType;
                kv.Value.FlatAppearance.BorderColor = selected ? Color.Cyan : Color.FromArgb(140, 80, 50, 120);
                kv.Value.FlatAppearance.BorderSize = selected ? 4 : 2;
                kv.Value.BackColor = selected ? Color.FromArgb(230, 25, 45, 65) : Color.FromArgb(200, 18, 14, 32);
            }
            foreach (var kv in p2Tiles)
            {
                bool selected = kv.Key == p2SelectedType;
                kv.Value.FlatAppearance.BorderColor = selected ? Color.HotPink : Color.FromArgb(140, 80, 50, 120);
                kv.Value.FlatAppearance.BorderSize = selected ? 4 : 2;
                kv.Value.BackColor = selected ? Color.FromArgb(230, 65, 25, 45) : Color.FromArgb(200, 18, 14, 32);
            }
        }

        private void UpdateFightButtonEnabled()
        {
            btnFight.Enabled = p1SelectedType != null && p2SelectedType != null;
        }

        // Maps a roster name to a fresh Character instance + the frame
        // counts for its sprite folder (folder name == roster name).
        private static Character CreateCharacter(string type, string name) => type switch
        {
            "Warrior" => new Warrior(name),
            "Ninja" => new Ninja(name),
            "Mage" => new Mage(name),
            "Disciple" => new Disciple(name),
            "Gunner" => new Gunner(name),
            "Baller" => new Baller(name == "Baller" || name.Contains("Baterbonia") ? "Rene" : name),
            _ => new Warrior(name)
        };

        private static Dictionary<AnimState, int> FrameCountsFor(string type) => type switch
        {
            "Warrior" => SpriteSet.FumikoFrameCounts,
            "Ninja" => SpriteSet.SamuraiFrameCounts,
            "Mage" => SpriteSet.MageFrameCounts,
            "Disciple" => SpriteSet.DiscipleFrameCounts,
            "Gunner" => SpriteSet.GunnerFrameCounts,
            "Baller" => SpriteSet.BallerFrameCounts,
            _ => SpriteSet.FumikoFrameCounts
        };

        private async void BtnFight_Click(object? sender, EventArgs e)
        {
            string p1Type = p1SelectedType!;
            string p2Type = p2SelectedType!;
            string p1Name = p1Type == "Baller" ? "Rene" : p1Type;
            string p2Name = p2Type == "Baller" ? "Rene" : p2Type;

            p1Char = CreateCharacter(p1Type, p1Name);
            p2Char = CreateCharacter(p2Type, p2Name);
            p1Char.OnLog += AppendLog;
            p2Char.OnLog += AppendLog;

            float groundY = canvas.Height * GroundYRatio;
            p1View = new FighterView(p1Char, canvas.Width * 0.28f, groundY, Color.DeepSkyBlue, facingSign: 1);
            p2View = new FighterView(p2Char, canvas.Width * 0.72f, groundY, Color.OrangeRed, facingSign: -1);
            p1View.Sprites = SpriteSet.TryLoad(Path.Combine(AppContext.BaseDirectory, "Sprites", p1Type), FrameCountsFor(p1Type));
            p2View.Sprites = SpriteSet.TryLoad(Path.Combine(AppContext.BaseDirectory, "Sprites", p2Type), FrameCountsFor(p2Type));

            gameOver = false;
            floatingTexts.Clear();
            boomEffects.Clear();
            activeBullets.Clear();
            muzzleFlashes.Clear();
            canvasFlash = 0f;
            bannerAlpha = 0f;
            introAttacker = null;
            introDim = 0f;
            introCardAlpha = 0f;
            pressedKeys.Clear();
            lstLog.Items.Clear();

            lblVersusText.Text = $"{p1Name.ToUpper()}   VS   {p2Name.ToUpper()}";
            lblFightFlash.Visible = false;
            ShowOnly(pnlVersus);
            await Task.Delay(1100);

            lblFightFlash.Visible = true;
            await Task.Delay(500);
            lblFightFlash.Visible = false;

            ShowOnly(pnlBattle);
            canvas.Focus();
            canvas.Invalidate();
            AppendLog("The fight begins! Move in and press your attack keys.");
        }

        // =========================================================
        // Screen 2 — VS splash
        // =========================================================
        private void BuildVersusPanel()
        {
            pnlVersus.Dock = DockStyle.Fill;
            pnlVersus.BackColor = Color.Black;

            lblVersusText.AutoSize = false;
            lblVersusText.Size = new Size(900, 60);
            lblVersusText.Location = new Point(0, 280);
            lblVersusText.TextAlign = ContentAlignment.MiddleCenter;
            lblVersusText.ForeColor = Color.Yellow;
            lblVersusText.Font = new Font("Segoe UI Black", 22f, FontStyle.Bold | FontStyle.Italic);
            pnlVersus.Controls.Add(lblVersusText);

            lblFightFlash.AutoSize = false;
            lblFightFlash.Size = new Size(900, 90);
            lblFightFlash.Location = new Point(0, 400);
            lblFightFlash.TextAlign = ContentAlignment.MiddleCenter;
            lblFightFlash.Text = "FIGHT!";
            lblFightFlash.ForeColor = Color.Red;
            lblFightFlash.Font = new Font("Segoe UI Black", 42f, FontStyle.Bold | FontStyle.Italic);
            lblFightFlash.Visible = false;
            pnlVersus.Controls.Add(lblFightFlash);
        }

        // =========================================================
        // Screen 3 — battle
        // =========================================================
        private void BuildBattlePanel()
        {
            pnlBattle.Dock = DockStyle.Fill;
            pnlBattle.BackColor = BackColor;

            canvas.Location = new Point(0, 0);
            canvas.Size = new Size(900, 420);
            canvas.BackColor = Color.FromArgb(30, 34, 54);
            canvas.Paint += Canvas_Paint;
            canvas.TabStop = true;
            pnlBattle.Controls.Add(canvas);

            lblControls.Text = "P1: A/D move  W jump  F attack  G special  S block  R reload      P2: ←/→ move  ↑ jump  K attack  L special  ↓ block  P reload      [F11: Fullscreen]";
            lblControls.Location = new Point(20, 430);
            lblControls.AutoSize = true;
            lblControls.ForeColor = Color.Gray;
            lblControls.Font = new Font("Segoe UI", 9f, FontStyle.Italic);
            pnlBattle.Controls.Add(lblControls);

            btnStatus.Text = "📋  SHOW STATS";
            btnStatus.Location = new Point(20, 460);
            btnStatus.Size = new Size(205, 40);
            btnStatus.FlatStyle = FlatStyle.Flat;
            btnStatus.FlatAppearance.BorderSize = 0;
            btnStatus.BackColor = Color.FromArgb(65, 65, 85);
            btnStatus.ForeColor = Color.White;
            btnStatus.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            btnStatus.Cursor = Cursors.Hand;
            btnStatus.MouseEnter += (o, e) => btnStatus.BackColor = Color.FromArgb(90, 90, 115);
            btnStatus.MouseLeave += (o, e) => btnStatus.BackColor = Color.FromArgb(65, 65, 85);
            btnStatus.Click += (s, e) =>
            {
                p1Char?.DisplayStatus();
                p2Char?.DisplayStatus();
            };
            pnlBattle.Controls.Add(btnStatus);

            lblLog.Text = "COMBAT LOG";
            lblLog.ForeColor = Color.Gray;
            lblLog.AutoSize = true;
            lblLog.Location = new Point(20, 516);
            lblLog.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
            pnlBattle.Controls.Add(lblLog);
            lstLog.Location = new Point(20, 536);
            lstLog.Size = new Size(860, 120);
            lstLog.BackColor = Color.FromArgb(12, 12, 18);
            lstLog.ForeColor = Color.Gainsboro;
            lstLog.BorderStyle = BorderStyle.FixedSingle;
            lstLog.Font = new Font("Consolas", 9f);
            lstLog.DrawMode = DrawMode.OwnerDrawFixed;
            lstLog.ItemHeight = 18;
            lstLog.DrawItem += LstLog_DrawItem;
            pnlBattle.Controls.Add(lblLog);
            pnlBattle.Controls.Add(lstLog);
        }

        private void LstLog_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.DrawBackground();

            string text = lstLog.Items[e.Index]?.ToString() ?? "";
            Color color = Color.Gainsboro;
            FontStyle style = FontStyle.Regular;

            if (text.StartsWith("---"))
            {
                color = Color.White;
                style = FontStyle.Bold;
            }
            else if (text.Contains("SPECIAL", StringComparison.OrdinalIgnoreCase) ||
                     text.Contains("INFERNO METEOR", StringComparison.OrdinalIgnoreCase) ||
                     text.Contains("DRAGON BLADE", StringComparison.OrdinalIgnoreCase) ||
                     text.Contains("SHADOW OMNI", StringComparison.OrdinalIgnoreCase) ||
                     text.Contains("DEMONIC SHADOW", StringComparison.OrdinalIgnoreCase) ||
                     text.Contains("BULLET STORM", StringComparison.OrdinalIgnoreCase) ||
                     text.Contains("MAMA", StringComparison.OrdinalIgnoreCase) ||
                     text.Contains("SLAM DUNK", StringComparison.OrdinalIgnoreCase) ||
                     text.Contains("BASKETBALL", StringComparison.OrdinalIgnoreCase) ||
                     text.Contains("RENE", StringComparison.OrdinalIgnoreCase) ||
                     text.Contains("BATERBONIA", StringComparison.OrdinalIgnoreCase) ||
                     text.Contains("TARGET-LOCKED", StringComparison.OrdinalIgnoreCase) ||
                     text.Contains("TARGET LOCK", StringComparison.OrdinalIgnoreCase))
                color = Color.Gold;
            else if (text.Contains("misses", StringComparison.OrdinalIgnoreCase) || text.Contains("too far", StringComparison.OrdinalIgnoreCase))
                color = Color.Gray;
            else if (text.Contains("blocks", StringComparison.OrdinalIgnoreCase))
                color = Color.DeepSkyBlue;
            else if (text.Contains("WINS", StringComparison.OrdinalIgnoreCase) || text.Contains("K.O", StringComparison.OrdinalIgnoreCase))
                color = Color.Red;
            else if (text.Contains("takes", StringComparison.OrdinalIgnoreCase))
                color = Color.OrangeRed;

            using var font = new Font(lstLog.Font, style);
            using var brush = new SolidBrush(color);
            e.Graphics.DrawString(text, font, brush, e.Bounds.X + 4, e.Bounds.Y + 1);
            e.DrawFocusRectangle();
        }

        private void Canvas_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Camera punch-in: the "world" (background/fighters/effects)
            // scales in briefly on the impact point. HUD and overlays below
            // are drawn after Restore() so they stay screen-locked and crisp.
            var worldState = g.Save();
            if (zoomPunch > 0.005f)
            {
                float scale = 1f + zoomPunch * 0.18f;
                g.TranslateTransform(zoomCenter.X, zoomCenter.Y);
                g.ScaleTransform(scale, scale);
                g.TranslateTransform(-zoomCenter.X, -zoomCenter.Y);
            }

            using (var sky = new LinearGradientBrush(canvas.ClientRectangle, Color.FromArgb(45, 50, 85), Color.FromArgb(20, 22, 40), LinearGradientMode.Vertical))
                g.FillRectangle(sky, canvas.ClientRectangle);

            float groundY = canvas.Height * GroundYRatio;
            using (var groundBrush = new SolidBrush(Color.FromArgb(40, 30, 30, 45)))
                g.FillRectangle(groundBrush, 0, groundY, canvas.Width, canvas.Height - groundY);
            using (var groundPen = new Pen(Color.FromArgb(90, 255, 255, 255), 2f))
                g.DrawLine(groundPen, 0, groundY, canvas.Width, groundY);

            DrawChargingFighterEffectsFloor(g);
            p1View?.Draw(g);
            p2View?.Draw(g);
            DrawChargingFighterEffectsAura(g);

            DrawBullets(g);
            DrawFloatingTexts(g);
            DrawBoomEffects(g);
            g.Restore(worldState);

            DrawHud(g);
            DrawBanner(g);
            DrawScreenFlash(g);
            DrawSpecialIntro(g);
        }

        private void DrawHud(Graphics g)
        {
            if (p1View == null || p2View == null) return;

            const int barW = 280, barH = 24, marginX = 24, y = 18;
            const int rageBarH = 10, rageGap = 5;
            DrawHealthBar(g, marginX, y, barW, barH, p1View, alignRight: false);
            DrawHealthBar(g, canvas.Width - marginX - barW, y, barW, barH, p2View, alignRight: true);
            DrawRageBar(g, marginX, y + barH + rageGap, barW, rageBarH, p1View);
            DrawRageBar(g, canvas.Width - marginX - barW, y + barH + rageGap, barW, rageBarH, p2View);

            if (p1View.Character is Gunner g1)
                DrawGunnerAmmoHud(g, marginX, y + barH + rageGap + rageBarH + 6, barW, g1, alignRight: false, "R: Reload");
            if (p2View.Character is Gunner g2)
                DrawGunnerAmmoHud(g, canvas.Width - marginX - barW, y + barH + rageGap + rageBarH + 6, barW, g2, alignRight: true, "P: Reload");

            using var vsFont = new Font("Segoe UI Black", 18f, FontStyle.Bold);
            using var vsBrush = new SolidBrush(Color.Gold);
            var vsSize = g.MeasureString("VS", vsFont);
            g.DrawString("VS", vsFont, vsBrush, canvas.Width / 2f - vsSize.Width / 2f, y);

            using var roundFont = new Font("Segoe UI", 10f, FontStyle.Bold);
            string roundText = $"ROUND {Round}";
            var rSize = g.MeasureString(roundText, roundFont);
            g.DrawString(roundText, roundFont, Brushes.White, canvas.Width / 2f - rSize.Width / 2f, y + vsSize.Height + 2);
        }

        private static void DrawHealthBar(Graphics g, int x, int y, int w, int h, FighterView v, bool alignRight)
        {
            float maxHp = v.Character.GetMaxHP();
            float ratio = maxHp <= 0 ? 0 : Math.Clamp(v.DisplayHp / maxHp, 0f, 1f);

            using (var bg = new SolidBrush(Color.FromArgb(200, 25, 25, 30)))
                g.FillRectangle(bg, x, y, w, h);

            Color barColor = ratio > 0.5f ? Color.LimeGreen : ratio > 0.2f ? Color.Gold : Color.Red;
            int filledW = (int)(w * ratio);
            using (var fill = new SolidBrush(barColor))
            {
                if (!alignRight)
                    g.FillRectangle(fill, x, y, filledW, h);
                else
                    g.FillRectangle(fill, x + (w - filledW), y, filledW, h);
            }
            using (var border = new Pen(Color.White, 2f))
                g.DrawRectangle(border, x, y, w, h);

            using var nameFont = new Font("Segoe UI", 10f, FontStyle.Bold);
            string nameText = v.Character.GetName().ToUpper();
            var nameSize = g.MeasureString(nameText, nameFont);
            float nameX = alignRight ? x + w - nameSize.Width : x;
            g.DrawString(nameText, nameFont, Brushes.White, nameX, y - nameSize.Height - 2);

            string hpText = $"{Math.Max(0, (int)Math.Round(v.DisplayHp))}/{(int)maxHp}";
            using var hpFont = new Font("Consolas", 8.5f, FontStyle.Bold);
            var hpSize = g.MeasureString(hpText, hpFont);
            g.DrawString(hpText, hpFont, Brushes.Black, x + w / 2f - hpSize.Width / 2f + 1, y + h / 2f - hpSize.Height / 2f + 1);
            g.DrawString(hpText, hpFont, Brushes.White, x + w / 2f - hpSize.Width / 2f, y + h / 2f - hpSize.Height / 2f);
        }

        private static void DrawRageBar(Graphics g, int x, int y, int w, int h, FighterView v)
        {
            const float rageMax = 100f;
            float ratio = Math.Clamp(v.Rage / rageMax, 0f, 1f);
            bool full = ratio >= 0.999f;

            using (var bg = new SolidBrush(Color.FromArgb(200, 25, 25, 30)))
                g.FillRectangle(bg, x, y, w, h);

            int filledW = (int)(w * ratio);
            Color rageColor = full ? Color.Gold : Color.MediumPurple;
            using (var fill = new SolidBrush(rageColor))
                g.FillRectangle(fill, x, y, filledW, h);

            using (var border = new Pen(full ? Color.Gold : Color.White, full ? 2.5f : 1.5f))
                g.DrawRectangle(border, x, y, w, h);

            if (full)
            {
                using var readyFont = new Font("Segoe UI", 7.5f, FontStyle.Bold);
                string readyText = "SPECIAL READY";
                var size = g.MeasureString(readyText, readyFont);
                g.DrawString(readyText, readyFont, Brushes.Black, x + w / 2f - size.Width / 2f, y + h / 2f - size.Height / 2f);
            }
        }

        private void DrawFloatingTexts(Graphics g)
        {
            foreach (var ft in floatingTexts)
            {
                int alpha = (int)Math.Clamp(ft.Alpha * 255f, 0, 255);
                using var font = new Font("Segoe UI", ft.FontSize, FontStyle.Bold);
                var size = g.MeasureString(ft.Text, font);
                float x = ft.X - size.Width / 2f;
                using var shadow = new SolidBrush(Color.FromArgb(alpha, Color.Black));
                using var brush = new SolidBrush(Color.FromArgb(alpha, ft.Color));
                g.DrawString(ft.Text, font, shadow, x + 1, ft.Y + 1);
                g.DrawString(ft.Text, font, brush, x, ft.Y);
            }
        }

        private void DrawBanner(Graphics g)
        {
            if (bannerAlpha <= 0.01f || string.IsNullOrEmpty(bannerText)) return;

            using var font = new Font("Segoe UI Black", Math.Max(10f, 24f * bannerScale), FontStyle.Bold | FontStyle.Italic);
            var size = g.MeasureString(bannerText, font);
            float x = canvas.Width / 2f - size.Width / 2f;
            float y = canvas.Height * 0.34f - size.Height / 2f;

            int alpha = (int)Math.Clamp(bannerAlpha * 255f, 0, 255);
            using var outline = new SolidBrush(Color.FromArgb(alpha, Color.Black));
            using var fill = new SolidBrush(Color.FromArgb(alpha, Color.Gold));
            g.DrawString(bannerText, font, outline, x + 2, y + 2);
            g.DrawString(bannerText, font, fill, x, y);
        }

        private void DrawScreenFlash(Graphics g)
        {
            if (canvasFlash <= 0.01f) return;
            int alpha = (int)Math.Clamp(canvasFlash * 170f, 0, 170);
            using var flashBrush = new SolidBrush(Color.FromArgb(alpha, Color.White));
            g.FillRectangle(flashBrush, canvas.ClientRectangle);
        }

        // The "boom" — an expanding shockwave (rings + radiating spikes + a
        // bright core) spawned right where a special attack actually lands.
        private void DrawBoomEffects(Graphics g)
        {
            foreach (var b in boomEffects)
            {
                float t = b.Progress;

                for (int ring = 0; ring < 3; ring++)
                {
                    float ringT = Math.Clamp(t - ring * 0.12f, 0f, 1f);
                    if (ringT <= 0f) continue;
                    float radius = 10f + ringT * 90f;
                    int ringAlpha = (int)Math.Clamp((1f - ringT) * 200f, 0, 200);
                    using var ringPen = new Pen(Color.FromArgb(ringAlpha, b.Color), Math.Max(1f, 4f - ringT * 2.5f));
                    g.DrawEllipse(ringPen, b.X - radius, b.Y - radius, radius * 2, radius * 2);
                }

                const int spikeCount = 10;
                float spikeLen = 18f + t * 55f;
                int spikeAlpha = (int)Math.Clamp((1f - t) * 220f, 0, 220);
                using var spikePen = new Pen(Color.FromArgb(spikeAlpha, Color.White), 3f);
                for (int i = 0; i < spikeCount; i++)
                {
                    double angle = i * (2 * Math.PI / spikeCount) + t * 0.6;
                    float x1 = b.X + (float)Math.Cos(angle) * 6f;
                    float y1 = b.Y + (float)Math.Sin(angle) * 6f;
                    float x2 = b.X + (float)Math.Cos(angle) * (6f + spikeLen);
                    float y2 = b.Y + (float)Math.Sin(angle) * (6f + spikeLen);
                    g.DrawLine(spikePen, x1, y1, x2, y2);
                }

                if (t < 0.35f)
                {
                    float coreT = t / 0.35f;
                    int coreAlpha = (int)Math.Clamp((1f - coreT) * 255f, 0, 255);
                    float coreR = 26f * (1f - coreT) + 10f;
                    using var coreBrush = new SolidBrush(Color.FromArgb(coreAlpha, Color.White));
                    g.FillEllipse(coreBrush, b.X - coreR, b.Y - coreR, coreR * 2, coreR * 2);
                }
            }
        }

        private void DrawChargingFighterEffectsFloor(Graphics g)
        {
            if (introAttacker == null || introCardAlpha <= 0.01f) return;

            float fx = introAttacker.PosX;
            float fy = introAttacker.GroundY;
            float pulse = introFlashPulse;
            float a = introCardAlpha;

            // Expanding magical energy rings on the floor beneath feet
            for (int r = 0; r < 4; r++)
            {
                float ringProgress = (pulse * 2.8f + r * 0.25f) % 1f;
                float rw = 50f + ringProgress * 160f;
                float rh = rw * 0.35f;
                int rAlpha = (int)(a * 190f * (1f - ringProgress));
                Color col = r % 2 == 0 ? introAttacker.TeamColor : Color.Gold;
                using var ringPen = new Pen(Color.FromArgb(rAlpha, col), 2.5f);
                g.DrawEllipse(ringPen, fx - rw / 2f, fy - rh / 2f, rw, rh);
            }
        }

        private void DrawChargingFighterEffectsAura(Graphics g)
        {
            if (introAttacker == null || introCardAlpha <= 0.01f) return;

            float fx = introAttacker.PosX;
            float fy = introAttacker.GroundY;
            float pulse = introFlashPulse;
            float a = introCardAlpha;

            // Rising aura wisps / energy particles swirling up
            int particleCount = 18;
            for (int p = 0; p < particleCount; p++)
            {
                float pProg = (pulse * 3.8f + p * (1f / particleCount)) % 1f;
                float angle = p * (MathF.PI * 2f / particleCount) + pulse * 5f;
                float px = fx + MathF.Cos(angle) * (26f + pProg * 32f);
                float py = fy - pProg * 160f;
                float pSize = 7f + 9f * (1f - pProg);
                int pAlpha = (int)(a * 220f * (1f - pProg));

                Color auraCol = p % 2 == 0 ? introAttacker.TeamColor : (p % 3 == 0 ? Color.Cyan : Color.Gold);
                using var pBrush = new SolidBrush(Color.FromArgb(pAlpha, auraCol));
                g.FillEllipse(pBrush, px - pSize / 2f, py - pSize / 2f, pSize, pSize);
            }

            // Crackling lightning bolts
            using var sparkPen = new Pen(Color.FromArgb((int)(a * 240), Color.White), 2f);
            var rnd = new Random((int)(pulse * 350));
            for (int s = 0; s < 6; s++)
            {
                float sx1 = fx + rnd.Next(-32, 33);
                float sy1 = fy - rnd.Next(15, 125);
                float sx2 = sx1 + rnd.Next(-20, 21);
                float sy2 = sy1 - rnd.Next(12, 35);
                g.DrawLine(sparkPen, sx1, sy1, sx2, sy2);
            }
        }

        // Arcade-style super intro: dim the arena, pop up a portrait card
        // (real sprite art if loaded, a team-color placeholder if not) with
        // a strobing glow ring, and show the move's name underneath.
        private void DrawSpecialIntro(Graphics g)
        {
            if (introAttacker == null || introCardAlpha <= 0.01f) return;

            int alpha = (int)Math.Clamp(introCardAlpha * 255f, 0, 255);

            // 1. Dark vignette & arena dimming
            int dimAlpha = (int)Math.Clamp(introDim * 215f, 0, 215);
            if (dimAlpha > 0)
            {
                using var dimBrush = new SolidBrush(Color.FromArgb(dimAlpha, 10, 8, 22));
                g.FillRectangle(dimBrush, canvas.ClientRectangle);
            }

            // 2. Animated anime speed lines across screen
            float timeOffset = introFlashPulse * 450f;
            using (var speedLinePen = new Pen(Color.FromArgb((int)(introCardAlpha * 75), Color.White), 2f))
            {
                for (int i = 0; i < 16; i++)
                {
                    float y = (i * 38f + timeOffset) % canvas.Height;
                    g.DrawLine(speedLinePen, 0, y, canvas.Width, y - 60f);
                }
            }

            // 3. Tekken-style diagonal cut-in ribbon banner
            float bandY = canvas.Height * 0.32f;
            float bandH = 155f;
            PointF[] bannerPts = new PointF[]
            {
                new PointF(-40f, bandY - 20f),
                new PointF(canvas.Width + 40f, bandY - 45f),
                new PointF(canvas.Width + 40f, bandY + bandH - 25f),
                new PointF(-40f, bandY + bandH)
            };

            int bannerAlpha = (int)Math.Clamp(introCardAlpha * 230f, 0, 230);
            using (var bannerBg = new LinearGradientBrush(
                new PointF(0, bandY), new PointF(canvas.Width, bandY + bandH),
                Color.FromArgb(bannerAlpha, 25, 12, 38), Color.FromArgb(bannerAlpha, 12, 10, 24)))
            {
                g.FillPolygon(bannerBg, bannerPts);
            }

            using (var topNeonPen = new Pen(Color.FromArgb(bannerAlpha, Color.FromArgb(0, 210, 255)), 4f))
                g.DrawLine(topNeonPen, bannerPts[0], bannerPts[1]);
            using (var botNeonPen = new Pen(Color.FromArgb(bannerAlpha, Color.Gold), 3f))
                g.DrawLine(botNeonPen, bannerPts[3], bannerPts[2]);

            // 4. Character Face / Portrait Cut-In on the Left Side
            float slideX = (1f - introSlideIn) * -380f;
            float portraitX = 45f + slideX;
            float portraitY = bandY - 25f;
            float portraitSize = 165f;
            var portRect = new RectangleF(portraitX, portraitY, portraitSize, portraitSize);

            // Pulsing aura behind portrait
            float pulse = 0.5f + 0.5f * MathF.Sin(introFlashPulse * MathF.PI * 8f);
            int glowAlpha = (int)Math.Clamp(alpha * (0.35f + 0.55f * pulse), 0, 255);
            using (var glowBrush = new SolidBrush(Color.FromArgb(glowAlpha, introAttacker.TeamColor)))
            {
                g.FillEllipse(glowBrush, portRect.X - 20f, portRect.Y - 20f, portRect.Width + 40f, portRect.Height + 40f);
            }

            // Portrait box background
            using (var cardBg = new SolidBrush(Color.FromArgb(alpha, 16, 12, 28)))
                g.FillRectangle(cardBg, portRect);

            // Draw character portrait image inside box
            if (introFrames.Count > 0)
            {
                // Animated frame index: dynamically cycles through special attack frames during the charge!
                int fIdx = (int)(introFlashPulse * introFrames.Count * 5f) % introFrames.Count;
                var currentFrame = introFrames[fIdx];

                var prevInterp = g.InterpolationMode;
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                float imgW = currentFrame.Width;
                float imgH = currentFrame.Height;
                // Dynamic charge zoom as power builds up towards the beat drop
                float chargeZoom = 1.15f + 0.15f * introFlashPulse;
                float scale = Math.Min(portRect.Width / imgW, portRect.Height / imgH) * chargeZoom;
                float dw = imgW * scale;
                float dh = imgH * scale;
                float dx = portRect.X + (portRect.Width - dw) / 2f;
                float dy = portRect.Y + (portRect.Height - dh) / 2f;
                g.DrawImage(currentFrame, new RectangleF(dx, dy, dw, dh));
                g.InterpolationMode = prevInterp;
            }
            else
            {
                // Fallback team circle with initial
                float pad = portRect.Width * 0.15f;
                var circleRect = new RectangleF(portRect.X + pad, portRect.Y + pad, portRect.Width - pad * 2, portRect.Height - pad * 2);
                using var circleBrush = new SolidBrush(Color.FromArgb(alpha, introAttacker.TeamColor));
                g.FillEllipse(circleBrush, circleRect);

                string name = introAttacker.Character.GetName();
                string letter = name.Length > 0 ? name[0].ToString().ToUpper() : "?";
                using var letterFont = new Font("Segoe UI Black", portRect.Width * 0.32f, FontStyle.Bold);
                var letterSize = g.MeasureString(letter, letterFont);
                using var letterBrush = new SolidBrush(Color.FromArgb(alpha, Color.White));
                g.DrawString(letter, letterFont, letterBrush,
                    circleRect.X + circleRect.Width / 2f - letterSize.Width / 2f,
                    circleRect.Y + circleRect.Height / 2f - letterSize.Height / 2f);
            }

            // Glowing border on portrait box
            using (var borderPen = new Pen(Color.FromArgb(alpha, Color.Gold), 3.5f))
                g.DrawRectangle(borderPen, portRect.X, portRect.Y, portRect.Width, portRect.Height);

            // Eye flare / lens spark
            if (introCardAlpha > 0.4f)
            {
                float sparkX = portRect.X + portRect.Width * 0.65f;
                float sparkY = portRect.Y + portRect.Height * 0.35f;
                float sparkLen = 14f * pulse + 6f;
                using var sparkPen = new Pen(Color.FromArgb(alpha, Color.White), 2.5f);
                g.DrawLine(sparkPen, sparkX - sparkLen, sparkY, sparkX + sparkLen, sparkY);
                g.DrawLine(sparkPen, sparkX, sparkY - sparkLen, sparkX, sparkY + sparkLen);
                using var sparkDot = new SolidBrush(Color.FromArgb(alpha, Color.Gold));
                g.FillEllipse(sparkDot, sparkX - 3f, sparkY - 3f, 6f, 6f);
            }

            // 5. Ultimate Title & Fighter Name Typography
            float textStartX = portRect.Right + 32f;
            float textY = bandY + 12f;

            // Header line: Character name & awakening
            string charTag = $"{introAttacker.Character.GetName().ToUpper()} // RAGE ART ACTIVATED";
            using (var tagFont = new Font("Segoe UI Black", 12f, FontStyle.Bold))
            using (var tagBrush = new SolidBrush(Color.FromArgb(alpha, Color.FromArgb(0, 210, 255))))
            {
                g.DrawString(charTag, tagFont, tagBrush, textStartX, textY);
            }

            // Giant Skill Name Banner
            if (!string.IsNullOrEmpty(introSkillName))
            {
                string skillTitle = $"【 {introSkillName.ToUpper()} 】";
                using var nameFont = new Font("Segoe UI Black", 28f, FontStyle.Bold | FontStyle.Italic);
                using var nameShadow = new SolidBrush(Color.FromArgb(alpha, Color.Black));
                using var nameFill = new SolidBrush(Color.FromArgb(alpha, Color.Gold));

                g.DrawString(skillTitle, nameFont, nameShadow, textStartX + 3f, textY + 30f);
                g.DrawString(skillTitle, nameFont, nameFill, textStartX, textY + 27f);
            }

            // Flashing subtitle bar: MAXIMUM POWER / BEAT DROP CHARGING
            int subAlpha = (int)Math.Clamp(alpha * (0.6f + 0.4f * MathF.Sin(introFlashPulse * MathF.PI * 12f)), 0, 255);
            string subText = "⚡ MAXIMUM POWER ⚡ >>> UNLEASHING ON BEAT DROP";
            using (var subFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (var subBrush = new SolidBrush(Color.FromArgb(subAlpha, Color.White)))
            {
                g.DrawString(subText, subFont, subBrush, textStartX + 4f, textY + 95f);
            }
        }

        private sealed class BoomEffect
        {
            public float X;
            public float Y;
            public float Progress;
            public Color Color;
        }

        private enum ProjectileType
        {
            Bullet,
            Fireball,
            Meteor,
            ShadowOrb,
            DemonicWave,
            Basketball
        }

        private sealed class BulletProjectile
        {
            public float X;
            public float Y;
            public float VelocityX;
            public float DistanceTraveled;
            public float MaxDistance = 520f;
            public Color Color = Color.Gold;
            public bool IsSpecial;
            public ProjectileType Type = ProjectileType.Bullet;
            public float Radius = 6f;
        }

        private sealed class MuzzleFlash
        {
            public float X;
            public float Y;
            public float Alpha = 1f;
            public float Radius = 14f;
        }

        private readonly List<BulletProjectile> activeBullets = new();
        private readonly List<MuzzleFlash> muzzleFlashes = new();

        private void DrawBullets(Graphics g)
        {
            foreach (var mf in muzzleFlashes)
            {
                int a = (int)Math.Clamp(mf.Alpha * 255f, 0, 255);
                using var b = new SolidBrush(Color.FromArgb(a, Color.FromArgb(255, 230, 80)));
                g.FillEllipse(b, mf.X - mf.Radius, mf.Y - mf.Radius, mf.Radius * 2, mf.Radius * 2);
                using var inner = new SolidBrush(Color.FromArgb(a, Color.White));
                g.FillEllipse(inner, mf.X - mf.Radius * 0.5f, mf.Y - mf.Radius * 0.5f, mf.Radius, mf.Radius);
            }

            foreach (var b in activeBullets)
            {
                float sign = MathF.Sign(b.VelocityX);

                if (b.Type == ProjectileType.Fireball)
                {
                    // Scorching Fireball (Mage Basic)
                    float r = b.Radius > 0 ? b.Radius : 12f;
                    float tailLen = 32f;
                    float tailX = b.X - sign * tailLen;

                    // Fiery tail streak
                    using (var trailPen = new Pen(Color.FromArgb(160, Color.OrangeRed), r * 1.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                        g.DrawLine(trailPen, tailX, b.Y, b.X, b.Y);
                    using (var coreTrail = new Pen(Color.FromArgb(220, Color.Gold), r * 0.7f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                        g.DrawLine(coreTrail, b.X - sign * (tailLen * 0.6f), b.Y, b.X, b.Y);

                    // Outer flame aura
                    using (var flameAura = new SolidBrush(Color.FromArgb(180, Color.DarkOrange)))
                        g.FillEllipse(flameAura, b.X - r, b.Y - r, r * 2f, r * 2f);

                    // Inner bright fire core
                    using (var fireCore = new SolidBrush(Color.Gold))
                        g.FillEllipse(fireCore, b.X - r * 0.65f, b.Y - r * 0.65f, r * 1.3f, r * 1.3f);

                    // Hot white center
                    using (var whiteCenter = new SolidBrush(Color.White))
                        g.FillEllipse(whiteCenter, b.X - r * 0.35f, b.Y - r * 0.35f, r * 0.7f, r * 0.7f);
                }
                else if (b.Type == ProjectileType.Meteor)
                {
                    // Inferno Meteor (Mage Ultimate)
                    float r = b.Radius > 0 ? b.Radius : 24f;
                    float tailLen = 50f;
                    float tailX = b.X - sign * tailLen;

                    // Giant cosmic plasma trail
                    using (var trailPen = new Pen(Color.FromArgb(180, Color.Crimson), r * 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                        g.DrawLine(trailPen, tailX, b.Y, b.X, b.Y);
                    using (var midTrail = new Pen(Color.FromArgb(220, Color.DarkOrange), r * 1.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                        g.DrawLine(midTrail, b.X - sign * (tailLen * 0.65f), b.Y, b.X, b.Y);
                    using (var coreTrail = new Pen(Color.FromArgb(250, Color.Gold), r * 0.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                        g.DrawLine(coreTrail, b.X - sign * (tailLen * 0.35f), b.Y, b.X, b.Y);

                    // Outer cosmic aura
                    using (var cosmicAura = new SolidBrush(Color.FromArgb(170, Color.DarkRed)))
                        g.FillEllipse(cosmicAura, b.X - r * 1.2f, b.Y - r * 1.2f, r * 2.4f, r * 2.4f);

                    // Glowing plasma core
                    using (var plasmaCore = new SolidBrush(Color.OrangeRed))
                        g.FillEllipse(plasmaCore, b.X - r, b.Y - r, r * 2f, r * 2f);

                    // Molten gold layer
                    using (var goldLayer = new SolidBrush(Color.Gold))
                        g.FillEllipse(goldLayer, b.X - r * 0.6f, b.Y - r * 0.6f, r * 1.2f, r * 1.2f);

                    // Superheated white eye
                    using (var whiteEye = new SolidBrush(Color.White))
                        g.FillEllipse(whiteEye, b.X - r * 0.3f, b.Y - r * 0.3f, r * 0.6f, r * 0.6f);
                }
                else if (b.Type == ProjectileType.ShadowOrb)
                {
                    // Disciple Basic: Demonic Shadow Orb (dark purple & violet shadow)
                    float r = b.Radius > 0 ? b.Radius : 13f;
                    float tailLen = 34f;
                    float tailX = b.X - sign * tailLen;

                    // Shadow smoke trail
                    using (var trailPen = new Pen(Color.FromArgb(170, Color.Purple), r * 1.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                        g.DrawLine(trailPen, tailX, b.Y, b.X, b.Y);
                    using (var coreTrail = new Pen(Color.FromArgb(230, Color.Magenta), r * 0.7f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                        g.DrawLine(coreTrail, b.X - sign * (tailLen * 0.6f), b.Y, b.X, b.Y);

                    // Outer shadow aura
                    using (var shadowAura = new SolidBrush(Color.FromArgb(180, Color.DarkSlateBlue)))
                        g.FillEllipse(shadowAura, b.X - r * 1.1f, b.Y - r * 1.1f, r * 2.2f, r * 2.2f);

                    // Inner demonic purple core
                    using (var purpleCore = new SolidBrush(Color.FromArgb(230, 138, 43, 226)))
                        g.FillEllipse(purpleCore, b.X - r * 0.7f, b.Y - r * 0.7f, r * 1.4f, r * 1.4f);

                    // Hot radiant magenta center
                    using (var magCenter = new SolidBrush(Color.FromArgb(255, 255, 105, 180)))
                        g.FillEllipse(magCenter, b.X - r * 0.35f, b.Y - r * 0.35f, r * 0.7f, r * 0.7f);
                }
                else if (b.Type == ProjectileType.DemonicWave)
                {
                    // Disciple Ultimate: Demonic Shadow Slam / Spectral Surge
                    float r = b.Radius > 0 ? b.Radius : 26f;
                    float tailLen = 52f;
                    float tailX = b.X - sign * tailLen;

                    // Deep void trail
                    using (var trailPen = new Pen(Color.FromArgb(190, 48, 10, 72), r * 1.9f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                        g.DrawLine(trailPen, tailX, b.Y, b.X, b.Y);
                    using (var midTrail = new Pen(Color.FromArgb(230, Color.DarkMagenta), r * 1.3f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                        g.DrawLine(midTrail, b.X - sign * (tailLen * 0.65f), b.Y, b.X, b.Y);
                    using (var coreTrail = new Pen(Color.FromArgb(255, Color.Orchid), r * 0.65f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                        g.DrawLine(coreTrail, b.X - sign * (tailLen * 0.35f), b.Y, b.X, b.Y);

                    // Outer demonic void aura
                    using (var voidAura = new SolidBrush(Color.FromArgb(180, 20, 10, 35)))
                        g.FillEllipse(voidAura, b.X - r * 1.25f, b.Y - r * 1.25f, r * 2.5f, r * 2.5f);

                    // Violent purple spectral crest
                    using (var spectralCrest = new SolidBrush(Color.FromArgb(230, 148, 0, 211)))
                        g.FillEllipse(spectralCrest, b.X - r, b.Y - r, r * 2f, r * 2f);

                    // Demonic eye core
                    using (var eyeCore = new SolidBrush(Color.MediumPurple))
                        g.FillEllipse(eyeCore, b.X - r * 0.55f, b.Y - r * 0.55f, r * 1.1f, r * 1.1f);

                    // Bright ethereal center
                    using (var ethWhite = new SolidBrush(Color.White))
                        g.FillEllipse(ethWhite, b.X - r * 0.28f, b.Y - r * 0.28f, r * 0.56f, r * 0.56f);
                }
                else if (b.Type == ProjectileType.Basketball)
                {
                    // Spinning Molten Basketball (Baller Basic)
                    float r = b.Radius > 0 ? b.Radius : 14f;
                    float tailLen = 32f;
                    float tailX = b.X - sign * tailLen;

                    // Motion trail streaks
                    using (var trailPen = new Pen(Color.FromArgb(140, 255, 140, 20), r * 1.1f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                        g.DrawLine(trailPen, tailX, b.Y, b.X, b.Y);
                    using (var trailPen2 = new Pen(Color.FromArgb(200, 255, 210, 60), r * 0.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                        g.DrawLine(trailPen2, b.X - sign * (tailLen * 0.5f), b.Y, b.X, b.Y);

                    // Ball rotation angle based on distance traveled
                    float rotDeg = (b.DistanceTraveled * 12f) * sign;
                    var state = g.Save();
                    g.TranslateTransform(b.X, b.Y);
                    g.RotateTransform(rotDeg);

                    // Orange leather sphere
                    using (var ballBrush = new LinearGradientBrush(new RectangleF(-r, -r, r * 2f, r * 2f), Color.FromArgb(255, 145, 25), Color.FromArgb(195, 65, 5), 45f))
                        g.FillEllipse(ballBrush, -r, -r, r * 2f, r * 2f);

                    // Dark outer outline
                    using (var outlinePen = new Pen(Color.FromArgb(25, 20, 20), 2f))
                        g.DrawEllipse(outlinePen, -r, -r, r * 2f, r * 2f);

                    // Basketball black seam lines
                    using (var seamPen = new Pen(Color.FromArgb(30, 20, 20), 1.8f))
                    {
                        g.DrawLine(seamPen, -r, 0, r, 0);
                        g.DrawLine(seamPen, 0, -r, 0, r);
                        g.DrawArc(seamPen, -r * 0.85f, -r * 0.85f, r * 1.7f, r * 1.7f, 30, 120);
                        g.DrawArc(seamPen, -r * 0.85f, -r * 0.85f, r * 1.7f, r * 1.7f, 210, 120);
                    }

                    // Specular shine highlight
                    using (var glint = new SolidBrush(Color.FromArgb(160, Color.White)))
                        g.FillEllipse(glint, -r * 0.55f, -r * 0.55f, r * 0.5f, r * 0.35f);

                    g.Restore(state);
                }
                else
                {
                    // Standard Gunner bullet
                    float len = b.IsSpecial ? 36f : 24f;
                    float tailX = b.X - sign * len;

                    // Bullet tracer streak
                    using var pen = new Pen(b.IsSpecial ? Color.Magenta : Color.FromArgb(255, 235, 90), b.IsSpecial ? 4.5f : 3f)
                    {
                        StartCap = LineCap.Round,
                        EndCap = LineCap.Round
                    };
                    g.DrawLine(pen, tailX, b.Y, b.X, b.Y);

                    // Bullet core
                    using var headBrush = new SolidBrush(Color.White);
                    g.FillEllipse(headBrush, b.X - 3.5f, b.Y - 3.5f, 7f, 7f);
                }
            }
        }

        private static void DrawGunnerAmmoHud(Graphics g, int x, int y, int w, Gunner gunner, bool alignRight, string reloadHint)
        {
            if (gunner.IsReloading)
            {
                using var font = new Font("Segoe UI Black", 9f, FontStyle.Bold);
                using var brush = new SolidBrush(Color.Gold);
                string text = "⚡ RELOADING... ⚡";
                var sz = g.MeasureString(text, font);
                float tx = alignRight ? x + w - sz.Width : x;
                g.DrawString(text, font, brush, tx, y);
            }
            else
            {
                using var font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                string label = $"AMMO ({gunner.CurrentAmmo}/{gunner.MaxAmmo}) [{reloadHint}]";
                using var brush = new SolidBrush(Color.Gainsboro);
                var sz = g.MeasureString(label, font);
                float lx = alignRight ? x + w - sz.Width : x;
                g.DrawString(label, font, brush, lx, y);

                int pipW = 10, pipH = 5, pipGap = 3;
                float startPipX = alignRight ? lx - (gunner.MaxAmmo * (pipW + pipGap)) - 6 : lx + sz.Width + 8;
                for (int i = 0; i < gunner.MaxAmmo; i++)
                {
                    bool loaded = i < gunner.CurrentAmmo;
                    float px = startPipX + i * (pipW + pipGap);
                    using var pipBrush = new SolidBrush(loaded ? Color.Gold : Color.FromArgb(70, 70, 70));
                    using var pipPen = new Pen(loaded ? Color.Yellow : Color.FromArgb(100, 100, 100), 1f);
                    g.FillRectangle(pipBrush, px, y + 2, pipW, pipH);
                    g.DrawRectangle(pipPen, px, y + 2, pipW, pipH);
                }
            }
        }

        private async Task TriggerManualReload(FighterView view, Gunner gunner)
        {
            if (gunner.IsReloading || gunner.CurrentAmmo >= gunner.MaxAmmo || view.IsAttacking || !gunner.IsAlive()) return;
            gunner.SetReloading(true);
            view.IsAttacking = true;
            view.AttackCooldown = 850f;
            _ = SpawnFloatingText(view, "RELOADING...", Color.Gold, 13f);
            AppendLog($"{gunner.GetName()} begins manual reload...");
            await Task.Delay(800);
            gunner.Reload();
            _ = SpawnFloatingText(view, "RELOADED! 6/6", Color.LimeGreen, 13f);
            view.IsAttacking = false;
            canvas.Invalidate();
        }

        private void AppendLog(string message)
        {
            if (lstLog.IsDisposed) return;
            lstLog.Items.Add(message);
            lstLog.TopIndex = Math.Max(0, lstLog.Items.Count - 6);
        }

        // ---- Real-time attack ----
        private async Task PerformAttack(FighterView attacker, FighterView defender, bool special)
        {
            if (gameOver || attacker.IsAttacking || !attacker.Character.IsAlive() || !defender.Character.IsAlive()) return;
            if (attacker.AttackCooldown > 0f) return;
            if (special && attacker.Rage < RageMax)
            {
                AppendLog($"{attacker.Character.GetName()} needs a full RAGE meter to use a special!");
                return;
            }

            // Gunner ammo & reload check
            bool isGunner = attacker.Character is Gunner;
            bool isMage = attacker.Character is Mage;
            bool isDisciple = attacker.Character is Disciple;
            bool isBaller = attacker.Character is Baller;
            bool isRanged = isGunner || isMage || isDisciple || (isBaller && !special);
            Gunner? gunner = attacker.Character as Gunner;
            Mage? mage = attacker.Character as Mage;
            Disciple? disciple = attacker.Character as Disciple;
            Baller? baller = attacker.Character as Baller;
            if (isGunner && gunner != null)
            {
                if (gunner.IsReloading)
                {
                    AppendLog($"{gunner.GetName()} is reloading! Cannot fire yet.");
                    return;
                }

                if (!special && gunner.CurrentAmmo <= 0)
                {
                    _ = TriggerManualReload(attacker, gunner);
                    return;
                }
            }

            attacker.IsAttacking = true;
            attacker.AttackCooldown = special ? SpecialCooldownMs : BasicCooldownMs;
            attacker.LastAttackWasSpecial = special;

            float reach = isRanged ? GunnerMidRange : (isBaller && special ? 9999f : (special ? SpecialReach : BasicReach));
            float distDelta = defender.PosX - attacker.PosX;
            bool facingTarget = (distDelta * attacker.FacingSign) >= -30f;
            float distance = MathF.Abs(distDelta);
            bool willConnect = (isBaller && special) || (facingTarget && distance <= reach);

            float lungeDist = isRanged ? (isGunner ? -14f : -10f) : (isBaller && special ? (distance - 30f) : (special ? 95f : 70f));
            if (!isRanged && !willConnect) lungeDist *= 0.55f;

            if (special)
            {
                attacker.Rage = 0f;
                Audio.PlaySpecial(attacker.Character.GetType().Name);
                await ShowSpecialIntro(attacker);
            }
            else
            {
                Audio.PlayBasicSwing(attacker.Character.GetType().Name);
            }

            if (isGunner && gunner != null)
            {
                float gunX = attacker.PosX + attacker.FacingSign * 38f;
                float gunY = attacker.GroundY - 78f;

                if (!special)
                {
                    gunner.ConsumeBullet();
                    muzzleFlashes.Add(new MuzzleFlash { X = gunX, Y = gunY });
                    activeBullets.Add(new BulletProjectile
                    {
                        X = gunX,
                        Y = gunY,
                        VelocityX = attacker.FacingSign * 36f,
                        MaxDistance = GunnerMidRange,
                        IsSpecial = false,
                        Color = Color.Gold,
                        Type = ProjectileType.Bullet
                    });
                }
                else
                {
                    // Special: Bullet Storm multi-burst
                    for (int bIdx = 0; bIdx < 4; bIdx++)
                    {
                        muzzleFlashes.Add(new MuzzleFlash { X = gunX, Y = gunY + (bIdx - 1.5f) * 6f });
                        activeBullets.Add(new BulletProjectile
                        {
                            X = gunX,
                            Y = gunY + (bIdx - 1.5f) * 6f,
                            VelocityX = attacker.FacingSign * 38f,
                            MaxDistance = GunnerMidRange,
                            IsSpecial = true,
                            Color = Color.Magenta,
                            Type = ProjectileType.Bullet
                        });
                    }
                    gunner.Reload(); // Refills ammo after bullet storm
                }
            }
            else if (isMage && mage != null)
            {
                float staffX = attacker.PosX + attacker.FacingSign * 34f;
                float staffY = attacker.GroundY - 74f;

                if (!special)
                {
                    // Basic: Scorching Fireball
                    muzzleFlashes.Add(new MuzzleFlash { X = staffX, Y = staffY, Radius = 16f });
                    activeBullets.Add(new BulletProjectile
                    {
                        X = staffX,
                        Y = staffY,
                        VelocityX = attacker.FacingSign * 26f,
                        MaxDistance = GunnerMidRange,
                        IsSpecial = false,
                        Color = Color.OrangeRed,
                        Type = ProjectileType.Fireball,
                        Radius = 13f
                    });
                }
                else
                {
                    // Ultimate: Inferno Meteor
                    muzzleFlashes.Add(new MuzzleFlash { X = staffX, Y = staffY, Radius = 26f });
                    activeBullets.Add(new BulletProjectile
                    {
                        X = staffX,
                        Y = staffY,
                        VelocityX = attacker.FacingSign * 22f,
                        MaxDistance = GunnerMidRange,
                        IsSpecial = true,
                        Color = Color.Crimson,
                        Type = ProjectileType.Meteor,
                        Radius = 26f
                    });
                }
            }
            else if (isDisciple && disciple != null)
            {
                float castX = attacker.PosX + attacker.FacingSign * 34f;
                float castY = attacker.GroundY - 74f;

                if (!special)
                {
                    // Basic: Demonic Shadow Orb
                    muzzleFlashes.Add(new MuzzleFlash { X = castX, Y = castY, Radius = 16f });
                    activeBullets.Add(new BulletProjectile
                    {
                        X = castX,
                        Y = castY,
                        VelocityX = attacker.FacingSign * 26f,
                        MaxDistance = GunnerMidRange,
                        IsSpecial = false,
                        Color = Color.MediumPurple,
                        Type = ProjectileType.ShadowOrb,
                        Radius = 13f
                    });
                }
                if (!special)
                {
                    // Basic: Demonic Shadow Orb
                    muzzleFlashes.Add(new MuzzleFlash { X = castX, Y = castY, Radius = 16f });
                    activeBullets.Add(new BulletProjectile
                    {
                        X = castX,
                        Y = castY,
                        VelocityX = attacker.FacingSign * 26f,
                        MaxDistance = GunnerMidRange,
                        IsSpecial = false,
                        Color = Color.MediumPurple,
                        Type = ProjectileType.ShadowOrb,
                        Radius = 13f
                    });
                }
            }
            else if (isBaller && baller != null)
            {
                float ballX = attacker.PosX + attacker.FacingSign * 34f;
                float ballY = attacker.GroundY - 72f;

                if (!special)
                {
                    // Basic: Shoot spinning basketball projectile
                    muzzleFlashes.Add(new MuzzleFlash { X = ballX, Y = ballY, Radius = 15f });
                    activeBullets.Add(new BulletProjectile
                    {
                        X = ballX,
                        Y = ballY,
                        VelocityX = attacker.FacingSign * 28f,
                        MaxDistance = GunnerMidRange,
                        IsSpecial = false,
                        Color = Color.FromArgb(245, 115, 20),
                        Type = ProjectileType.Basketball,
                        Radius = 14f
                    });
                }
            }

            if (isBaller && special)
            {
                // Target Lock: automatically re-track enemy position anywhere on the field
                float currentDelta = defender.PosX - attacker.PosX;
                if (MathF.Abs(currentDelta) > 1f)
                    attacker.FacingSign = MathF.Sign(currentDelta);

                AppendLog($"🎯 [TARGET LOCK] {attacker.Character.GetName()} locks on {defender.Character.GetName()} — AUTOMATIC HOMING DUNK!");

                // High stratosphere leap into the air
                attacker.VerticalVelocity = JumpPower * 1.55f;

                // Automatic target-tracking flight across the entire screen right above the opponent
                float homeDist = Math.Max(0f, MathF.Abs(currentDelta) - 30f);

                await Tween(250, t =>
                {
                    float e = EaseOutCubic(t);
                    attacker.LungeOffset = homeDist * e;
                    attacker.PunchProgress = e;
                });

                // Meteorite slam dunk touchdown directly on defender
                attacker.VerticalVelocity = -JumpPower * 2.5f;
                attacker.JumpHeight = 0f;
                willConnect = true;
            }
            else if (isDisciple && special)
            {
                // Target lock & auto-aim toward enemy
                float currentDelta = defender.PosX - attacker.PosX;
                if (MathF.Abs(currentDelta) > 1f)
                    attacker.FacingSign = MathF.Sign(currentDelta);

                AppendLog($"🔮 [DEMONIC SURGE] {attacker.Character.GetName()} unleashes an inescapable shadow sphere toward {defender.Character.GetName()}!");

                float castX = attacker.PosX + attacker.FacingSign * 34f;
                float castY = attacker.GroundY - 74f;
                muzzleFlashes.Add(new MuzzleFlash { X = castX, Y = castY, Radius = 35f });

                var demonProj = new BulletProjectile
                {
                    X = castX,
                    Y = castY,
                    VelocityX = attacker.FacingSign * 34f,
                    MaxDistance = 1400f,
                    IsSpecial = true,
                    Color = Color.DarkViolet,
                    Type = ProjectileType.DemonicWave,
                    Radius = 32f
                };
                activeBullets.Add(demonProj);

                // Projectile flies to target
                float distToTarget = MathF.Abs(defender.PosX - castX);
                int travelMs = Math.Clamp((int)(distToTarget / 1.15f), 100, 280);

                await Task.Delay(travelMs);
                activeBullets.Remove(demonProj);

                // Hits and EXPLODES right on the enemy!
                willConnect = true;
            }
            else
            {
                await Lunge(attacker, lungeDist, special ? (isRanged ? 130 : 200) : (isRanged ? 90 : 160));
            }

            if (willConnect)
            {
                if (!special)
                    attacker.Rage = Math.Min(RageMax, attacker.Rage + RageGainPerHit);

                if (defender.IsHoldingBlock)
                {
                    defender.Character.Defend();
                    Audio.PlayBasicBlock();
                }
                else if (!special)
                {
                    Audio.PlayBasicHit(attacker.Character.GetType().Name);
                }

                canvasFlash = 1f;
                canvas.Invalidate();
                await HitStop(special ? 110 : 35);
                _ = Tween(180, t => canvasFlash = 1f - t);
                _ = PunchZoom(new PointF(defender.PosX, defender.GroundY - 90f), special ? 320 : 160, special ? 0.18f : 0.08f);

                if (special)
                {
                    Color boomColor = isMage ? Color.OrangeRed : (isDisciple ? Color.DarkViolet : (isBaller ? Color.FromArgb(255, 120, 20) : defender.TeamColor));
                    _ = SpawnBoom(defender.PosX, defender.GroundY - 90f, boomColor);
                    if (isDisciple)
                    {
                        AppendLog($"💥 [DEMONIC DETONATION] The demonic shadow sphere explodes with catastrophic force upon {defender.Character.GetName()}!");
                        _ = SpawnFloatingText(defender, "💥 DEMONIC DETONATION! 💥", Color.DarkViolet, 20f);
                        _ = SpawnBoom(defender.PosX, defender.GroundY - 90f, Color.DarkViolet);
                        _ = SpawnBoom(defender.PosX + 38f, defender.GroundY - 70f, Color.MediumPurple);
                        _ = SpawnBoom(defender.PosX - 38f, defender.GroundY - 70f, Color.Crimson);
                        _ = SpawnBoom(defender.PosX, defender.GroundY - 140f, Color.DarkViolet);
                        _ = SpawnBoom(defender.PosX + 22f, defender.GroundY - 110f, Color.Magenta);
                        _ = SpawnBoom(defender.PosX - 22f, defender.GroundY - 110f, Color.White);
                    }
                    else if (isBaller)
                    {
                        _ = SpawnFloatingText(defender, "💥 TARGET LOCKED! POSTER DUNK! 💥", Color.Gold, 18f);
                        _ = SpawnBoom(defender.PosX + 25f, defender.GroundY - 30f, Color.Gold);
                        _ = SpawnBoom(defender.PosX - 25f, defender.GroundY - 30f, Color.OrangeRed);
                    }
                }

                var hits = new List<HitInfo>();
                void Collect(HitInfo h) => hits.Add(h);
                defender.Character.OnHit += Collect;

                if (special)
                    attacker.Character.SpecialAttack(defender.Character);
                else
                    attacker.Character.BasicAttack(defender.Character);

                defender.Character.OnHit -= Collect;

                for (int i = 0; i < hits.Count; i++)
                {
                    var hit = hits[i];
                    _ = SpawnFloatingText(defender, hit);
                    await HitReact(defender, big: special);
                    await AnimateHpTo(defender, hit.HpAfter);
                    if (i < hits.Count - 1)
                        await Task.Delay(90);
                }
            }
            else
            {
                if (isGunner)
                    AppendLog($"{attacker.Character.GetName()} fires but {defender.Character.GetName()} is out of mid-range — bullet misses!");
                else if (isMage)
                    AppendLog($"{attacker.Character.GetName()} casts fire but {defender.Character.GetName()} is out of mid-range — fireball dissipates!");
                else if (isDisciple)
                    AppendLog($"{attacker.Character.GetName()} unleashes shadow magic but {defender.Character.GetName()} is out of mid-range — shadow dissipates!");
                else if (isBaller)
                    AppendLog($"{attacker.Character.GetName()} shoots the basketball but {defender.Character.GetName()} is out of range — air ball!");
                else
                    AppendLog($"{attacker.Character.GetName()} swings but {defender.Character.GetName()} is too far away — misses!");
            }

            await LungeBack(attacker, special ? (isRanged ? 110 : 170) : (isRanged ? 80 : 140));
            attacker.IsAttacking = false;
            canvas.Invalidate();

            if (p1Char != null && p2Char != null && (!p1Char.IsAlive() || !p2Char.IsAlive()))
                await EndGame();
        }

        private static float EaseOutCubic(float t) => 1f - MathF.Pow(1f - t, 3f);
        private static float EaseInCubic(float t) => t * t * t;

        private async Task Tween(int durationMs, Action<float> onProgress)
        {
            var sw = Stopwatch.StartNew();
            while (true)
            {
                float t = durationMs <= 0 ? 1f : Math.Min((float)sw.ElapsedMilliseconds / durationMs, 1f);
                onProgress(t);
                canvas.Invalidate();
                if (t >= 1f) break;
                await Task.Delay(15);
            }
        }

        private Task Lunge(FighterView v, float distance, int durationMs) =>
            Tween(durationMs, t =>
            {
                float e = EaseOutCubic(t);
                v.LungeOffset = distance * e;
                v.PunchProgress = e;
            });

        private Task LungeBack(FighterView v, int durationMs)
        {
            float start = v.LungeOffset;
            return Tween(durationMs, t =>
            {
                float e = EaseInCubic(t);
                v.LungeOffset = start * (1f - e);
                v.PunchProgress = 1f - e;
            });
        }

        private Task HitReact(FighterView v, bool big) =>
            Tween(big ? 380 : 260, t =>
            {
                float magnitude = big ? 12f : 8f;
                v.ShakeOffset = MathF.Sin(t * MathF.PI * 8f) * magnitude * (1f - t);
                v.FlashIntensity = 1f - t;
            });

        private Task AnimateHpTo(FighterView v, int targetHp)
        {
            float start = v.DisplayHp;
            float target = targetHp;
            if (MathF.Abs(start - target) < 0.5f)
            {
                v.DisplayHp = target;
                return Task.CompletedTask;
            }
            return Tween(300, t => v.DisplayHp = start + (target - start) * EaseOutCubic(t));
        }

        private async Task FlashScreen()
        {
            await Tween(120, t => canvasFlash = 1f - t);
            canvasFlash = 0f;
            canvas.Invalidate();
        }

        private async Task ShowBanner(string text)
        {
            bannerText = text;
            await Tween(160, t =>
            {
                bannerAlpha = t;
                bannerScale = 0.55f + 0.55f * EaseOutCubic(t);
            });
            await Task.Delay(260);
            await Tween(200, t => bannerAlpha = 1f - t);
            bannerText = "";
        }

        // The full arcade-style super intro: dim in, pop the portrait card
        // with a strobing glow, hold on the move name, then pop back out.
        // Awaited (not fire-and-forget) so the actual attack/lunge waits
        // for the cinematic to finish before it resolves.
        private void LoadCutInFrames(string type, int targetHeight = 220)
        {
            foreach (var b in introFrames) b.Dispose();
            introFrames.Clear();

            string cutinPath = Path.Combine(AppContext.BaseDirectory, "Sprites", type, "cutin.png");
            string path = File.Exists(cutinPath) ? cutinPath : Path.Combine(AppContext.BaseDirectory, "Sprites", type, "special.png");
            if (!File.Exists(path)) path = Path.Combine(AppContext.BaseDirectory, "Sprites", type, "idle.png");
            if (!File.Exists(path)) return;

            try
            {
                using var full = Image.FromFile(path);
                int frameCount = 1;
                var frameMap = FrameCountsFor(type);
                if (path == cutinPath)
                    frameCount = 6;
                else if (path.Contains("special") && frameMap.TryGetValue(AnimState.Special, out int sCount))
                    frameCount = Math.Max(1, sCount);
                else if (frameMap.TryGetValue(AnimState.Idle, out int iCount))
                    frameCount = Math.Max(1, iCount);

                int frameW = Math.Max(1, full.Width / frameCount);
                int frameH = full.Height;

                float scale = targetHeight / (float)frameH;
                int destW = Math.Max(1, (int)(frameW * scale));

                for (int i = 0; i < frameCount; i++)
                {
                    var bmp = new Bitmap(destW, targetHeight);
                    using var g = Graphics.FromImage(bmp);
                    g.InterpolationMode = path == cutinPath ? InterpolationMode.HighQualityBicubic : InterpolationMode.NearestNeighbor;
                    g.PixelOffsetMode = PixelOffsetMode.Half;
                    g.DrawImage(full, new Rectangle(0, 0, destW, targetHeight), new Rectangle(i * frameW, 0, frameW, frameH), GraphicsUnit.Pixel);
                    introFrames.Add(bmp);
                }
            }
            catch { }
        }

        private async Task ShowSpecialIntro(FighterView attacker)
        {
            introAttacker = attacker;
            introSkillName = attacker.Character.GetSpecialName();
            introFlashPulse = 0f;
            introSlideIn = 0f;
            LoadCutInFrames(attacker.Character.GetType().Name, 220);

            // Phase 1: Rapid rush in from left with bright energy flash (350ms)
            canvasFlash = 0.85f;
            await Tween(350, t =>
            {
                introDim = t;
                introCardAlpha = t;
                introSlideIn = EaseOutCubic(t);
                introCardScale = 0.85f + 0.15f * EaseOutCubic(t);
            });

            // Phase 2: Hold on screen, aura & speed lines pulsing with the sound build-up (~2150ms)
            await Tween(2150, t =>
            {
                introFlashPulse = t;
                // Charging energy rumble on the stage as ultimate power surges
                attacker.ShakeOffset = MathF.Sin(t * MathF.PI * 24f) * (2f + t * 4f);
            });

            // Phase 3: Slashes forward with bright flash, transitioning right into the hit impact on the beat drop (350ms)
            canvasFlash = 1f;
            attacker.ShakeOffset = 0f;
            await Tween(350, t =>
            {
                float e = 1f - t;
                introCardAlpha = e;
                introSlideIn = 1f + t * 0.7f;
                introDim = e;
            });

            foreach (var b in introFrames) b.Dispose();
            introFrames.Clear();
            introAttacker = null;
            introSkillName = "";
            introSlideIn = 0f;
        }

        // ---- Practice loop: press T in battle to repeat the hit-stop +
        // zoom + boom beat on its own, with no combat needed, so you can
        // sit and audition .wav files against it. Audio.PlaySpecial()
        // reloads the file fresh every call — replace the .wav directly in
        // bin/.../Sounds/Special/ while this is running to hear a new take
        // instantly; editing the one under the project's Sounds/ folder
        // needs a rebuild (stop and dotnet run again) to get re-copied.
        private async void TogglePracticeLoop()
        {
            practiceLoopActive = !practiceLoopActive;
            AppendLog(practiceLoopActive
                ? "Practice loop ON (T to stop) — looping the impact beat for sound sync."
                : "Practice loop OFF.");

            while (practiceLoopActive && pnlBattle.Visible)
            {
                await PlayPracticeImpact();
                await Task.Delay(700); // breathing room between beats
            }
            practiceLoopActive = false;
        }

        private async Task PlayPracticeImpact()
        {
            if (p1View == null || p2View == null) return;

            var center = new PointF(canvas.Width / 2f, canvas.Height * GroundYRatio - 90f);
            string voiceType = p1Char?.GetType().Name ?? "Warrior";

            Audio.PlaySpecial(voiceType);
            canvasFlash = 1f;
            canvas.Invalidate();
            await HitStop(110);
            _ = Tween(180, t => canvasFlash = 1f - t);
            _ = PunchZoom(center, 320, 0.18f);
            await SpawnBoom(center.X, center.Y, Color.Gold);
        }

        // Spawns the expanding shockwave right where a special attack lands.
        private async Task SpawnBoom(float x, float y, Color color)
        {
            var boom = new BoomEffect { X = x, Y = y, Color = color };
            boomEffects.Add(boom);
            await Tween(480, t => boom.Progress = t);
            boomEffects.Remove(boom);
        }

        // Pauses the whole live simulation (movement, jumping, cooldowns —
        // everything gameTimer drives) for a beat. Scripted attack tweens
        // (Lunge, HitReact, this very freeze) run on their own Stopwatch and
        // keep working normally, so the frozen frame still paints correctly.
        private async Task HitStop(int ms)
        {
            gameTimer.Stop();
            await Task.Delay(ms);
            gameTimer.Start();
        }

        // Snaps the camera in on the impact point, then eases back out —
        // a quick "punch" that sells the weight of a hit.
        private async Task PunchZoom(PointF center, int ms, float strength)
        {
            zoomCenter = center;
            zoomPunch = strength;
            await Tween(ms, t => zoomPunch = strength * (1f - EaseOutCubic(t)));
            zoomPunch = 0f;
        }

        private async Task SpawnFloatingText(FighterView v, HitInfo hit)
        {
            string text = hit.Blocked ? $"BLOCKED -{hit.ActualDamage}" : $"-{hit.ActualDamage}";
            Color color = hit.Blocked ? Color.DeepSkyBlue : (hit.ActualDamage >= 15 ? Color.Orange : Color.White);
            float fontSize = hit.ActualDamage >= 15 ? 20f : 15f;

            var ft = new FloatingText { Text = text, X = v.PosX, Y = v.GroundY - 150f, Color = color, FontSize = fontSize };
            floatingTexts.Add(ft);
            float startY = ft.Y;
            await Tween(650, t =>
            {
                ft.Y = startY - 42f * t;
                ft.Alpha = 1f - t;
            });
            floatingTexts.Remove(ft);
        }

        private async Task SpawnFloatingText(FighterView v, string text, Color color, float fontSize = 15f)
        {
            var ft = new FloatingText { Text = text, X = v.PosX, Y = v.GroundY - 150f, Color = color, FontSize = fontSize };
            floatingTexts.Add(ft);
            float startY = ft.Y;
            await Tween(650, t =>
            {
                ft.Y = startY - 42f * t;
                ft.Alpha = 1f - t;
            });
            floatingTexts.Remove(ft);
        }

        private sealed class FloatingText
        {
            public string Text = "";
            public float X;
            public float Y;
            public Color Color;
            public float Alpha = 1f;
            public float FontSize = 15f;
        }

        // =========================================================
        // Screen 4 — KO / result
        // =========================================================
        private void BuildResultPanel()
        {
            pnlResult.Dock = DockStyle.Fill;
            pnlResult.BackColor = Color.Black;

            lblKO.Text = "K . O . !";
            lblKO.ForeColor = Color.Red;
            lblKO.Font = new Font("Segoe UI Black", 40f, FontStyle.Bold);
            lblKO.AutoSize = true;
            lblKO.Location = new Point(280, 220);

            lblWinner.ForeColor = Color.Gold;
            lblWinner.Font = new Font("Segoe UI", 18f, FontStyle.Bold);
            lblWinner.AutoSize = true;
            lblWinner.Location = new Point(280, 300);

            btnRematch.Text = "REMATCH";
            btnRematch.Size = new Size(180, 46);
            btnRematch.Location = new Point(280, 400);
            btnRematch.BackColor = Color.Firebrick;
            btnRematch.ForeColor = Color.White;
            btnRematch.FlatStyle = FlatStyle.Flat;
            btnRematch.FlatAppearance.BorderSize = 0;
            btnRematch.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnRematch.Cursor = Cursors.Hand;
            btnRematch.MouseEnter += (s, e) => btnRematch.BackColor = Color.FromArgb(210, 60, 45);
            btnRematch.MouseLeave += (s, e) => btnRematch.BackColor = Color.Firebrick;
            btnRematch.Click += (s, e) => ShowOnly(pnlCharSelect);

            btnExit.Text = "EXIT";
            btnExit.Size = new Size(180, 46);
            btnExit.Location = new Point(480, 400);
            btnExit.BackColor = Color.FromArgb(50, 55, 80);
            btnExit.ForeColor = Color.White;
            btnExit.FlatStyle = FlatStyle.Flat;
            btnExit.FlatAppearance.BorderSize = 0;
            btnExit.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnExit.Cursor = Cursors.Hand;
            btnExit.MouseEnter += (s, e) => btnExit.BackColor = Color.FromArgb(75, 80, 105);
            btnExit.MouseLeave += (s, e) => btnExit.BackColor = Color.FromArgb(50, 55, 80);
            btnExit.Click += (s, e) => Close();

            pnlResult.Controls.Add(lblKO);
            pnlResult.Controls.Add(lblWinner);
            pnlResult.Controls.Add(btnRematch);
            pnlResult.Controls.Add(btnExit);
        }

        private async Task EndGame()
        {
            gameOver = true;

            if (p1Char == null || p2Char == null) return;

            if (!p1Char.IsAlive() && !p2Char.IsAlive())
            {
                lblKO.Text = "DOUBLE K.O.";
                lblWinner.Text = "DRAW";
            }
            else
            {
                var winner = p1Char.IsAlive() ? p1Char : p2Char;
                lblKO.Text = "K . O . !";
                lblWinner.Text = $"{winner.GetName().ToUpper()} WINS!";
            }

            await Task.Delay(900);
            ShowOnly(pnlResult);
        }

        private void ShowOnly(Panel panel)
        {
            pnlStartMenu.Visible = panel == pnlStartMenu;
            pnlCharSelect.Visible = panel == pnlCharSelect;
            pnlVersus.Visible = panel == pnlVersus;
            pnlBattle.Visible = panel == pnlBattle;
            pnlResult.Visible = panel == pnlResult;
            panel.Dock = DockStyle.Fill;
            panel.BringToFront();
            LayoutAllScreens();
        }

        private void LayoutAllScreens()
        {
            LayoutCharSelectPanel();
            LayoutVersusPanel();
            LayoutBattlePanel();
            LayoutResultPanel();
            startMenuCanvas.Invalidate();
        }

        private void LayoutCharSelectPanel()
        {
            int w = pnlCharSelect.ClientSize.Width;
            int h = pnlCharSelect.ClientSize.Height;
            if (w <= 0 || h <= 0) return;

            btnBackMenu.Location = new Point(24, 25);
            lblSelectTitle.Location = new Point((w - lblSelectTitle.Width) / 2, 25);

            const int tileW = 150, gap = 20;
            int totalTilesW = tileW * RosterNames.Length + gap * (RosterNames.Length - 1);
            int startX = Math.Max(24, (w - totalTilesW) / 2);

            lblP1.Location = new Point(startX, 90);
            lblP2.Location = new Point(startX, 335);

            for (int i = 0; i < RosterNames.Length; i++)
            {
                string type = RosterNames[i];
                int x = startX + i * (tileW + gap);
                if (p1Tiles.TryGetValue(type, out var p1b)) p1b.Location = new Point(x, 125);
                if (p2Tiles.TryGetValue(type, out var p2b)) p2b.Location = new Point(x, 370);
            }

            btnFight.Location = new Point(startX, 580);
            btnFight.Size = new Size(totalTilesW, 48);
        }

        private void LayoutVersusPanel()
        {
            int w = pnlVersus.ClientSize.Width;
            int h = pnlVersus.ClientSize.Height;
            if (w <= 0 || h <= 0) return;

            lblVersusText.Size = new Size(w, 60);
            lblVersusText.Location = new Point(0, (int)(h * 0.38f));

            lblFightFlash.Size = new Size(w, 90);
            lblFightFlash.Location = new Point(0, (int)(h * 0.52f));
        }

        private void LayoutBattlePanel()
        {
            int w = pnlBattle.ClientSize.Width;
            int h = pnlBattle.ClientSize.Height;
            if (w <= 0 || h <= 0) return;

            int logH = Math.Clamp(h - 590, 110, 180);
            int canvasH = Math.Max(380, h - (logH + 115));

            canvas.Location = new Point(0, 0);
            canvas.Size = new Size(w, canvasH);

            lblControls.Location = new Point(20, canvasH + 8);
            btnStatus.Location = new Point(20, lblControls.Bottom + 6);
            lblLog.Location = new Point(20, btnStatus.Bottom + 8);
            lstLog.Location = new Point(20, lblLog.Bottom + 4);
            lstLog.Size = new Size(w - 40, Math.Max(60, h - lstLog.Top - 15));

            canvas.Invalidate();
        }

        private void LayoutResultPanel()
        {
            int w = pnlResult.ClientSize.Width;
            int h = pnlResult.ClientSize.Height;
            if (w <= 0 || h <= 0) return;

            int cx = w / 2;
            lblKO.Location = new Point(cx - lblKO.Width / 2, (int)(h * 0.28f));
            lblWinner.Location = new Point(cx - lblWinner.Width / 2, (int)(h * 0.40f));
            btnRematch.Location = new Point(cx - 190, (int)(h * 0.56f));
            btnExit.Location = new Point(cx + 10, (int)(h * 0.56f));
        }

        // DoubleBuffered is a protected member of Control, so a plain Panel
        // can't have it turned on from outside — this tiny subclass just
        // exposes it to stop the canvas from flickering while it redraws.
        private sealed class BufferedPanel : Panel
        {
            public BufferedPanel()
            {
                DoubleBuffered = true;
            }
        }
    }
}

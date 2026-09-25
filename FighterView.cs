using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace FightingGame
{
    // Everything needed to draw and animate one fighter on screen.
    // Talks to Character only through its public getters — same rule as BattleUI.
    public class FighterView
    {
        private const float HeadRadius = 17f;
        private const float BodyLength = 55f;
        private const float ArmLength = 34f;
        private const float LegLength = 42f;

        public Character Character { get; }
        public float GroundY { get; }
        public Color TeamColor { get; }
        public int FacingSign { get; set; } // +1 faces right, -1 faces left — updated every frame to always face the opponent

        // Live simulation state — driven every frame by the movement/jump
        // loop in BattleForm (WASD / arrow keys), separate from the
        // attack-animation tweens below.
        public float PosX;             // current horizontal position (walking moves this)
        public float JumpHeight;       // 0 = grounded, positive = height above ground
        public float VerticalVelocity; // current vertical speed (jump/gravity)

        // Real-time combat state.
        public bool IsAttacking;       // locked into an attack animation right now
        public float AttackCooldown;   // ms remaining before this fighter can attack again
        public bool IsHoldingBlock;    // block key currently held down
        public bool LastAttackWasSpecial; // which anim (Attack vs Special) to show while IsAttacking
        public float Rage;             // 0..100 — builds from landed basic attacks, spent entirely on a Special

        // Animation state, driven by tweens in BattleForm.
        public float LungeOffset;      // horizontal offset while attacking (in FacingSign direction)
        public float ShakeOffset;      // small jitter offset when hit
        public float PunchProgress;    // 0 = idle arms, 1 = fully extended punch
        public float GuardProgress;    // 0 = idle, 1 = arms fully crossed in guard
        public float FlashIntensity;   // 0..1 red "just got hit" flash
        public float DisplayHp;        // animated HP value shown on the bar (tweens toward real HP)

        // Optional sprite art — null (or incomplete) means "keep using the
        // procedural stick figure below". Drop PNGs in Sprites/P1 or
        // Sprites/P2 and this switches on automatically, no code changes.
        public SpriteSet? Sprites;
        private AnimState currentAnim = AnimState.Idle;
        private int frameIndex;
        private float frameTimer;

        public FighterView(Character character, float startX, float groundY, Color teamColor, int facingSign)
        {
            Character = character;
            PosX = startX;
            GroundY = groundY;
            TeamColor = teamColor;
            FacingSign = facingSign;
            DisplayHp = character.GetHP();
        }

        // Called once per tick by BattleForm with whatever state the fighter
        // should currently be showing (idle/walk/jump/attack/etc).
        public void UpdateAnimation(float dtMs, AnimState desiredState)
        {
            if (Sprites == null || !Sprites.IsComplete) return;

            if (desiredState != currentAnim)
            {
                currentAnim = desiredState;
                frameIndex = 0;
                frameTimer = 0f;
            }

            var strip = Sprites.Get(currentAnim);
            if (strip == null) return;

            frameTimer += dtMs;
            if (frameTimer >= Sprites.FrameDurationMs)
            {
                frameTimer = 0f;
                frameIndex++;
                bool loop = currentAnim is AnimState.Idle or AnimState.Walk or AnimState.Special;
                if (frameIndex >= strip.FrameCount)
                    frameIndex = loop ? 0 : strip.FrameCount - 1;
            }
        }

        public void Draw(Graphics g)
        {
            if (Sprites != null && Sprites.IsComplete && Sprites.Get(currentAnim) is { } strip)
            {
                DrawSprite(g, strip);
                return;
            }

            DrawStickFigure(g);
        }

        private void DrawSprite(Graphics g, SpriteStrip strip)
        {
            float x = PosX + LungeOffset * FacingSign + ShakeOffset;
            float feetY = GroundY - JumpHeight;

            float shadowScale = Math.Max(0.35f, 1f - JumpHeight / 140f);
            float shadowW = 34f * shadowScale;
            using (var shadowBrush = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
                g.FillEllipse(shadowBrush, x - shadowW / 2f, GroundY - 6f, shadowW, 12f * shadowScale);

            float destW = strip.FrameWidth * Sprites!.Scale;
            float destH = strip.FrameHeight * Sprites.Scale;

            if (currentAnim == AnimState.Special)
            {
                // Pulsing charging aura around the fighter while casting ultimate
                float auraPulse = 0.5f + 0.5f * MathF.Sin(frameIndex * 1.5f + frameTimer * 0.05f);
                int a = (int)(80 + 90 * auraPulse);
                using (var auraBrush = new SolidBrush(Color.FromArgb(a, TeamColor)))
                {
                    g.FillEllipse(auraBrush, x - destW * 0.55f, feetY - destH * 0.95f, destW * 1.1f, destH * 1.05f);
                }

                // Inner radiant core
                using (var innerBrush = new SolidBrush(Color.FromArgb((int)(a * 0.6f), Color.White)))
                {
                    g.FillEllipse(innerBrush, x - destW * 0.35f, feetY - destH * 0.75f, destW * 0.7f, destH * 0.8f);
                }

                // Electric spark arcs crackling around fighter
                using var sparkPen = new Pen(Color.Gold, 2f);
                for (int sp = 0; sp < 4; sp++)
                {
                    float angle = (sp * 90f + frameIndex * 45f) * MathF.PI / 180f;
                    float sx = x + MathF.Cos(angle) * destW * 0.42f;
                    float sy = feetY - destH * 0.5f + MathF.Sin(angle) * destH * 0.38f;
                    g.DrawLine(sparkPen, sx - 4f, sy - 4f, sx + 4f, sy + 4f);
                    g.DrawLine(sparkPen, sx - 4f, sy + 4f, sx + 4f, sy - 4f);
                }
            }

            var destRect = new RectangleF(x - destW / 2f, feetY - destH, destW, destH);
            var srcRect = new Rectangle(frameIndex * strip.FrameWidth, 0, strip.FrameWidth, strip.FrameHeight);

            var prevInterp = g.InterpolationMode;
            g.InterpolationMode = InterpolationMode.NearestNeighbor; // keep pixel art crisp, no blur

            var saved = g.Save();
            if (FacingSign < 0)
            {
                g.TranslateTransform(destRect.Right, destRect.Top);
                g.ScaleTransform(-1f, 1f);
                g.DrawImage(strip.Image, new RectangleF(0, 0, destRect.Width, destRect.Height), srcRect, GraphicsUnit.Pixel);
            }
            else
            {
                g.DrawImage(strip.Image, destRect, srcRect, GraphicsUnit.Pixel);
            }
            g.Restore(saved);
            g.InterpolationMode = prevInterp;
        }

        private void DrawStickFigure(Graphics g)
        {
            float x = PosX + LungeOffset * FacingSign + ShakeOffset;
            float feetY = GroundY - JumpHeight;
            float hipY = feetY - LegLength;
            float shoulderY = hipY - BodyLength;
            float headCenterY = shoulderY - HeadRadius - 3f;

            bool alive = Character.IsAlive();

            // Ground shadow — shrinks as the fighter jumps higher, gives a cheap sense of depth.
            float shadowScale = Math.Max(0.35f, 1f - JumpHeight / 140f);
            float shadowW = 34f * shadowScale;
            using (var shadowBrush = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
                g.FillEllipse(shadowBrush, x - shadowW / 2f, GroundY - 6f, shadowW, 12f * shadowScale);

            using var bodyPen = new Pen(alive ? Color.Black : Color.Gray, 4f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            using var teamBrush = new SolidBrush(alive ? TeamColor : Color.Silver);

            // Legs — a slight forward stance, front leg widens a bit while lunging.
            float stride = 11f + PunchProgress * 5f;
            g.DrawLine(bodyPen, x, hipY, x - stride, feetY);
            g.DrawLine(bodyPen, x, hipY, x + stride * 0.6f, feetY);

            // Torso.
            g.DrawLine(bodyPen, x, hipY, x, shoulderY);

            // Arms.
            if (GuardProgress > 0.01f)
            {
                // Crossed guard: both arms raised in front of the chest.
                float gx = x + FacingSign * (6f + 10f * GuardProgress);
                g.DrawLine(bodyPen, x, shoulderY, gx, shoulderY - 14f * GuardProgress - 6f);
                g.DrawLine(bodyPen, x, shoulderY, gx, shoulderY + 10f * GuardProgress - 4f);
            }
            else
            {
                // Back arm, idle.
                g.DrawLine(bodyPen, x, shoulderY, x - FacingSign * 12f, shoulderY + 18f);
                // Front arm — extends for the punch.
                float reach = 12f + ArmLength * PunchProgress;
                float armDrop = 14f * (1f - PunchProgress); // straightens out as it extends
                g.DrawLine(bodyPen, x, shoulderY, x + FacingSign * reach, shoulderY + armDrop);
            }

            // Head.
            var headRect = new RectangleF(x - HeadRadius, headCenterY - HeadRadius, HeadRadius * 2, HeadRadius * 2);
            g.FillEllipse(teamBrush, headRect);
            g.DrawEllipse(bodyPen, headRect);

            // Hit flash — a soft red halo that fades out.
            if (FlashIntensity > 0.01f)
            {
                int alpha = (int)Math.Clamp(FlashIntensity * 140, 0, 140);
                using var flashBrush = new SolidBrush(Color.FromArgb(alpha, Color.Red));
                var flashRect = new RectangleF(x - HeadRadius - 14f, headCenterY - HeadRadius - 10f, (HeadRadius + 14f) * 2, (feetY - headCenterY) + HeadRadius + 10f);
                g.FillEllipse(flashBrush, flashRect);
            }

            // KO'd — draw an X over the head instead of removing the fighter.
            if (!alive)
            {
                using var xPen = new Pen(Color.DarkRed, 3f);
                g.DrawLine(xPen, headRect.Left, headRect.Top, headRect.Right, headRect.Bottom);
                g.DrawLine(xPen, headRect.Right, headRect.Top, headRect.Left, headRect.Bottom);
            }

            // Guard indicator — lit while the block key is held (real-time feedback),
            // and also right after a hit was actually blocked.
            if (alive && (IsHoldingBlock || Character.IsDefending()))
            {
                using var guardPen = new Pen(Color.FromArgb(200, 90, 220, 255), 2.5f);
                var ringRect = new RectangleF(headRect.Left - 6f, headRect.Top - 6f, headRect.Width + 12f, headRect.Height + 12f);
                g.DrawEllipse(guardPen, ringRect);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace FightingGame
{
    // One horizontal filmstrip PNG for a single animation (e.g. "walk.png"
    // with N frames side by side, all the same height).
    public sealed class SpriteStrip
    {
        public Image Image { get; }
        public int FrameCount { get; }
        public int FrameWidth => Image.Width / Math.Max(1, FrameCount);
        public int FrameHeight => Image.Height;

        public SpriteStrip(Image image, int frameCount)
        {
            Image = image;
            FrameCount = Math.Max(1, frameCount);
        }
    }

    // A full set of animation strips for one character, loaded from a
    // folder. Falls back to "not complete" (procedural stick figure keeps
    // drawing) until every required file is present.
    public sealed class SpriteSet
    {
        // How many frames are in each strip file, left-to-right.
        // EDIT THESE to match whatever you actually export for each
        // animation — this is the one thing you can't skip, since a sprite
        // sheet you download won't necessarily match these numbers.
        // Used as the fallback when TryLoad isn't given its own counts
        // (handy when both fighters share one sheet's layout).
        public static readonly Dictionary<AnimState, int> DefaultFrameCounts = new()
        {
            { AnimState.Idle, 4 },
            { AnimState.Walk, 6 },
            { AnimState.Jump, 1 },
            { AnimState.Attack, 3 },
            { AnimState.Special, 4 },
            { AnimState.Block, 1 },
            { AnimState.Hit, 1 },
            { AnimState.KO, 1 },
        };

        // Frame counts for the Samurai art pack (now used by Ninja).
        public static readonly Dictionary<AnimState, int> SamuraiFrameCounts = new()
        {
            { AnimState.Idle, 3 },
            { AnimState.Walk, 5 },
            { AnimState.Jump, 1 },
            { AnimState.Attack, 3 },
            { AnimState.Special, 6 },
            { AnimState.Block, 3 },
            { AnimState.Hit, 1 },
            { AnimState.KO, 1 },
        };

        // Frame counts for the Fumiko art pack (now used by Warrior).
        public static readonly Dictionary<AnimState, int> FumikoFrameCounts = new()
        {
            { AnimState.Idle, 8 },
            { AnimState.Walk, 8 },
            { AnimState.Jump, 1 },
            { AnimState.Attack, 8 },
            { AnimState.Special, 8 },
            { AnimState.Block, 8 },
            { AnimState.Hit, 8 },
            { AnimState.KO, 8 },
        };

        // Frame counts for the Mage sheets (mage-1 idle, mage-2 attack/staff
        // cast, mage-3 special/tendril burst). Walk/Jump/Block/Hit/KO reuse
        // idle frames as placeholders since the pack has no dedicated art
        // for those states.
        public static readonly Dictionary<AnimState, int> MageFrameCounts = new()
        {
            { AnimState.Idle, 8 },
            { AnimState.Walk, 8 },
            { AnimState.Jump, 1 },
            { AnimState.Attack, 8 },
            { AnimState.Special, 8 },
            { AnimState.Block, 1 },
            { AnimState.Hit, 1 },
            { AnimState.KO, 1 },
        };

        // Frame counts for the Disciple sheet (idle/attack/ko are its own
        // 3 rows; special borrows the "shadow" monster's lunge row as a
        // summon effect since Disciple has no special animation of its own).
        public static readonly Dictionary<AnimState, int> DiscipleFrameCounts = new()
        {
            { AnimState.Idle, 4 },
            { AnimState.Walk, 4 },
            { AnimState.Jump, 1 },
            { AnimState.Attack, 4 },
            { AnimState.Special, 4 },
            { AnimState.Block, 1 },
            { AnimState.Hit, 1 },
            { AnimState.KO, 4 },
        };

        // Frame counts for the Gunner sheet (OpenGameArt "Cowboy" pack,
        // CC0 by software_atelier). Attack = standing shoot, Special =
        // jump-shoot for a flashier "ultimate" pose. Block/Hit/KO reuse
        // idle frames — the pack has no dedicated art for those states.
        public static readonly Dictionary<AnimState, int> GunnerFrameCounts = new()
        {
            { AnimState.Idle, 4 },
            { AnimState.Walk, 4 },
            { AnimState.Jump, 1 },
            { AnimState.Attack, 4 },
            { AnimState.Special, 4 },
            { AnimState.Block, 4 },
            { AnimState.Hit, 1 },
            { AnimState.KO, 1 },
        };

        // Frame counts for Baller (Basketball player).
        public static readonly Dictionary<AnimState, int> BallerFrameCounts = new()
        {
            { AnimState.Idle, 4 },
            { AnimState.Walk, 4 },
            { AnimState.Jump, 1 },
            { AnimState.Attack, 4 },
            { AnimState.Special, 6 },
            { AnimState.Block, 2 },
            { AnimState.Hit, 1 },
            { AnimState.KO, 2 },
        };

        private readonly Dictionary<AnimState, SpriteStrip> strips = new();

        // Frames per second used to play every strip. Idle/Walk loop;
        // everything else plays once and holds its last frame.
        public float FrameDurationMs { get; init; } = 110f;

        // How big to draw each frame on screen (native pixel size * this).
        public float Scale { get; init; } = 3.2f;

        public bool IsComplete { get; private set; }

        public SpriteStrip? Get(AnimState state) => strips.TryGetValue(state, out var s) ? s : null;

        public static SpriteSet? TryLoad(string folder, Dictionary<AnimState, int>? frameCounts = null)
        {
            if (!Directory.Exists(folder)) return null;
            frameCounts ??= DefaultFrameCounts;

            var set = new SpriteSet();
            bool anyFound = false;
            bool anyMissing = false;

            foreach (AnimState state in Enum.GetValues<AnimState>())
            {
                string path = Path.Combine(folder, FileNameFor(state));
                if (!File.Exists(path))
                {
                    anyMissing = true;
                    continue;
                }

                anyFound = true;
                var image = Image.FromFile(path);
                set.strips[state] = new SpriteStrip(image, frameCounts[state]);
            }

            set.IsComplete = anyFound && !anyMissing;
            return anyFound ? set : null;
        }

        private static string FileNameFor(AnimState state) => state switch
        {
            AnimState.Idle => "idle.png",
            AnimState.Walk => "walk.png",
            AnimState.Jump => "jump.png",
            AnimState.Attack => "attack.png",
            AnimState.Special => "special.png",
            AnimState.Block => "block.png",
            AnimState.Hit => "hit.png",
            AnimState.KO => "ko.png",
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        };
    }
}

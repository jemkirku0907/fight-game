using System;
using System.Collections.Generic;
using System.IO;
using System.Media;

namespace FightingGame
{
    // Loads and plays short .wav combat sound effects. Every method is
    // safe to call even if the file is missing (silently does nothing) —
    // so dropping in your own Special sfx later never breaks anything.
    public static class Audio
    {
        private static string SoundsDir => Path.Combine(AppContext.BaseDirectory, "Sounds");

        private static readonly SoundPlayer swingSwordPlayer = new();
        private static readonly SoundPlayer hitSwordPlayer = new();
        private static readonly SoundPlayer swingMagicPlayer = new();
        private static readonly SoundPlayer hitMagicPlayer = new();
        private static readonly SoundPlayer blockPlayer = new();

        // Which roster characters use the metallic sword set vs the magic splash set.
        private static readonly HashSet<string> MeleeCharacters = new() { "Warrior", "Ninja" };

        static Audio()
        {
            TryLoad(swingSwordPlayer, "basic_swing_sword.wav");
            TryLoad(hitSwordPlayer, "basic_hit_sword.wav");
            TryLoad(swingMagicPlayer, "basic_swing_magic.wav");
            TryLoad(hitMagicPlayer, "basic_hit_splash.wav");
            TryLoad(blockPlayer, "basic_block.wav");
        }

        private static void TryLoad(SoundPlayer player, string fileName)
        {
            try
            {
                string path = Path.Combine(SoundsDir, fileName);
                if (File.Exists(path))
                {
                    player.SoundLocation = path;
                    player.Load();
                }
            }
            catch
            {
                // Missing/corrupt sound file — just play silently instead of crashing the fight.
            }
        }

        public static void PlayBasicSwing(string characterType) =>
            SafePlay(MeleeCharacters.Contains(characterType) ? swingSwordPlayer : swingMagicPlayer);

        public static void PlayBasicHit(string characterType) =>
            SafePlay(MeleeCharacters.Contains(characterType) ? hitSwordPlayer : hitMagicPlayer);

        public static void PlayBasicBlock() => SafePlay(blockPlayer);

        [System.Runtime.InteropServices.DllImport("winmm.dll")]
        private static extern int mciSendString(string command, System.Text.StringBuilder? buffer, int bufferSize, IntPtr hwndCallback);

        // Special sfx are per-character, dropped in by the user as
        // Sounds/Special/<RosterName>.wav or .mp3 (e.g. Sounds/Special/Baller.mp3).
        // Loaded fresh each time since these may be added/changed after the app starts.
        public static void PlaySpecial(string characterType)
        {
            try
            {
                string[] extensions = { ".mp3", ".wav", ".wma" };
                string? foundPath = null;

                string[] candidateNames = characterType == "Baller"
                    ? new[] { "Baller", "Rene", "Rene Clert", "Rene Clert Baterbonia" }
                    : new[] { characterType };

                foreach (var name in candidateNames)
                {
                    foreach (var ext in extensions)
                    {
                        string p = Path.Combine(SoundsDir, "Special", $"{name}{ext}");
                        if (File.Exists(p))
                        {
                            foundPath = p;
                            break;
                        }
                    }
                    if (foundPath != null) break;
                }

                if (foundPath == null) return;

                if (foundPath.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                {
                    using var player = new SoundPlayer(foundPath);
                    player.Play();
                }
                else
                {
                    mciSendString("close specialAudio", null, 0, IntPtr.Zero);
                    mciSendString($"open \"{foundPath}\" type mpegvideo alias specialAudio", null, 0, IntPtr.Zero);
                    mciSendString("play specialAudio from 0", null, 0, IntPtr.Zero);
                }
            }
            catch
            {
                // If the file is missing/invalid, the fight just continues without sfx.
            }
        }

        private static void SafePlay(SoundPlayer player)
        {
            try { player.Play(); } catch { /* ignore */ }
        }
    }
}

using System;
using System.IO;

namespace FightingGame
{
    public static class SpecialAudioGenerator
    {
        public static void EnsureSounds(string? explicitSoundsDir = null)
        {
            var targetDirs = new List<string>();
            string projectSounds = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Sounds"));
            if (Directory.Exists(projectSounds)) targetDirs.Add(projectSounds);

            string appSounds = Path.Combine(AppContext.BaseDirectory, "Sounds");
            if (Directory.Exists(appSounds) && !targetDirs.Contains(appSounds)) targetDirs.Add(appSounds);

            if (explicitSoundsDir != null && Directory.Exists(explicitSoundsDir) && !targetDirs.Contains(explicitSoundsDir))
                targetDirs.Add(explicitSoundsDir);

            foreach (var soundsDir in targetDirs)
            {
                string specialDir = Path.Combine(soundsDir, "Special");
                Directory.CreateDirectory(specialDir);

                string discipleWav = Path.Combine(specialDir, "Disciple.wav");
                if (!File.Exists(discipleWav))
                {
                    GenerateDiscipleSound(discipleWav);
                }

                string warriorWav = Path.Combine(specialDir, "Warrior.wav");
                if (!File.Exists(warriorWav))
                {
                    GenerateWarriorSound(warriorWav);
                }

                string ninjaWav = Path.Combine(specialDir, "Ninja.wav");
                if (!File.Exists(ninjaWav))
                {
                    GenerateNinjaSound(ninjaWav);
                }
            }
        }

        private static void GenerateDiscipleSound(string path)
        {
            // 44.1kHz, 16-bit Stereo PCM, duration: 6.2 seconds
            // 0.0s - 2.85s: Dark demonic hum + rising swirling vortex + crackling sub-bass charging
            // 2.85s - 3.4s: Colossal Demonic Detonation (explosive transient + deep sub drop + distortion)
            // 3.4s - 6.2s: Ominous low-frequency demonic rumble & cavernous reverb tail
            const int sampleRate = 44100;
            const double duration = 6.2;
            int totalSamples = (int)(sampleRate * duration);
            var samplesL = new float[totalSamples];
            var samplesR = new float[totalSamples];

            var rnd = new Random(1337);
            double impactTime = 2.85;

            for (int i = 0; i < totalSamples; i++)
            {
                double t = i / (double)sampleRate;
                float l = 0f, r = 0f;

                if (t < impactTime)
                {
                    // Charging buildup
                    double chargeProgress = t / impactTime; // 0..1
                    double freq = 55.0 + 220.0 * Math.Pow(chargeProgress, 2.5); // rising 55Hz -> 275Hz
                    double choirFreq = 110.0 + 330.0 * Math.Pow(chargeProgress, 2.0);

                    // Sub-bass drone + pulse
                    double tremolo = 0.5 + 0.5 * Math.Sin(2.0 * Math.PI * (4.0 + chargeProgress * 12.0) * t);
                    double drone = Math.Sin(2.0 * Math.PI * freq * t) * (0.25 + 0.55 * chargeProgress) * tremolo;

                    // Demonic choir / eerie harmonic overtone
                    double choir = (Math.Sin(2.0 * Math.PI * choirFreq * t) + 0.5 * Math.Sin(2.0 * Math.PI * choirFreq * 1.5 * t)) * (0.15 + 0.45 * chargeProgress);

                    // Crackling dark energy noise bursts
                    double crackle = (rnd.NextDouble() * 2.0 - 1.0) * Math.Pow(chargeProgress, 3.0) * 0.35;

                    float mono = (float)(drone + choir + crackle);
                    // Slight stereo swirl
                    double pan = Math.Sin(2.0 * Math.PI * 2.5 * t);
                    l = mono * (float)(0.7 + 0.3 * pan);
                    r = mono * (float)(0.7 - 0.3 * pan);
                }
                else
                {
                    // Explosive Detonation on impact
                    double postT = t - impactTime; // 0..3.35s

                    // 1. Initial transient crack (0..0.15s)
                    double crack = 0.0;
                    if (postT < 0.15)
                    {
                        double crackEnv = 1.0 - postT / 0.15;
                        crack = (rnd.NextDouble() * 2.0 - 1.0) * crackEnv * 0.95;
                    }

                    // 2. Heavy Sub-Bass Explosion drop (120Hz down to 28Hz)
                    double expFreq = Math.Max(28.0, 130.0 * Math.Exp(-postT * 4.5));
                    double bassEnv = Math.Exp(-postT * 1.8);
                    double subBoom = Math.Sin(2.0 * Math.PI * expFreq * postT) * bassEnv * 0.90;

                    // Soft saturation / overdrive distortion on boom
                    subBoom = Math.Clamp(subBoom * 1.4, -0.9, 0.9);

                    // 3. Midrange blast shockwave (noise through low-pass decay)
                    double shockwave = (rnd.NextDouble() * 2.0 - 1.0) * Math.Exp(-postT * 3.0) * 0.65;

                    // 4. Cavernous rumble tail
                    double tail = Math.Sin(2.0 * Math.PI * 38.0 * postT) * Math.Exp(-postT * 0.9) * 0.35;

                    float mono = (float)(crack + subBoom + shockwave + tail);
                    l = mono * (float)(0.95 + 0.05 * Math.Sin(postT * 10.0));
                    r = mono * (float)(0.95 - 0.05 * Math.Sin(postT * 10.0));
                }

                samplesL[i] = Math.Clamp(l, -1f, 1f);
                samplesR[i] = Math.Clamp(r, -1f, 1f);
            }

            WriteWavFile(path, sampleRate, samplesL, samplesR);
        }

        private static void GenerateWarriorSound(string path)
        {
            // Rising metallic blade charge followed by a colossal earth-splitting slash boom at 2.85s
            const int sampleRate = 44100;
            const double duration = 5.8;
            int totalSamples = (int)(sampleRate * duration);
            var samplesL = new float[totalSamples];
            var samplesR = new float[totalSamples];
            var rnd = new Random(4242);
            double impactTime = 2.85;

            for (int i = 0; i < totalSamples; i++)
            {
                double t = i / (double)sampleRate;
                float l = 0f, r = 0f;

                if (t < impactTime)
                {
                    double prog = t / impactTime;
                    double freq = 180.0 + 520.0 * Math.Pow(prog, 2.0);
                    double bladeShimmer = Math.Sin(2.0 * Math.PI * freq * t) * (0.3 + 0.5 * prog);
                    double warHorn = Math.Sin(2.0 * Math.PI * 110.0 * t) * (0.2 + 0.4 * prog);
                    float mono = (float)(bladeShimmer + warHorn);
                    l = mono; r = mono;
                }
                else
                {
                    double postT = t - impactTime;
                    double slashNoise = (rnd.NextDouble() * 2.0 - 1.0) * Math.Exp(-postT * 5.0) * 0.85;
                    double heavyImpact = Math.Sin(2.0 * Math.PI * Math.Max(35.0, 160.0 * Math.Exp(-postT * 6.0)) * postT) * Math.Exp(-postT * 2.2) * 0.9;
                    float mono = (float)(slashNoise + heavyImpact);
                    l = Math.Clamp(mono, -1f, 1f);
                    r = Math.Clamp(mono, -1f, 1f);
                }
                samplesL[i] = Math.Clamp(l, -1f, 1f);
                samplesR[i] = Math.Clamp(r, -1f, 1f);
            }

            WriteWavFile(path, sampleRate, samplesL, samplesR);
        }

        private static void GenerateNinjaSound(string path)
        {
            // Whispering wind + sonic shadow buildup followed by a hyper-speed razor burst at 2.85s
            const int sampleRate = 44100;
            const double duration = 5.6;
            int totalSamples = (int)(sampleRate * duration);
            var samplesL = new float[totalSamples];
            var samplesR = new float[totalSamples];
            var rnd = new Random(7777);
            double impactTime = 2.85;

            for (int i = 0; i < totalSamples; i++)
            {
                double t = i / (double)sampleRate;
                float l = 0f, r = 0f;

                if (t < impactTime)
                {
                    double prog = t / impactTime;
                    double wind = (rnd.NextDouble() * 2.0 - 1.0) * (0.15 + 0.35 * prog);
                    double whoosh = Math.Sin(2.0 * Math.PI * (220.0 + 600.0 * prog) * t) * (0.2 + 0.45 * prog);
                    float mono = (float)(wind + whoosh);
                    l = mono * (float)(0.6 + 0.4 * Math.Sin(t * 8.0));
                    r = mono * (float)(0.6 - 0.4 * Math.Sin(t * 8.0));
                }
                else
                {
                    double postT = t - impactTime;
                    double slice = (rnd.NextDouble() * 2.0 - 1.0) * Math.Exp(-postT * 8.0) * 0.9;
                    double sonicBoom = Math.Sin(2.0 * Math.PI * Math.Max(40.0, 200.0 * Math.Exp(-postT * 7.0)) * postT) * Math.Exp(-postT * 2.5) * 0.85;
                    float mono = (float)(slice + sonicBoom);
                    l = Math.Clamp(mono, -1f, 1f);
                    r = Math.Clamp(mono, -1f, 1f);
                }
                samplesL[i] = Math.Clamp(l, -1f, 1f);
                samplesR[i] = Math.Clamp(r, -1f, 1f);
            }

            WriteWavFile(path, sampleRate, samplesL, samplesR);
        }

        private static void WriteWavFile(string path, int sampleRate, float[] left, float[] right)
        {
            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            using var bw = new BinaryWriter(fs);

            int numSamples = left.Length;
            int numChannels = 2;
            int bitsPerSample = 16;
            int byteRate = sampleRate * numChannels * (bitsPerSample / 8);
            int blockAlign = numChannels * (bitsPerSample / 8);
            int dataSize = numSamples * blockAlign;

            // RIFF chunk
            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + dataSize);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

            // fmt chunk
            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16); // subchunk1 size
            bw.Write((short)1); // PCM
            bw.Write((short)numChannels);
            bw.Write(sampleRate);
            bw.Write(byteRate);
            bw.Write((short)blockAlign);
            bw.Write((short)bitsPerSample);

            // data chunk
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(dataSize);

            for (int i = 0; i < numSamples; i++)
            {
                short sl = (short)Math.Clamp((int)(left[i] * 32767f), -32768, 32767);
                short sr = (short)Math.Clamp((int)(right[i] * 32767f), -32768, 32767);
                bw.Write(sl);
                bw.Write(sr);
            }
        }
    }
}

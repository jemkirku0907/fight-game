HOW TO ADD YOUR OWN SPECIAL SOUNDS
====================================
Drop a .wav file in here named exactly after the character class:

  Sounds/Special/Warrior.wav
  Sounds/Special/Ninja.wav
  Sounds/Special/Mage.wav
  Sounds/Special/Disciple.wav
  Sounds/Special/Gunner.wav

It plays automatically the instant that character's Special Attack is
thrown (win or miss). If a file is missing, the special just plays with
no sound — nothing breaks.

Keep clips short (under ~2 seconds) so they don't overlap the next
action. .wav only (PCM, 16-bit is safest) — no mp3.

Mage.wav and Gunner.wav are already filled in, trimmed from the two
reference clips you uploaded (raw untrimmed copies are in
../RawReferenceClips/ if you want to re-cut them yourself):
  - Mage.wav (~9.8s) — the long slow-build-then-boom clip. This is well
    past the "~2 seconds" guideline above on purpose, since that slow
    burn was the point — but it WILL keep playing under whatever
    happens next in the fight, so listen for whether that reads as
    cool or messy once other hits land on top of it.
  - Gunner.wav (~6.5s) — trimmed tighter around the loudest hit in the
    punchier second clip. Still long by fighting-game standards; cut it
    down further in an editor if it starts stepping on the action.
Warrior.wav, Ninja.wav, and Disciple.wav are still unfilled.

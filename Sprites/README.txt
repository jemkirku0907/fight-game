HOW SPRITE MODE WORKS
======================
The game auto-detects sprite art per character, by roster name. If
Sprites/<Name> (Warrior, Ninja, Mage, or Disciple) has ALL 8 files
below, that fighter draws with real sprites instead of the stick
figure — whichever slot (P1 or P2) that character is picked into on
the name-entry screen. If any file is missing, that fighter just
keeps using the stick figure — nothing breaks either way.

Current roster:
  Sprites/Warrior  - Fumiko Complete Charset (OpenGameArt)
  Sprites/Ninja    - Samurai Sprite Sheet by DAGON (itch.io)
  Sprites/Mage     - mage-1/2/3 sheets (idle / staff-cast attack / tendril-burst special)
  Sprites/Disciple - disciple sheet (idle / cast attack / dissolve KO) +
                      the "shadow" monster's lunge row borrowed as its special

Files each folder needs (exact names, all lowercase):
  idle.png
  walk.png
  jump.png
  attack.png
  special.png
  block.png
  hit.png
  ko.png

WHAT EACH FILE SHOULD LOOK LIKE
================================
Each file is a single horizontal filmstrip: every frame of that one
animation, same height, placed side by side left-to-right, in order,
on a transparent background. Example: if "walk.png" has a 4-frame walk
cycle and each frame is 32x48px, the whole file is 128x48px.

You do NOT need to draw anything — see the two free packs suggested in
chat (Samurai Sprite Sheet by DAGON on itch.io, or Fumiko Complete
Charset on OpenGameArt, both free). Whatever sheet you download, open
it in a free editor like Piskel (piskelapp.com) or Photopea
(photopea.com), crop out each animation's row, and export it as its
own PNG using the file names above.

TELLING THE CODE HOW MANY FRAMES ARE IN EACH FILE
====================================================
Open SpriteSet.cs and edit the FrameCounts dictionary near the top —
set each number to match how many frames you actually exported for
that animation. This is the one thing that can't be auto-detected.

TWEAKING SIZE
==============
FighterView.cs creates SpriteSet with default Scale = 3.2 (how much to
blow up each pixel-art frame) and FrameDurationMs = 110 (how long each
frame is held, in milliseconds). Adjust those two numbers in
SpriteSet.cs to taste once you see it in-game.

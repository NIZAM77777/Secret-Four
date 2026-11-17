SECRET FOUR — AUDIO

All sounds and music in this folder are original, synthesized from code for Secret Four
(no samples, loops or third-party recordings), so they are free to publish.
The generator script is kept outside the build at _RemovedFromBuild/AudioSource/GenerateAudio.py.

Music (seamless loops, stereo, streamed):
  MenuMusic  - calm A-minor theme for the main menu, store and rooms
  GameMusic  - quiet, focused D-minor theme during matches

Sound effects:
  Click   - any button          Back    - Back / Close / Cancel / Main Menu buttons
  Place   - coin placed         Lock    - direction chosen (hidden)
  Reveal  - directions revealed Move    - coins moving
  Win     - you won             Lose    - you lost (bot / online)
  Draw    - draw                Unlock  - store item unlocked
  Reward  - one rewarded ad counted toward an unlock
  Select  - skin/board selected Error   - something failed / opponent left
  Notify  - room created, waiting for a friend

GameAudio loads these by Resources path. To replace a sound, overwrite the WAV and keep its .meta.
Settings > Master / Music / Effects / Mute control everything. Music and effects go silent while
a full-screen ad is showing.

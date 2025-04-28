# Guide

The following is a guide for both a demonstratory executable and the source code, which can be imported to Unity and customised for variations of the current prototype.

## Executable

The executable is generated from a setup including two rigs. The "always assign alignments" boolean is set to true, so the alignment phase will be included every time the executable is run. The other inspector values are as follows:

- sdaas
- sadsa

Running the executable will first use the alignment phase for both talks. Then, the user enters the game scene, where they may navigate the environment. Red circles will appear at each talk location. Entering these thresholds will play a demonstratory volumetric video talk. Walking out of the threshold will convert the avatar back to its rigged form, where it will continue to follow the player.

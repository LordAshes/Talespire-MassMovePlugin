# Mass Move Plugin

This unofficial TaleSpire allows you to select multiple mini at once (using single adds or mass selection) and then move
all selected minis at once using any of the selected minis as the "leader". This plugins keeps the relative distances
between minis and ignores all obstacles. So, for example, moving the leader through a door will likely cause the other
selected minis to move through the wall around the door.

Future expansion to this plugin is expected which will implement a true follow the leader option.

This plugin, like all others, is free but if you want to donate, use: http://LordAshes.ca/TalespireDonate/Donate.php

Video Demo/Tutorial: https://youtu.be/-gXBG2uyQXs

## Change Log

```
2.0.0: Fix after BR HF Integration update
1.1.0: Added single file mode and formation
1.0.0: Initial release
```

## Install

Use R2ModMan or similar installer to install this plugin.

## Usage

There are two ways to select minis: single and mass.
There are two movement modes: mass move and single file.

### Single select

1. Select the first mini as normal
2. To add minis to the selection, hold CTRL and select a mini.

There is currently no remove option (coming in future releases) and thus to remove selections, it is necessary to clear
all selections (by selecting a single mini) and the re-adding the ones that are desired.

### Mass selection

1. Select a mini (or multiple minis using the option above)
2. Hold the ALT key and select a mini

All minis in a rectangular region formed by the previously selected mini and the last select mini will be selected.

Note: The Windows convention for range is usually SHIFT but SHIFT does not work due to its existing use in core TS.


#### Mass Move

When selecting a mini to move you need to still be holding one of the modidifer keys (typically control) in order to
not deselect the multi-selection. Holding control prevent a mini from being moved since it trips core TS height
adjustment. To get around this:

1. Hold control and select the desired mini to move.
2. Release control.
3. Move the mini.

Once the mini is dropped, all other selected minis will update their position by the same change.

#### Single File

Single file mode can be toggled using the keyboard (default LCTRL+F) or using the radial menu. When in single file mode
the followers will try to reach the spot where the leader started leading and then follow the leader's steps. During this
mode the followers check for collisions between other followers and can be prevented by moving if another folower or the
leader is in the way. After some movement this will tend to create a single file line following the leader allowing the
group to get through choke points. Once single file mode is turned off, the default mode of Mass Move will take effect.
The groups formation prior to starting the single file mode is not restored automatically but can be restored, if it was
saved, by using the keyboard shortcut (default RSHIFT+F) or using the radial menu.

By default, the formation is saved automatically when single line mode is started. However, this feature can be turned off
in which case the formation needs to be saved manually using the keyobard (default RCTRL+F) or using the radial menu.

## Limitation

1. Only one group selection is possible at any time
2. Only one formation can be saved at a time
3. Collision detection takes place between minis only. Restoring a formation or starting a single file can cause minis to
   pass through objects.

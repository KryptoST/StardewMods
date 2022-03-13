# Changelog

## 0.12.1
Released March 12th, 2022.

### General

* The Current Location filter for fishing now says "Unfiltered" rather than
  "No" when inactive. This should make it more clear that it's not active.
* Added a "Debug Mode" setting. Currently, this doesn't do a lot. The idea
  is that this will start showing more IDs and other internal information in
  the Almanac that would be useful for modders or translators working with it.

### Fixes

* Fish with multiple time ranges now show their time ranges correctly, rather
  than just repeating the first range.

## Translation

* Add a Chinese translation. Thank you, ninthalley!


## 0.12.0
Released March 12th, 2022.

### General

* Added a Fishing page! The fishing page lets you filter by several different
  categories, and shows you useful information about fish in the game. I
  tried to balance utility and cheatyness, but it still probably needs tweaks.
  If nothing else, I need to figure out how to handle locations that shouldn't
  be listed until you've visited them.
* Almanac pages now have much nicer scrolling! You can scroll smoothly now, as
  well as by holding the middle mouse button. The scrollbar grows and shrinks
  depending on how much content there is. And when you click it, it grows a
  little too for visual feedback.
* Now, when a crop can grow into a giant crop, you can mouse over the words
  giant crop to view an image of it.

### Fixes

* Fixed an issue with crop data failing to load if a single crop has corrupt
  data. Now, the bad crop will be logged and processing will continue.
* Fixed an incorrect duration calculation being performed for some custom
  crops, specifically when fertilizer was being used.
* Fixed an exception when joining a split-screen game where Almanac tried
  opening its menu while the game was still loading.

### Mod Compatibility

* Added support for the Luck Skill mod. Calculations are performed based on
  the assumption that all farmers will be online and on the same team.
* Fixed DynamicGameAssets crops failing to load at all when encountering a
  problem with a single crop. Now, an error is logged for every bad crop but
  the process continues.
* DynamicGameAssets crops will now display when out of season, as long as
  their dynamic fields are sufficiently simple to parse manually.
* DynamicGameAssets crops will now show their regrow times more correctly,
  though still likely a bit wrong due to how they handle phases.
* Add support for More Giant Crops and JsonAssets giant crop sprites.
* Add Stardew Aquarium and Stardew Valley Expanded location names to the
  default languages for display in Almanac menus that use locations.

### (Internal) C# API Changes

* AlmanacMenu now has support for a "Seasonal" page type, which displays the
  season picker but does not display the calendar.
* AlmanacMenu now has support for separate left and right Flow scrolls.
* There is a new management class for a scrollable Flow that manages events
  and components. Makes it much easier to integrate.
* Made a bunch of tweaks and improvements to Flow so that we can have nice,
  smooth scrolling.


## 0.11.0
Released March 3rd, 2022.

### General

* Added settings to hide different sets of notices in the Local Notices
  page. So far, you can show or hide: Anniversaries, Gathering, and
  Festivals separately.
* Added festivals and anniversaries to the Local Notices page.
* Added support for scrolling the Almanac's tabs when there are more
  than nine tabs to display. This currently isn't important, but it
  will be in the future as other mods add their own content to
  Almanac (with any luck).
* Try to snap the cursor better when scrolling with a gamepad.
* Only display birthdays for NPCs we can socialize with on the
  Local Notices' tab, for now. There might be a better way to handle
  this in the future, but this seems to prevent spoilers from
  happening?
* When there are multiple notices on a day, and there is no NPC with
  a birthday on that day, display up to three separate notice icons
  before giving up and displaying a rotating icon.

### Bug Fixes

* Fixed an issue with the game menu overlay spamming errors when the
  menu does not have a components list.
* Fixed the Crop Previews setting not working to disable that feature.


## 0.10.1
Released March 3rd, 2022.

### General

* Make the PageUp, PageDown, Home, and End keys more useful when viewing
  long text on Almanac pages.

* Fix vertical centering of calendar labels.
* Fix a bug with the wrong cutscene flag being checked, resulting in being
  unable to open the Almanac after saving and loading.

### Translation

* Add initial Portuguese translation, thanks for Pedrowser on NexusMods!
* Add initial Russian translation, thanks to AveAcVale on GitHub!

### C# API Changes

* Nothing yet, but work is underway on an API for adding custom pages. I might
  end up waiting for SMAPI 3.14 for support for generics in API interfaces.


## 0.10.0
Released March 2nd, 2022.

### General

* Make the Almanac remember its previous state when you close and re-open it.
* Remember the scroll position when changing pages in the Almanac.
* Add hazelnut season to the Local Notices page.
* Stop recalculating page contents if the date changes but the season has not.
* Add more keyboard bindings for the Almanac menu.

### Translation

* Add support for translating events, both with normal translation keys and by
  supporting entirely different event.json files when more significant changes
  are required.

### C# API Changes

* Add methods for adding entries to the Fortune and Local Notices page.


## 0.9.0
Released March 1st, 2022.

* Initial release.
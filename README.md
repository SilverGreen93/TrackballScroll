TrackballScroll Extended
========================
**Allow scrolling with a trackball without scroll wheel by using a low level mouse hook.**

This project is based on the work of Martin Seelge from https://github.com/Seelge/TrackballScroll

Numerous improvements and enhancements have been made to the original TrackballScroll applications which include:

- Select each X-Button action individually: scroll key, middle-click, default action (back, forward)
- Change scroll speed independent of Windows settings: low, medium, high
- Select scroll key secondary action when not scrolling: hold to scroll, toggle scrolling, default action (back, forward), middle-click
- New About box

###### Requirements
- A trackball or mouse with X-Buttons
- A Microsoft Windows x64 operating system with .NET 4.5.2

This software has been tested with a *Logitech Marble Trackball*(tm) and *Microsoft Windows 10*.

###### Download the latest release
https://github.com/SilverGreen93/TrackballScroll/releases/latest

###### Run the program
- Execute `TrackballScroll.exe`, no installation is necessary.
- Configuration options are available using right click on the icon in the systray (the icons next to the clock).
- When using this program with a driver software that allows customization of the button behavior, make sure to set the X-Buttons to default behavior. E.g. with a *Logitech Trackman Marble*, make sure to set the buttons 3 and 4 to `default` button behaviour and not `back`/`Universal Scroll`.
- The lines scrolled per wheel event are determined by the *Microsoft Windows* mouse wheel settings.

###### Compile the source code
- Clone the repository from the github page or download the latest source code archive
- Open the solution with *Microsoft Visual Studio 2019*.
- Change the build type to `Release`.
- Build the solution

###### Version history
- v7: Implement more options for handling different click types and options including speed control, secondary action, independent configuration; Fix About box lag.
- v6 fixes #20: allows to reverse scrolling directions
- v5 fixes #17 + improved stability (rewrite of the mouse hook, state machine and mouse event sending)
- v4 prevents #16... sort of (middle clicks can be disabled if necessary using the tray menu).
- v3 feature #12: Allows to choose which X-buttons are used and saves the settings. Also changed versioning scheme to single numbers.
- v2.1.2 fixes #14. Compatibility with Windows 10 versions of 2017.
- v2.1.1 fixes issues #8, #9, #10. Instead of calculating the scaled coordinates, both original and scaled coordinates are memorized.
- v2.1.0 fixes issue #5 with high dpi scaling. Note: The app must be restartet after changing the scaling factor in windows.
- v2.0.1 Added an application icon and moved all strings to a resource file.
- v2.0.0 The application is accessible through a systray-icon, the console window is gone. Converted from C++ to C#.
- v1.1.0 Add horizontal scroll 
- v1.0.1 Add info to the console window

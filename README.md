<p align="center">
	<img align="center" alt="Icon" src="./MainPageImages/SSIcon_1.png" width="200"/>
</p>
<h1 align="center"> Screens Shots </h1>

<p align="center"><a href="./README_CN.md">中文</a>
 This is a free & open-source tool for taking snapshots of multiple screens/monitors on Windows PC. 
</p>

<p align="center">
	<img align="center" alt="ScreenShotUIDemo" src="./MainPageImages/ScreenShotUIDemo.png" width="500"/>
</p>
<p align="center"> <sup>Frame mode that allows you to choose an area on any of your monitors</sup></p>

<h2 align="center">It requires a <a href="https://dotnet.microsoft.com/download/dotnet-core/3.1">.NET Core 3.1</a> (or above) environment</h2>

<h3>Features:</h3>

 - One tool for multiple monitors with different dpi / system UI scales
 - Different modes: Instant screenshot(s) of the whole monitor(s) or of a specific area
 - You can definitely use it for one monitor as well

<h3>How to use it:</h3>

 - Install it.
 - Open options window and check shortcut and if previews of all your monitors are shown correctly.
 - Click-click.

<h3>Examples:</h3>

<img align="center" alt="StartUpPage" src="./MainPageImages/StartUpPage.png" width="300">
</img>

<img align="center" alt="OptionMain" src="./MainPageImages/OptionMain.png" width="500">
</img>

<img align="center" alt="OptionTwo" src="./MainPageImages/OptionTwo.png"  width="500">
</img>

<img align="center" alt="OptionDarkTheme" src="./MainPageImages/OptionDarkTheme.png"  width="500">
</img>


<h3>Known Issues:</h3>

 - Cannot identify the second monitor correctly if change primary monitor to another one while using "second screen only".
 - Incorrect position of floating buttons after selecting the desired area.

<h3>Trivials:</h3>

 - It's a WPF project and dooesn't require any special NuGet package. If you want to compile it, just make a copy and use VS or enter `dotnet run` in the command line.
 - In essence, this app tries to "guess" what is the correct resolution for each monitor and it will remember your settings if you change the defaults. At its core, it uses gdi to capture screen by frame.
 - WPF is not per-monitor-dpi-aware for any Windows version. Therefore, you may experience blurriness on certain screen(s) when the ScreenShots freezes your screen. Don't worry, ScreenShots will make sure those final snapshots are in lostless quality (if you choose png/bmp).
 - Welcome to report bugs / criticise me (via Issue or my personal mail xinyu@fishesplace.com)

<h3>Huge Thanks to:</h3>

 - [Nicke Manarin and his ScreenToGif](https://github.com/NickeManarin/ScreenToGif) I used some of his fantastic custom controls and I won't pretend they are mine.
 - [Modern UI Icons](http://modernuiicons.com/) for all lovely icons in this app.
 - [Handy Control](https://github.com/HandyOrg/HandyControl) for their practical example of using Gdi and device context for screenshot
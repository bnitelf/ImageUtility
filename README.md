# ImageUtility
I create this repo to develop my own Image Utility for my routine image task.

Background
- I usually upload many image (General picture, Tutorial, etc..) to Google Drive.
- I use free-tier Google Drive (limit to 15 GB)
- So I want to reduce the size of images I upload as small as possible.
- I usually use GIMP (Photoshop-liked), PhotoScape (Image manipulation, bulk resize)
- actually they all have the functionalities I needed.
- but they require many steps to do so.
- I want something that I can just drag-drop boom! done!
- so I create this small ImageUtility to fit my need.

## Stack
- .NET 8
- WPF
- ImageMagick cmd
  - command line for bulk image manipulation (resize, convert between image extension)
- Initially created project from Visual Studio 2022

## Project Structure
- As of now, I decide to make code structure simple and straight forward. 
- Not using any design pattern.
- As the functionality is quite small.

## How to use
- OS support **Window** only
- Install [ImageMagick](https://imagemagick.org/script/download.php) (if not yet installed)
  - set ImageMagick path in System Environment Variable
  - sample path = `C:\Program Files\ImageMagick-7.1.1-Q16-HDRI`
  - after set, you should be able to type `magick` command in your **Terminal**
- Then you can use this app
- (if needed) setting file is located at `C:\Users\{username}\AppData\Local\ImageUtility\ImageUtility_Url_{random}\1.0.0.0\user.config`
  - setting file will be created after first launch.
  
  
## TODO
- Fix jpg compress percent not working if file extension is uppercase (.JPG)

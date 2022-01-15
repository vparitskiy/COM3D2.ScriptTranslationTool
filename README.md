# COM3D2 Script Translation Tool
This small program was made to handle Custom Order Maid 3D2 extracted script files,  
Unlike [Translation Manager](https://github.com/Pain-Brioche/COM3D2.TranslationManager) it is way easier to use and less finicky.  
This is the exact same I used for the translation pack.

It can:
- Support Official translation and cache for later use.
- Support manual translation.
- Support new translation via Sugoi Translator.
- Reconstruct translate and sort Japanese scripts from cached translations.
- Merge Japanese and Official translations.
- Merge scripts with the same name.


## Basic instructions.

- You need .Net framework 4.8 (you probably already have it if you keep your system updated).  
- Place your Japanese extracted scripts in ``Scripts\Japanese\``
- Place (if you have one) your English extracted scripts in ``Scripts\English``
- Start the program and follow the instructions on screen.  
**When working with COM3D2 scripts you're dealing with tens of thousands of script files and more than 650.000 translated lines!  
This takes time, be patient!**

## How to add/edit translations

- Translations are located in the ``Caches`` folde, feel free to edit them as you please.  
Keep in mind that the program will always choose manual translations over official over machine.  
One line per sentence with this format ``Japanese[tab]English``.

- If you wish to use [Sugoi translator](https://www.youtube.com/watch?v=r8xFzVbmo7k) (Download links in the video description)  
Just start it before this tool.  
``\backendServer\Program-Backend\Sugoi-Translator-Offline\offlineTranslation\activateOfflineTranslationServer.bat``.  
**Be warned this will use a LOT of CPU time on your computer (or GPU if you use cuda)**  
If correctly configure the program will detect it and use it if it can't find a match in any cached data.

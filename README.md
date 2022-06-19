# COM3D2 Script Translation Tool
This small program was made to handle Custom Order Maid 3D2 extracted script files,  
Unlike [Translation Manager](https://github.com/Pain-Brioche/COM3D2.TranslationManager) it is way easier to use and less finicky.  
This is the exact same I used for the translation pack.

It can:
- Support Official translation and cache for later use.
- Support manual translation.
- Support new translation via Sugoi Translator.
- Reconstruct, translate and sort Japanese scripts from cached translations.
- Merge Japanese and Official translations.
- Merge scripts with the same name.


## Basic instructions.

- You need .Net framework 4.8 (you probably already have it if you keep your system updated).  
- Place your Japanese extracted scripts in ``Scripts\Japanese\``
- Place (if you have one) your English extracted scripts in ``Scripts\English\``
- Start the program and follow the instructions on screen.  
**When working with COM3D2 scripts you're dealing with tens of thousands of script files and more than 650.000 translated lines!  
This takes time, be patient!**


## Why merge Machine and Official Translations

Two reasons:
- First i18nEx only loads one version of each script, so if it will discard any script with a identical name already loaded.  
As a result you cannot have both machine & official in, as you don't know which one will be picked (and you want the official of course)
- Second, because of how Kiss manage the game files updates.  
You see when Kiss fix or update a script (.ks) it doesn't remove the old one, it simply add a new one and the game picks the latest in .arc alphabetical order.
The script extractor can't do that pick and so extract everything including duplicates; leading to the first issue above where i18nEx will only pick one.

I made a point in this tool to merge all identicaly named scripts in one file, removing that issue for translated script and ensuring that you always have a translation available for whaterver version of the script you're on.
Merging the official script will also enforce that, ensuring the official translation is always used when available.
I recognize this is an additional step not everyone is going to take, but this is the best solution I came up with.

Using a dedupe software isn't a bad solution in itself, you just won't be sure which script is the most recent one  
(keeping in mind that the english extracted scripts also have duplicates)

## How to merge Machine and Official Translations
**You do not need the translated pack, everything is already included in the tool Cache folder**
- Extract the Japanese **AND** English scripts from your games.  
[How to Extract Scripts](https://github.com/ghorsington/COM3D2.i18nEx#extracting-translations-from-the-english-game)
- Put the Japanese scripts in ``Scripts\Japanese\``
- Put the English scripts in ``Scripts\English\``
- Run the tool, it will start by building an official cache.
- Wait for it to finish loading (remember dealing with thousands of files takes time) and press any key when asked too.
- Wait for it to finish and grab your merged script from ``Scripts\i18nEx\``

## How to add/edit translations

- Translations are located in the ``Caches`` folder, feel free to edit them as you please.  
Keep in mind that the program will always choose manual translations over official over machine.  
One line per sentence with this format ``Japanese[tab]English``.

- If you wish to use [Sugoi translator](https://www.youtube.com/watch?v=r8xFzVbmo7k) (Download links in the video description)  
Just start it before this tool.  
``\backendServer\Program-Backend\Sugoi-Translator-Offline\offlineTranslation\activateOfflineTranslationServer.bat``.  
**Be warned this will use a LOT of CPU time on your computer (or GPU if you use cuda)**  
If correctly configure the program will detect it and use it if it can't find a match in any cached data.

## Notes
- __npc_names.txt must not be put inside your i18nEx folder or it'll cause lipsync issue for NPCs, use XUnityAutoTranslator instead.
- Due to how i18nEx loads translation and how Kiss update their files, I strongly suggest to merge your official to the translation pack.
- Any line the program can't parse or translate will be reported in Errors.txt
- Please report any bug you find.

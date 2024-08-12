# Echokraut-Tools
* Tools to create a dataset for your own trained voice model with your own FFXIV GameData.
* FOR PERSONAL USE ONLY.
* Using this tool I expect you to know how to use [alltalk_tts](https://github.com/erew123/alltalk_tts/tree/alltalkbeta). If you need any assistance feel free to [![Discord](https://img.shields.io/badge/Join-Discord-blue)](https://discord.gg/5gesjDfDBr)
* This tool is at the moment only tested in german but should still work in the other official client languages(english, french, japanese)

# How to Use
* Download latest release
* Extract to a place of your liking
* Start "Echokraut Tools.exe"
* Select your game install location ("game" folder)
* Select save location for the extracted data (should have atleast 10 to 20GB of free space)
* Select your language
* Hit the button.
* After it loads all necessary exd data it will begin extracting the audio files and converting them to a usable format. (A separate CMD window will open for that process, don't do anything and let it work.)
* The progress bar will show "done" when it's done. You can now close "Echokraut Tools"
* Copy "metadata_train.csv", "metadata_eval.csv" and the "wavs" folder into a new folder (FFXIV for example) inside the "finetune" folder of [alltalk_tts](https://github.com/erew123/alltalk_tts/tree/alltalkbeta).
* Start the finetune process according to the alltalk manual.
* Once the finetune process is finished: You can create your voice files by taking one of the many .wav files out of the wavs/npcname folders for each npc and test them in [alltalk_tts](https://github.com/erew123/alltalk_tts/tree/alltalkbeta) until you find one you are happy with. And name them according to the [Echokraut](https://github.com/RenNagasaki/Echokraut)-Plugin instructions. 

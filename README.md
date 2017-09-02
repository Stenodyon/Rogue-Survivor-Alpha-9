Rogue Survivor Linux
====================
Rogue Survivor game original source code from 2012 adapted to run on linux.
It compiles and run on my machine (XUbuntu 14.04) but I have not tested it
extensively yet. There is no sound because no DirectX or SFML. If you know
how to compile with SFML.Net, please send me a mail or a message.


WHAT IS THIS
------------

This is a fork of the game source source from 2012, the goal is to make this
compile and run on linux. There are no data files or resources, you have to
download the game for that.


GET THE GAME
------------

The original blog where you can get the game is still alive, so go there:
http://roguesurvivor.blogspot.com/
You'll need this for the resources (data files, images etc...)

BUILDING
--------

You will need ```gmcs``` and the System libraries to compile. Here are the
Debian/Ubuntu packages you'll need:
```
mono-gmcs
mono-devel
```

Then do
```bash
./compile.sh
```
This will compile the game (lots of warnings still) to ```RogueSurvivor.exe```

RUNNING
-------
In order to run the game, you will need the data. Simply download the game
from [here](https://roguesurvivor.blogspot.fr/p/download.html) and copy the
Resources folder at the root of the repo.

:warning: Make sure you do not overwrite the already existing Resource files
(modifications were needed).

To run the game, simply do
```
mono RogueSurvivor.exe
```

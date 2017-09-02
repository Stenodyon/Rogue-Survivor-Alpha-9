#!/bin/bash
# -*- coding: UTF8 -*-

gmcs -out:RogueSurvivor.exe -pkg:dotnet -define:LINUX $CSFLAGS $(find . -name *.cs)


BINARY=RogueSurvivor.exe

CS=gmcs
CSFLAGS = -pkg:dotnet \
	      -define:LINUX \
		  -debug

SRC = $(shell find . -name *.cs)

default:
	$(CS) $(CSFLAGS) -out:$(BINARY) $(SRC)

.PHONY: default clean

clean:
	rm $(BINARY)


BINARY=RogueSurvivor.exe
TEST_BIN=test_$(BINARY)

CS=gmcs
CSFLAGS = -pkg:dotnet \
	      -define:LINUX \
		  -debug

SRC = $(shell find src | grep .cs$$)
TEST_SRC = $(filter-out src/Program.cs,$(SRC)) $(shell find test | grep .cs$$)

default: $(BINARY)

test: $(TEST_BIN) $(TEST_SRC)
	mono --debug $(TEST_BIN)

$(BINARY):
	@$(CS) $(CSFLAGS) -out:$(BINARY) $(SRC)

$(TEST_BIN):
	@$(CS) $(CSFLAGS) -out:$(TEST_BIN) $(TEST_SRC)

.PHONY: default test clean

clean:
	-rm $(BINARY)
	-rm $(TEST_BIN)
	-rm *.mdb


BINARY=RogueSurvivor.exe
TEST_BIN=test_$(BINARY)

CS=mcs
CSFLAGS = -pkg:dotnet \
	      -define:LINUX \
		  -lib:/usr/lib/mono/2.0

SRC = $(shell find src | grep .cs$$)
TEST_SRC = $(filter-out src/Program.cs,$(SRC)) $(shell find test | grep .cs$$)

default: release

release: $(BINARY)

debug:CSFLAGS += -debug -define:DEBUG
debug: $(BINARY)

test: $(TEST_BIN) $(TEST_SRC)
	mono --debug $(TEST_BIN)

$(BINARY):
	@$(CS) $(CSFLAGS) -out:$(BINARY) $(SRC)

$(TEST_BIN):
	@$(CS) $(CSFLAGS) -out:$(TEST_BIN) $(TEST_SRC)

.PHONY: default release debug test clean

clean:
	rm $(BINARY) 2>/dev/null || true
	rm $(TEST_BIN) 2>/dev/null || true
	rm *.mdb 2>/dev/null || true

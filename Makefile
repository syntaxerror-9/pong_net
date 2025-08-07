RAYLIB_PATH = external/raylib
RAYLIB_BUILD_PATH = $(RAYLIB_PATH)/build/raylib


run-client: 
	export RAYLIB_BUILD_PATH=$(RAYLIB_BUILD_PATH) && \
	dotnet run --project client

run-client2:
	export RAYLIB_BUILD_PATH=$(RAYLIB_BUILD_PATH) && \
	dotnet run --project client & \
	export RAYLIB_BUILD_PATH=$(RAYLIB_BUILD_PATH) && \
	sleep 1 && \
	dotnet run --project client

run-server:
	dotnet run --project server

build-raylib:
	mkdir -p $(RAYLIB_PATH)/build
	cmake -S $(RAYLIB_PATH) -B $(RAYLIB_PATH)/build \
		-DBUILD_SHARED_LIBS=ON \
		-DBUILD_EXAMPLES=OFF \
		-DBUILD_GAMES=OFF
	cmake --build $(RAYLIB_PATH)/build --config Release -j8


clean-raylib:
	rm -rf $(RAYLIB_PATH)/build

RAYLIB_PATH = external/raylib
RAYLIB_BUILD_PATH = $(RAYLIB_PATH)/build/raylib


run-client: 
	export RAYLIB_BUILD_PATH=$(RAYLIB_BUILD_PATH) && \
	dotnet run --project client

build_raylib:
	mkdir -p $(RAYLIB_PATH)/build
	cmake -S $(RAYLIB_PATH) -B $(RAYLIB_PATH)/build \
		-DBUILD_SHARED_LIBS=ON \
		-DBUILD_EXAMPLES=OFF \
		-DBUILD_GAMES=OFF
	cmake --build $(RAYLIB_PATH)/build --config Release -j8


clean_raylib:
	rm -rf $(RAYLIB_PATH)/build

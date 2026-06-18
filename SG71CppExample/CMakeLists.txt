cmake_minimum_required(VERSION 3.16)
project(SG71CppExample LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

add_executable(SG71CppExample
    main.cpp
    SelfUpdater.cpp
    ${CMAKE_SOURCE_DIR}/../../cpp_client/SG71Client.cpp
)

target_include_directories(SG71CppExample PRIVATE
    ${CMAKE_SOURCE_DIR}/../../cpp_client
    ${CMAKE_CURRENT_SOURCE_DIR}
)

target_link_libraries(SG71CppExample PRIVATE
    winhttp
    crypt32
    advapi32
)

if(MSVC)
    target_compile_options(SG71CppExample PRIVATE /W4 /utf-8)
endif()

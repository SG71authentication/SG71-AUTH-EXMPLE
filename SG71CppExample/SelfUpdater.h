#pragma once

#include <string>

namespace SelfUpdater {

std::wstring GetExecutablePath();
bool DownloadUpdateBesideExe(const std::string& updateUrl, std::wstring& outPath);
void ApplyUpdateAndRestart(const std::wstring& downloadedExe);

} // namespace SelfUpdater

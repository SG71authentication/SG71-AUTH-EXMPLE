#pragma once

#include "SG71Client.h"

namespace AuthConfig {

inline const char* AdminId = "YOUR_ADMIN_ID";
inline const char* AppName = "Your App Name";
/** Must match App Version in Application Manager (checked via API). */
inline const char* AppVersion = "1.0";

inline void Apply() {
#ifdef SG71_USE_PRODUCTION_API
    SG71::SG71Client::SetApiBaseUrl(SG71::SG71Client::kProductionApiUrl);
#else
    SG71::SG71Client::SetApiBaseUrl(SG71::SG71Client::kLocalDirectApiUrl);
#endif
}

} // namespace AuthConfig

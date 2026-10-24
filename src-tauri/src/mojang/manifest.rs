use serde::{Deserialize, Serialize};
use std::collections::HashMap;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct VersionManifest {
    pub id: String,
    pub mainClass: String,
    pub minecraftArguments: String,
    pub downloads: VersionDownloads,
    pub libraries: Vec<Library>,
    pub assetIndex: AssetIndexInfo,
    pub assets: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct VersionDownloads {
    pub client: DownloadArtifact,
    pub server: Option<DownloadArtifact>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct DownloadArtifact {
    pub sha1: String,
    pub size: u64,
    pub url: String,
    pub path: Option<String>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Library {
    pub name: String,
    pub downloads: Option<LibraryDownloads>,
    pub rules: Option<Vec<Rule>>,
    pub natives: Option<HashMap<String, String>>,
    pub extract: Option<ExtractRules>,
    pub url: Option<String>, // Legacy url if downloads not present
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct LibraryDownloads {
    pub artifact: Option<DownloadArtifact>,
    pub classifiers: Option<HashMap<String, DownloadArtifact>>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Rule {
    pub action: String, // "allow" or "disallow"
    pub os: Option<OsRule>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct OsRule {
    pub name: String, // "windows", "linux", "osx"
    pub version: Option<String>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ExtractRules {
    pub exclude: Option<Vec<String>>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct AssetIndexInfo {
    pub id: String,
    pub sha1: String,
    pub size: u64,
    pub totalSize: u64,
    pub url: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct AssetIndex {
    pub objects: HashMap<String, AssetObject>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct AssetObject {
    pub hash: String,
    pub size: u64,
}

impl Library {
    /// Determines if this library is allowed on Windows
    pub fn is_allowed_on_windows(&self) -> bool {
        if let Some(rules) = &self.rules {
            let mut allowed = false;
            for rule in rules {
                if let Some(os) = &rule.os {
                    if os.name == "windows" {
                        allowed = rule.action == "allow";
                    }
                } else {
                    // Rule without specific OS applies to all
                    allowed = rule.action == "allow";
                }
            }
            allowed
        } else {
            true
        }
    }

    /// Gets the native classifier name for Windows (usually "natives-windows")
    pub fn get_windows_native_classifier(&self) -> Option<String> {
        if let Some(natives) = &self.natives {
            if let Some(classifier) = natives.get("windows") {
                return Some(classifier.clone());
            }
        }
        None
    }
}

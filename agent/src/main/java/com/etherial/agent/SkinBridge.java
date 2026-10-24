package com.etherial.agent;

import java.io.BufferedReader;
import java.io.InputStreamReader;
import java.net.HttpURLConnection;
import java.net.URL;
import java.util.concurrent.ConcurrentHashMap;
import java.lang.reflect.Method;

public class SkinBridge {
    private static final ConcurrentHashMap<String, Object> CACHED_PROPERTIES = new ConcurrentHashMap<>();
    private static final ConcurrentHashMap<String, Boolean> FAILED_LOOKUPS = new ConcurrentHashMap<>();

    public static void populateSkin(Object gameProfile) {
        if (gameProfile == null) return;
        try {
            Class<?> gpClass = gameProfile.getClass();
            Method getNameMethod = gpClass.getMethod("getName");
            String username = (String) getNameMethod.invoke(gameProfile);
            if (username == null || username.trim().isEmpty()) return;

            Method getPropsMethod = gpClass.getMethod("getProperties");
            Object propMap = getPropsMethod.invoke(gameProfile);
            if (propMap == null) return;

            // Check if "textures" is already populated
            Method containsKey = propMap.getClass().getMethod("containsKey", Object.class);
            if ((Boolean) containsKey.invoke(propMap, "textures")) {
                Method getMethod = propMap.getClass().getMethod("get", Object.class);
                java.util.Collection<?> col = (java.util.Collection<?>) getMethod.invoke(propMap, "textures");
                if (col != null && !col.isEmpty()) {
                    return; // Already has textures!
                }
            }

            if (FAILED_LOOKUPS.containsKey(username.toLowerCase())) {
                return; // Don't hammer API for nonexistent skins
            }

            // Check memory cache
            Object cachedProp = CACHED_PROPERTIES.get(username.toLowerCase());
            if (cachedProp != null) {
                Method putMethod = propMap.getClass().getMethod("put", Object.class, Object.class);
                putMethod.invoke(propMap, "textures", cachedProp);
                return;
            }

            // Fetch from Mojang in background or inline
            Object prop = fetchMojangSkinProperty(username, gpClass.getClassLoader());
            if (prop != null) {
                CACHED_PROPERTIES.put(username.toLowerCase(), prop);
                Method putMethod = propMap.getClass().getMethod("put", Object.class, Object.class);
                putMethod.invoke(propMap, "textures", prop);
                System.out.println("[Etherial Agent] Successfully applied skin for player: " + username);
            } else {
                FAILED_LOOKUPS.put(username.toLowerCase(), Boolean.TRUE);
            }

        } catch (Throwable t) {
            System.err.println("[Etherial Agent] SkinBridge error: " + t.getMessage());
        }
    }

    private static Object fetchMojangSkinProperty(String username, ClassLoader loader) {
        try {
            // 1. Get UUID from Mojang API
            String profileUrl = "https://api.mojang.com/users/profiles/minecraft/" + username;
            String profileJson = httpGet(profileUrl);
            if (profileJson == null || !profileJson.contains("\"id\"")) {
                return null;
            }

            String id = extractJsonString(profileJson, "id");
            if (id == null) return null;

            // 2. Get Profile with textures
            String sessionUrl = "https://sessionserver.mojang.com/session/minecraft/profile/" + id + "?unsigned=false";
            String sessionJson = httpGet(sessionUrl);
            if (sessionJson == null || !sessionJson.contains("\"value\"")) {
                return null;
            }

            String value = extractJsonString(sessionJson, "value");
            String signature = extractJsonString(sessionJson, "signature");
            if (value == null) return null;

            // 3. Instantiate com.mojang.authlib.properties.Property
            Class<?> propClass = loader.loadClass("com.mojang.authlib.properties.Property");
            if (signature != null) {
                return propClass.getConstructor(String.class, String.class, String.class)
                        .newInstance("textures", value, signature);
            } else {
                return propClass.getConstructor(String.class, String.class)
                        .newInstance("textures", value);
            }

        } catch (Throwable t) {
            return null;
        }
    }

    private static String httpGet(String urlStr) {
        try {
            URL url = new URL(urlStr);
            HttpURLConnection conn = (HttpURLConnection) url.openConnection();
            conn.setRequestMethod("GET");
            conn.setConnectTimeout(2500);
            conn.setReadTimeout(2500);
            conn.setRequestProperty("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Etherial/1.0");
            if (conn.getResponseCode() != 200) return null;

            BufferedReader reader = new BufferedReader(new InputStreamReader(conn.getInputStream(), "UTF-8"));
            StringBuilder sb = new StringBuilder();
            String line;
            while ((line = reader.readLine()) != null) {
                sb.append(line);
            }
            reader.close();
            return sb.toString();
        } catch (Throwable t) {
            return null;
        }
    }

    private static String extractJsonString(String json, String key) {
        int keyIdx = json.indexOf("\"" + key + "\"");
        if (keyIdx == -1) return null;
        int colonIdx = json.indexOf(":", keyIdx);
        if (colonIdx == -1) return null;
        int quoteStart = json.indexOf("\"", colonIdx);
        if (quoteStart == -1) return null;
        int quoteEnd = json.indexOf("\"", quoteStart + 1);
        if (quoteEnd == -1) return null;
        return json.substring(quoteStart + 1, quoteEnd);
    }
}

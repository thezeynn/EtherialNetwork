package com.etherial.agent;

import java.lang.reflect.Constructor;
import java.lang.reflect.Field;
import java.lang.reflect.Method;

public class AutoConnectBridge {
    public static volatile String TARGET_HOST = "localhost";
    public static volatile int TARGET_PORT = 25565;

    public static boolean isForbiddenScreen(String name) {
        if (name == null) return false;
        return name.equals("aya") // Notch: GuiMainMenu
            || name.equals("azh") // Notch: GuiMultiplayer
            || name.equals("axv") // Notch: GuiSelectWorld (Singleplayer)
            || name.equals("axg") // Notch: GuiScreenServerList (Direct Connect)
            || name.equals("axi") // Notch: GuiScreenAddServer
            || name.equals("net.minecraft.client.gui.GuiMainMenu")
            || name.equals("net.minecraft.client.gui.GuiMultiplayer")
            || name.equals("net.minecraft.client.gui.GuiSelectWorld")
            || name.equals("net.minecraft.client.gui.GuiScreenServerList")
            || name.equals("net.minecraft.client.gui.GuiScreenAddServer")
            || name.endsWith(".GuiMainMenu")
            || name.endsWith(".GuiMultiplayer")
            || name.endsWith(".GuiSelectWorld")
            || name.contains("Realms");
    }

    public static boolean isWorldNull(Object mc) {
        if (mc == null) return true;
        try {
            Field f = null;
            try {
                f = mc.getClass().getDeclaredField("theWorld");
            } catch (NoSuchFieldException e) {
                f = mc.getClass().getDeclaredField("f"); // Notch ave.f is WorldClient
            }
            f.setAccessible(true);
            return f.get(mc) == null;
        } catch (Throwable t) {
            return false;
        }
    }

    /**
     * Intercepts in-game Disconnect clicks.
     * When user clicks Disconnect in ESC pause menu, directly terminates the game process.
     */
    public static boolean handleIngameMenuClick(Object button) {
        if (button == null) return false;
        try {
            Field idField;
            try {
                idField = button.getClass().getField("id");
            } catch (NoSuchFieldException e) {
                idField = button.getClass().getField("k"); // Notch avs.k is id
            }
            int id = idField.getInt(button);
            if (id == 1) { // Disconnect button!
                System.out.println("[Etherial Agent] User clicked In-Game Disconnect -> Terminating game process directly as requested!");
                System.exit(0);
                return true;
            }
        } catch (Throwable t) {
            System.err.println("[Etherial Agent] handleIngameMenuClick error: " + t.getMessage());
        }
        return false;
    }

    /**
     * Intercepts screen transitions in Minecraft.
     * Prevents any attempt to return to GuiMainMenu, GuiMultiplayer, GuiSelectWorld, etc.
     * Instantly redirects to GuiConnecting -> TARGET_HOST:TARGET_PORT.
     */
    public static Object replaceScreen(Object mc, Object targetScreen) {
        if (targetScreen == null) {
            if (isWorldNull(mc)) {
                System.out.println("[Etherial Agent] displayGuiScreen(null) outside world -> Direct connecting to " 
                    + TARGET_HOST + ":" + TARGET_PORT);
                Object connecting = createGuiConnecting(mc, TARGET_HOST, TARGET_PORT);
                if (connecting != null) {
                    return connecting;
                }
            }
            return null;
        }

        String className = targetScreen.getClass().getName();

        // If it's already connecting, do not intercept
        if (className.equals("awz") || className.endsWith("GuiConnecting")) {
            return targetScreen;
        }

        // If it's GuiDisconnected (axh in Notch 1.8.9, or GuiDisconnected in MCP),
        // let the player view the disconnect cause. When they click the button,
        // it triggers displayGuiScreen(parent) which will be intercepted below.
        if (className.equals("axh") || className.endsWith("GuiDisconnected")) {
            return targetScreen;
        }

        // Check if the opened screen is forbidden (MainMenu, Multiplayer, Singleplayer, Realms)
        if (isForbiddenScreen(className)) {
            System.out.println("[Etherial Agent] Forbidden screen blocked (" + className + ") -> Direct connecting to " 
                + TARGET_HOST + ":" + TARGET_PORT);

            Object connecting = createGuiConnecting(mc, TARGET_HOST, TARGET_PORT);
            if (connecting != null) {
                return connecting;
            }
        }

        return targetScreen;
    }

    public static Object createGuiConnecting(Object mc, String host, int port) {
        try {
            ClassLoader cl = mc.getClass().getClassLoader();

            // Locate GuiConnecting class (MCP: net.minecraft.client.multiplayer.GuiConnecting, Notch 1.8.9: awz)
            Class<?> guiConnectingClass;
            try {
                guiConnectingClass = cl.loadClass("net.minecraft.client.multiplayer.GuiConnecting");
            } catch (ClassNotFoundException e) {
                guiConnectingClass = cl.loadClass("awz");
            }

            // Try 4-param constructor: (GuiScreen parent, Minecraft mc, String host, int port)
            for (Constructor<?> c : guiConnectingClass.getConstructors()) {
                Class<?>[] p = c.getParameterTypes();
                if (p.length == 4 && p[2] == String.class && p[3] == int.class) {
                    return c.newInstance(null, mc, host, port);
                }
            }

            // Try 3-param constructor: (GuiScreen parent, Minecraft mc, ServerAddress address)
            Class<?> serverAddressClass;
            try {
                serverAddressClass = cl.loadClass("net.minecraft.client.multiplayer.ServerAddress");
            } catch (ClassNotFoundException e) {
                serverAddressClass = cl.loadClass("bde");
            }

            Method fromStringMethod;
            try {
                fromStringMethod = serverAddressClass.getMethod("fromString", String.class);
            } catch (NoSuchMethodException e) {
                fromStringMethod = serverAddressClass.getMethod("a", String.class);
            }
            Object serverAddress = fromStringMethod.invoke(null, host + ":" + port);

            for (Constructor<?> c : guiConnectingClass.getConstructors()) {
                if (c.getParameterTypes().length == 3) {
                    return c.newInstance(null, mc, serverAddress);
                }
            }

        } catch (Throwable t) {
            System.err.println("[Etherial Agent] Failed to instantiate GuiConnecting: " + t.getMessage());
            t.printStackTrace();
        }
        return null;
    }
}

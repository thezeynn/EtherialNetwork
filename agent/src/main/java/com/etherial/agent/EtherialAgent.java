package com.etherial.agent;

import java.lang.instrument.Instrumentation;

public class EtherialAgent {

    public static void premain(String agentArgs, Instrumentation inst) {
        System.out.println("==================================================");
        System.out.println("  ETHERIAL HIGH-PERFORMANCE CLIENT AGENT v1.0.0   ");
        System.out.println("  Medieval 1.8.9 Core Hook Engaged (Zero-Deadlock)");
        System.out.println("==================================================");

        if (agentArgs != null && !agentArgs.trim().isEmpty()) {
            String[] parts = agentArgs.trim().split(":");
            if (parts.length >= 1 && !parts[0].isEmpty()) {
                AutoConnectBridge.TARGET_HOST = parts[0];
            }
            if (parts.length >= 2) {
                try {
                    AutoConnectBridge.TARGET_PORT = Integer.parseInt(parts[1]);
                } catch (NumberFormatException e) {
                    System.err.println("[Etherial Agent] Invalid port specified in agentArgs: " + parts[1]);
                }
            }
        }

        System.out.println("[Etherial Agent] Auto-Connect Target Configured: " 
            + AutoConnectBridge.TARGET_HOST + ":" + AutoConnectBridge.TARGET_PORT);

        // Register the bytecode transformer
        inst.addTransformer(new MinecraftTransformer(), true);
        System.out.println("[Etherial Agent] Bytecode Transformer successfully registered.");
    }

    public static void agentmain(String agentArgs, Instrumentation inst) {
        premain(agentArgs, inst);
    }
}
